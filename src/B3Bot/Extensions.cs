using System;
using Microsoft.Extensions.DependencyInjection;

using B3Bot.Twitch;
using B3Bot.Twitch.API;
using B3Bot.Twitch.Chat;

namespace B3Bot
{
    public static class Extensions
    {
        public static IServiceCollection AddB3Bot(this IServiceCollection services)
        {
            services.AddHttpClient<APIClient>();
            services.AddSingleton<ChatClient>();
            services.AddSingleton<TwitchService>();

            services.AddHostedService<B3>();

            return services;
        }
    }
}
