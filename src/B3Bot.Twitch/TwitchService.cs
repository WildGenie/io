using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using B3Bot.Core;
using B3Bot.Twitch.API;
using B3Bot.Twitch.Chat;

namespace B3Bot.Twitch
{
    public class TwitchService : IDisposable
    {
        private const string LOGGER_CATEGORY = "B3Bot.Twitch.TwitchService";

        private const string FOLLOWER_COUNT_URL = "/helix/users/follows?to_id={0}&first=1";
        private const string STREAM_URL = "/helix/streams?user_login={0}";
        private const string USER_INFO_URL_ID = "/helix/users?id={0}";
        private const string USER_INFO_URL_NAME = "/helix/users?login={0}";

        private readonly APIClient _apiClient;
        private readonly ChatClient _chatClient;

        public ILogger Logger { get; }

        public event EventHandler<NewMessageEventArgs> OnNewChatMessage;
        public event EventHandler<ChatUserJoinedEventArgs> OnChatUserJoined;

        public event EventHandler<FollowerCountChangedEventArgs> OnFollowerCountChanged;
        public event EventHandler<ViewerCountChangedEventArgs> OnViewerCountChanged;

        private const int followerWatchInterval = 50000;
        private Timer _followersTimer;
        private int _watchedFollowerCount;

        private const int viewersWatchInterval = 50000;
        private Timer _viewersTimer;
        private int _watchedViewerCount;

        private static StreamData _currentStreamData;
        private static long _currentStreamLastFetchUtcTicks;
        private readonly static SemaphoreSlim _currentStreamLock = new SemaphoreSlim(1);

        public TwitchService(APIClient apiClient, ChatClient chatClient, ILoggerFactory loggerFactory) :
            this(loggerFactory.CreateLogger(LOGGER_CATEGORY))
        {
            _apiClient = apiClient;
            _chatClient = chatClient;
        }

        private TwitchService(ILogger logger)
        {
            Logger = logger;
        }

        public void StartTwitchMonitoring()
        {
            try
            {
                WatchFollowers();
                WatchViewers();

                _chatClient.Init();

                Logger.LogInformation($"Now monitoring Twitch with {_watchedFollowerCount} followers and {_watchedViewerCount} Viewers");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("StartTwitchMonitoring failed: " + ex.Message);
            }
        }

        #region Follower methods

        private void WatchFollowers()
        {
            _followersTimer?.Dispose();

            _followersTimer = new Timer(OnWatchFollowers, null, 0, followerWatchInterval);
        }

        private async void OnWatchFollowers(object state)
        {
            // async void as TimerCallback delegate
            try
            {
                // Turn off timer, in case runs longer than interval
                _followersTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                try
                {
                    var foundFollowerCount = await GetFollowerCountAsync();
                    if (foundFollowerCount != _watchedFollowerCount)
                    {
                        _watchedFollowerCount = foundFollowerCount;
                        OnFollowerCountChanged?.Invoke(this, new FollowerCountChangedEventArgs(foundFollowerCount));
                    }
                }
                finally
                {
                    // Turn on timer
                    var intervalMs = Math.Max(1000, followerWatchInterval);
                    _followersTimer?.Change(intervalMs, intervalMs);
                }
            }
            catch (Exception ex)
            {
                // Don't let exception escape from async void
                Logger.LogError($"{DateTime.UtcNow}: OnWatchFollowers - Error {Environment.NewLine}{ex}");
            }
        }

        private async Task<int> GetFollowerCountAsync()
        {
            var result = await _apiClient.GetFromEndpoint(string.Format(FOLLOWER_COUNT_URL, Constants.TwitchChannelId));

            var resultString = await result.Content.ReadAsStringAsync();
            Logger.LogTrace($"Response from Twitch GetFollowerCount: '{resultString}'");

            return ParseFollowerResult(resultString);
        }

