
namespace B3Bot.Core
{
    public class ConfigurationSettings
    {
        /// <summary>
        /// Name of Twitch channel we are watching
        /// </summary>
        public virtual string ChannelName { get; set; }

        /// <summary>
        /// Id of the Twitch user for the channel we're watching
        /// </summary>
        public virtual string ChannelId { get; set; }

        /// <summary>
        /// Application client id
        /// </summary>
        public virtual string ClientId { get; set; }

        /// <summary>
        /// Name of the bot in chat
        /// </summary>
        public virtual string ChatBotName { get; set; }

        /// <summary>
        /// Access token to Twitch API & services
        /// </summary>
        public virtual string OAuthToken { get; set; }
    }
}
