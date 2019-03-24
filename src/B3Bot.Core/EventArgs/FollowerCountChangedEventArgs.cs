using System;

namespace B3Bot.Core
{
    public class FollowerCountChangedEventArgs : EventArgs
    {
        public FollowerCountChangedEventArgs(int updatedFollowerCount)
        {
            FollowerCount = updatedFollowerCount;
        }

        public int FollowerCount { get; }
    }
}
