using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using B3Bot.Core;

namespace B3Bot.Twitch.Chat
{
    public class ChatClient : IDisposable
    {
        private const string LOGGER_CATEGORY = "B3Bot.Twitch.Chat";
        private const string IRC_ADDRESS = "irc.chat.twitch.tv";
        private const int IRC_PORT = 80;

        private TcpClient _tcpClient;
        private StreamReader inputStream;
        private StreamWriter outputStream;

        private static readonly Regex reUserName = new Regex(@"!([^@]+)@");
        private static readonly Regex reBadges = new Regex(@"@badges=([^;]*)");
        private static Regex reChatMessage;
        private static Regex reWhisperMessage;

        public ILogger Logger { get; }

        public string ChannelName => Constants.TwitchChannel;

        public event EventHandler<ChatConnectedEventArgs> Connected;
        public event EventHandler<NewMessageEventArgs> NewMessage;
        public event EventHandler<ChatUserJoinedEventArgs> UserJoined;

        private Thread _receiveMessagesThread;
        private DateTime _nextThrottleReset;
        private readonly CancellationTokenSource _shutdown;
        private bool disposedValue = false; // To detect redundant calls

        public ChatClient(ILoggerFactory loggerFactory) 
            : this(loggerFactory.CreateLogger(LOGGER_CATEGORY))
        {
        }

        private ChatClient(ILogger logger)
        {
            Logger = logger;

            reChatMessage = new Regex($@"PRIVMSG #{Constants.TwitchChannel} :(.*)$");
            reWhisperMessage = new Regex($@"WHISPER {Constants.TwitchUsername} :(.*)$");

            _shutdown = new CancellationTokenSource();
        }

        /// <summary>
        /// Initializes the connection to Twitch and begins watching IRC for messages
        /// </summary>
        public void Init()
        {
            Connect();

            _receiveMessagesThread = new Thread(ReceiveMessagesOnThread);
            _receiveMessagesThread.Start();
        }

        /// <summary>
        /// Public interface to post messages to channel
        /// </summary>
        /// <param name="message">Message to send to channel</param>
        public void PostMessage(string message)
        {
            var fullMessage = $":{Constants.TwitchUsername}!{Constants.TwitchUsername}@{Constants.TwitchUsername}.tmi.twitch.tv PRIVMSG #{Constants.TwitchChannel} :{message}";
            SendMessage(fullMessage);
        }

        /// <summary>
        /// Public interface to whisper messages to user
        /// </summary>
        /// <param name="message">Message to send to user</param>
        /// <param name="userName">Username to receive message</param>
        public void WhisperMessage(string message, string userName)
        {
            var fullMessage = $":{Constants.TwitchUsername}!{Constants.TwitchUsername}@{Constants.TwitchUsername}.tmi.twitch.tv PRIVMSG #jtv :/w {userName} {message}";
            SendMessage(fullMessage);
        }

        private void Connect()
        {
            _tcpClient = new TcpClient(IRC_ADDRESS, IRC_PORT);

            inputStream = new StreamReader(_tcpClient.GetStream());
            outputStream = new StreamWriter(_tcpClient.GetStream());

            Logger.LogTrace("Beginning IRC authentication to Twitch");

            outputStream.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
            outputStream.WriteLine($"PASS oauth:{Constants.TwitchAccessToken}");
            outputStream.WriteLine($"NICK {Constants.TwitchUsername}");
            outputStream.WriteLine($"USER {Constants.TwitchUsername} 8 * :{Constants.TwitchUsername}");
            outputStream.Flush();

            outputStream.WriteLine($"JOIN #{Constants.TwitchChannel}");
            outputStream.Flush();

            Connected?.Invoke(this, new ChatConnectedEventArgs());
        }

