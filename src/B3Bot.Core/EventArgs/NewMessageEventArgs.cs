using System;

namespace B3Bot.Core
{
    public class NewMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Username of user who sent message
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Message sent
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Badges associated with user
        /// </summary>
        public string[] Badges { get; set; }

        /// <summary>
        /// Is user a moderator
        /// </summary>
        public bool IsModerator { get; set; }

        /// <summary>
        /// Is user the broadcaster
        /// </summary>
        public bool IsBroadcaster { get; set; }

        /// <summary>
        /// Is the message a whisper?
        /// </summary>
        public bool IsWhisper { get; set; } = false;
    }
}