        private static int ParseFollowerResult(string twitchString)
        {
            var jObj = JsonConvert.DeserializeObject<JObject>(twitchString);
            return jObj.Value<int>("total");
        }

        #endregion

        #region Viewer methods 

        private void WatchViewers()
        {
            _viewersTimer?.Dispose();

            _viewersTimer = new Timer(OnWatchViewers, null, 0, viewersWatchInterval);
        }

        private async void OnWatchViewers(object state)
        {
            // async void as TimerCallback delegate
            try
            {
                // Turn off timer, in case runs longer than interval
                _viewersTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                try
                {
                    var foundViewerCount = await GetViewerCountAsync();
                    if (foundViewerCount != _watchedViewerCount)
                    {
                        _watchedViewerCount = foundViewerCount;
                        OnViewerCountChanged?.Invoke(this, new ViewerCountChangedEventArgs(foundViewerCount));
                    }
                }
                finally
                {
                    // Turn on timer
                    var intervalMs = Math.Max(1000, viewersWatchInterval);
                    _viewersTimer?.Change(intervalMs, intervalMs);
                }

            }
            catch (Exception ex)
            {
                // Don't let exception escape from async void
                Logger.LogError($"{DateTime.UtcNow}: OnWatchViewers - Error {Environment.NewLine}{ex}");
            }

        }

        private async Task<int> GetViewerCountAsync()
        {
            var stream = await GetStreamAsync();
            return (stream?.ViewerCount).GetValueOrDefault(0);
        }

        private async Task<StreamData> GetStreamAsync()
        {
            if (DateTime.UtcNow.Subtract(new DateTime(Volatile.Read(ref _currentStreamLastFetchUtcTicks))) <= TimeSpan.FromSeconds(5) && _currentStreamData != null)
            {
                return Volatile.Read(ref _currentStreamData);
            }

            if (await _currentStreamLock.WaitAsync(5000))
            {
                try
                {
                    var result = await _apiClient.GetFromEndpoint(string.Format(STREAM_URL, Constants.TwitchChannel));

                    var resultString = await result.Content.ReadAsStringAsync();
                    Logger.LogTrace($"Response from Twitch GetStream: '{resultString}'");

                    Volatile.Write(ref _currentStreamData, ParseStreamResult(resultString));
                    Volatile.Write(ref _currentStreamLastFetchUtcTicks, DateTime.UtcNow.Ticks);
                }
                finally
                {
                    _currentStreamLock.Release();
                }
            }

            return Volatile.Read(ref _currentStreamData);
        }

        private static StreamData ParseStreamResult(string twitchString)
        {
            var jObj = JsonConvert.DeserializeObject<JObject>(twitchString);

            if (!jObj["data"].HasValues)
            {
                return null;
            }

            var data = jObj.GetValue("data")[0];

            return (StreamData)data;
        }

        #endregion

        #region User methods

        public async Task<UserInfo> GetUserInfoAsync(long? userId, string userName = null)
        {
            string url = string.Empty;

            if (userId.HasValue)
            {
                url = string.Format(USER_INFO_URL_ID, userId);
            }
            else if (!string.IsNullOrEmpty(userName))
            {
                url = string.Format(USER_INFO_URL_NAME, userName);
            }

            if (string.IsNullOrEmpty(url)) return null;

            var result = await _apiClient.GetFromEndpoint(url);

            var resultString = await result.Content.ReadAsStringAsync();
            Logger.LogTrace($"Response from Twitch GetUserInfo: '{resultString}'");

            return ParseUserResult(resultString);
        }

        private static UserInfo ParseUserResult(string twitchString)
        {
            var obj = JsonConvert.DeserializeObject<JObject>(twitchString);
            return (UserInfo)obj;
        }

        #endregion

        public void Dispose()
        {
            _followersTimer?.Dispose();
            _viewersTimer?.Dispose();

            _apiClient?.Dispose();
            _chatClient?.Dispose();
        }
    }
}
