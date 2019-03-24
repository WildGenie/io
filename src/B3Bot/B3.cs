using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using B3Bot.Core;
using B3Bot.Twitch;

namespace B3Bot
{
    public class B3 : IHostedService
    {
        private const string LOGGER_CATEGORY = "B3Bot.B3";

        public ILogger Logger { get; }
        public IServiceProvider ServiceProvider;
        public static IHubContext<OverlayHub> _overlayHubContext;

        private TwitchService _twitchService;

        public B3(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHubContext<OverlayHub> overlayHubContext) : 
            this(loggerFactory.CreateLogger(LOGGER_CATEGORY), serviceProvider)
        {
            _overlayHubContext = overlayHubContext;
        }

        private B3(ILogger logger, IServiceProvider serviceProvider)
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
            _twitchService = serviceProvider.GetService<TwitchService>();

            _twitchService.OnFollowerCountChanged += OnFollowerCountChangedAsync;
            _twitchService.OnViewerCountChanged += OnViewerCountChangedAsync;
            _twitchService.OnNewChatMessage += OnNewChatMessageAsync;
        }

        private async void OnFollowerCountChangedAsync(Object sender, FollowerCountChangedEventArgs e)
        {
            await _overlayHubContext.Clients.All.SendAsync("FollowerCountChanged", e.FollowerCount);
        }

        private async void OnViewerCountChangedAsync(Object sender, ViewerCountChangedEventArgs e)
        {
            await _overlayHubContext.Clients.All.SendAsync("ViewerCountChanged", e.ViewerCount);
        }

        private async void OnNewChatMessageAsync(Object sender, NewMessageEventArgs e)
        {

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _twitchService.StartTwitchMonitoring();
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _twitchService?.Dispose();
            return Task.CompletedTask;
        }
    }
}
