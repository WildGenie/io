using System;
using System.Threading.Tasks;

namespace B3Bot.Core.Commands
{
    /// <summary>
    /// Simple keyword based command interface
    /// </summary>
    public interface IBasicCommand
    {
        /// <summary>
        /// The command keyword
        /// </summary>
        string Trigger { get; }

        /// <summary>
        /// Description of the command (used by !help)
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Cooldown for this command, or null
        /// </summary>
        TimeSpan? Cooldown { get; }

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">NewMessageEventArgs</param>
        Task Execute(object sender, NewMessageEventArgs e);
    }
}