        private void ReceiveMessagesOnThread()
        {
            var lastMessageReceivedTimestamp = DateTime.UtcNow;
            var errorPeriod = TimeSpan.FromSeconds(60);

            while (true)
            {
                Thread.Sleep(50);

                if (DateTime.UtcNow.Subtract(lastMessageReceivedTimestamp) > errorPeriod)
                {
                    Logger.LogInformation($"Haven't received a message in {errorPeriod.TotalSeconds} seconds");
                    lastMessageReceivedTimestamp = DateTime.UtcNow;
                }

                if (_shutdown.IsCancellationRequested)
                {
                    break;
                }

                if (_tcpClient.Connected && _tcpClient.Available > 0)
                {

                    var receivedMessage = ReadMessage();
                    if (string.IsNullOrEmpty(receivedMessage))
                    {
                        continue;
                    }

                    lastMessageReceivedTimestamp = DateTime.UtcNow;
                    Logger.LogTrace($"> {receivedMessage}");

                    // Handle the Twitch keep-alive
                    if (receivedMessage.StartsWith("PING"))
                    {
                        Logger.LogWarning("Received PING from Twitch... sending PONG");
                        SendMessage($"PONG :{receivedMessage.Split(':')[1]}");
                        continue;
                    }

                    ProcessMessage(receivedMessage);

                }
                else if (!_tcpClient.Connected)
                {
                    // Reconnect
                    Logger.LogWarning("Disconnected from Twitch.. Reconnecting in 2 seconds");
                    Thread.Sleep(2000);
                    this.Init();
                    return;
                }

            }

            Logger.LogWarning("Exiting ReceiveMessages Loop");

        }

        private string ReadMessage()
        {
            string message = null;

            try
            {
                message = inputStream.ReadLine();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error reading messages: " + ex);
            }

            return message ?? "";
        }

        private void ProcessMessage(string receivedMessage)
        {
            var userName = reUserName.Match(receivedMessage).Groups[1].Value;
            var message = "";

            if (userName == Constants.TwitchUsername) return; // Exit and do not process if the bot posted this message

            var badges = reBadges.Match(receivedMessage).Groups[1].Value.Split(',');

            if (!string.IsNullOrEmpty(userName) && receivedMessage.Contains($" JOIN #{ChannelName}"))
            {
                UserJoined?.Invoke(this, new ChatUserJoinedEventArgs { UserName = userName });
            }

            // Review messages sent to the channel
            if (reChatMessage.IsMatch(receivedMessage))
            {
                message = reChatMessage.Match(receivedMessage).Groups[1].Value;
                Logger.LogTrace($"Message received from '{userName}': {message}");
                NewMessage?.Invoke(this, new NewMessageEventArgs
                {
                    IsModerator = badges?.Contains(@"moderator/1") ?? false,
                    IsBroadcaster = (ChannelName == userName),
                    UserName = userName,
                    Message = message,
                    Badges = badges
                });
            }
            else if (reWhisperMessage.IsMatch(receivedMessage))
            {
                message = reWhisperMessage.Match(receivedMessage).Groups[1].Value;
                Logger.LogTrace($"Whisper received from '{userName}': {message}");

                NewMessage?.Invoke(this, new NewMessageEventArgs
                {
                    IsModerator = badges?.Contains(@"moderator/1") ?? false,
                    IsBroadcaster = (ChannelName == userName),
                    UserName = userName,
                    Message = message,
                    Badges = (badges ?? new string[] { }),
                    IsWhisper = true
                });
            }
        }

        private void SendMessage(string message)
        {
            TimeSpan? throttled = CheckThrottleStatus();

            Thread.Sleep(throttled.GetValueOrDefault(TimeSpan.FromSeconds(0)));

            outputStream.WriteLine(message);
            outputStream.Flush();
        }

        private TimeSpan? CheckThrottleStatus()
        {
            var throttleDuration = TimeSpan.FromSeconds(30);
            var maximumCommands = 100;

            if (_nextThrottleReset == null)
            {
                _nextThrottleReset = DateTime.UtcNow.Add(throttleDuration);
            }
            else if (_nextThrottleReset < DateTime.UtcNow)
            {
                _nextThrottleReset = DateTime.UtcNow.Add(throttleDuration);
            }

            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                Logger?.LogWarning("Disposing of ChatClient");
            }
            catch { }

            if (!disposedValue)
            {
                if (disposing)
                {
                    _shutdown.Cancel();
                }

                _tcpClient?.Dispose();
                disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose of the ChatClient
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
