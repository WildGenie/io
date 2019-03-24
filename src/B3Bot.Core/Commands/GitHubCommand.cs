using System;
using System.Threading.Tasks;

namespace B3Bot.Core.Commands
{
    public class GitHubCommand : IBasicCommand
    {
        public string Trigger => "github";
        public string Description => "Outputs the URL of Mike's Github Repository";
        public TimeSpan? Cooldown => TimeSpan.FromSeconds(30);

        public Task Execute(object sender, NewMessageEventArgs e)
        {
            //await chatService.SendMessageAsync("Mike's Github repository can by found here: https://github.com/michaeljolley/");
            throw new NotImplementedException();
        }
    }
}
