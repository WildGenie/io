using System;
using System.Threading.Tasks;

namespace B3Bot.Core.Commands
{
    public interface IExtendedCommand
    {
        string Name { get; }
        string Description { get; }

        /// <summary>
        /// Order by wich CanExecute are called, the higher the later
        /// </summary>
        int Order { get; }

        /// <summary>
        /// If true, don't run other commands after this one
        /// </summary>
        bool Final { get; }

        /// <summary>
        /// Cooldown for this command, or null
        /// </summary>
        /// <returns></returns>
        TimeSpan? Cooldown { get; }

        bool CanExecute(string userName, string fullCommandText);

        Task Execute(string userName, string fullCommandText);
    }
}
