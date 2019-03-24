using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace B3Bot
{
    public class OverlayHub : Hub
    {
        public async Task FollowerCountChangedAsync(int newFollowerCount)
        {
            await Clients.All.SendAsync("FollowerCountChanged", newFollowerCount);
        }

        public async Task ViewerCountChangedAsync(int newViewerCount)
        {
            await Clients.All.SendAsync("ViewerCountChanged", newViewerCount);
        }

        //public async Task NewEmojiAsync(string emojiUrl)
        //{
        //    await Clients.All.SendAsync("NewEmoji", emojiUrl);
        //}

        //public async Task NewChatMessageAsync(ChatMessage chatMessage)
        //{
        //    await Clients.All.SendAsync("NewChatMessage", chatMessage);
        //}

        //public async Task NewFollowerAsync(OnFollowArgs follower)
        //{
        //    await Clients.All.SendAsync("NewFollower", follower);
        //}

        //public async Task NewCheerAsync(OnBitsReceivedArgs bitsReceived)
        //{
        //    await Clients.All.SendAsync("NewCheer", bitsReceived);
        //}

        //public async Task NewSubscriptionAsync(ChannelSubscription subscription)
        //{
        //    await Clients.All.SendAsync("NewSubscription", subscription);
        //}

        //public async Task NewHostAsync(OnHostArgs host)
        //{
        //    await Clients.All.SendAsync("NewHost", host);
        //}
    }
}
