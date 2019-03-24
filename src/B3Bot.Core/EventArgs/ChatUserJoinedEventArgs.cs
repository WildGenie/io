using System;

namespace B3Bot.Core
{
    public class ChatUserJoinedEventArgs : EventArgs
    {
        /// <summary>
        /// Username of user who joined chat
        /// </summary>
        public string UserName { get; set; }
    }
}
