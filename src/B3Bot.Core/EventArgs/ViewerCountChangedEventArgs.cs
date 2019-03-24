using System;

namespace B3Bot.Core
{
    public class ViewerCountChangedEventArgs : EventArgs
    {
        public ViewerCountChangedEventArgs(int updatedViewerCount)
        {
            ViewerCount = updatedViewerCount;
        }

        public int ViewerCount { get; }
    }
}
