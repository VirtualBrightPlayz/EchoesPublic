using System.Linq;

/// <summary>
/// A base for simple admin commands (which can only be ran by the server host)
/// that are ran on players.
/// These commands are of the form `command playerID`.
/// Player ID parsing and permissions are handled automatically, and any additional
/// args are passed to the handler.
/// It is NOT possible to handle both `command playerID [args]` and
/// `command [args]`.
/// If an argument is specified, it will always be treated as a player ID.
/// </summary>
public abstract class SimpleAdminPlayerCommandBase : IConsoleCommand
{
    public abstract string Command { get; }
    public abstract string[] Alias { get; }
    public abstract string CommandDescription { get; }
    public abstract PlayerMode Target { get; }
    public abstract bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response);

    public bool Execute(ICommandSender sender, string[] args, out string response)
    {
        if (sender is PlayerConsole console)
        {
            if (console.IsAdmin)
            {
                if (args.Length == 0)
                {
                    if (Target == PlayerMode.Required || (Target == PlayerMode.SelfTarget && console.Player == null))
                    {
                        response = "Invalid player";
                        return false;
                    }

                    return Execute(console, Target == PlayerMode.SelfTarget ? console.Player : null, args, out response);
                }

                if (int.TryParse(args[0], out var selectedIdx))
                {
                    if (IPlayerList.List(console).PlayerList.FirstOrDefault(player => player.PlayerId == selectedIdx) is NetworkPlayer selectedPlayer)
                    {
                        return Execute(console, selectedPlayer, args.Skip(1).ToArray(), out response);
                    }
                    else if (selectedIdx == -1)
                    {
                        bool res = true;
                        response = string.Empty;
                        foreach (var plr in IPlayerList.List(console).PlayerList)
                        {
                            bool res2 = Execute(console, plr, args.Skip(1).ToArray(), out string resp);
                            response += $"({res2}) ({plr.PlayerId}): {resp}";
                        }
                        return res;
                    }
                }

                // They tried to provide a player, so regardless of the player mode, this
                // is an error.
                response = "Invalid player";
                return false;
            }
        }

        response = "You do not have permission to execute this command.";
        return false;
    }

    public enum PlayerMode
    {
        /// <summary>
        /// A player argument is required.
        /// </summary>
        Required,
        /// <summary>
        /// A player argument is optional, and will default to
        /// the player executing the command if not specified.
        /// If the command is not run by a player, and a player is
        /// not specified, then this command will fail.
        /// </summary>
        SelfTarget,
        /// <summary>
        /// A player argument is optional.
        /// </summary>
        Optional
    }
}