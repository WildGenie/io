using System;
using Microsoft.Extensions.DependencyInjection;

using B3Bot.Twitch.API;
using B3Bot.Twitch.Chat;

namespace B3Bot.Twitch
{
    public static class Extensions
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Convert a Unix timestamp to a .NET DateTime
        /// </summary>
        /// <param name="unixTime"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }
    }
}
