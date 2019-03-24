using Newtonsoft.Json.Linq;

namespace B3Bot.Core
{
    /// <summary>
    /// Class representing a User object from Twitch API.
    /// </summary>
    public class UserInfo
    {
        public string DisplayName { get; set; }

        public long UserId { get; set; }

        public string UserName { get; set; }

        public string ProfileImageUrl { get; set; }

        public string Type { get; set; }

        public static explicit operator UserInfo(JToken obj)
        {
            return new UserInfo
            {
                DisplayName = obj["display_name"].Value<string>(),
                UserId = obj["id"].Value<long>(),
                ProfileImageUrl = obj["profile_image_url"].Value<string>(),
                Type = obj["type"].Value<string>(),
                UserName = obj["login"].Value<string>()
            };
        }
    }
}
