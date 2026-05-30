using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class BroadcastCommand : SimpleAdminCommandBase, IAdminConsoleCommand
{
    public override string Command { get; } = "Broadcast";

    public override string[] Alias { get; } = new string[] { "bc" };

    public override string CommandDescription { get; } = "Sends a message to the specified player.";

    public Type Category => typeof(AdminCategoryRound);

    public string Container => nameof(AdminCategoryRound.Container.Broadcast);

    public override bool Execute(PlayerConsole _, string[] args, out string response) // broadcast (players) (time) (message)...
    {
        args = JoinWordsBetweenQuotes(args).ToArray();
        
        if (args.Length < 2)
        {
            response = "Wrong syntax.";
            return false;
        }

        HashSet<NetworkPlayer> players = CommandHelper.ParsePlayersArgument(args[0]);
        
        if (players.Count == 0)
        {
            response = "No suitable players.";
            return false;
        }

        bool timeNotSpecificed = false;
        
        Log.Print(args[1]);
        
        if (!int.TryParse(args[1], out int duration))
        {
            duration = 5;
            timeNotSpecificed = true;
        }
        
        string msg =  timeNotSpecificed ? string.Join(' ', args.Skip(1)) : string.Join(' ', args.Skip(2));
        
        if (string.IsNullOrWhiteSpace(msg))
        {
            response = "Message cannot be empty.";
            return false;
        }

        foreach (NetworkPlayer player in players)
        {
            RoundManager.Instance.RpcId(player.AuthorityId, nameof(RoundManager.RpcBroadcast), msg, (double)duration);
        }
        
        response = $"Sent broadcast to player(s) {string.Join(", ", players.Select(p => p.username))}";
        return true;
    }

    public Control GenerateUi(AdminHUD admin)
    {
        AdminCategoryRound category = admin.GetActiveCategory<AdminCategoryRound>();

        if (category == null)
        {
            return new Control();
        }

        HBoxContainer container = new HBoxContainer();

        LineEdit messageBox = new LineEdit()
        {
            PlaceholderText = "Message",
            ExpandToTextLength = true,
            LayoutMode = 2,
            CaretBlink = true,
        };

        LineEdit durationBox = new LineEdit()
        {
            PlaceholderText = "Duration",
            ExpandToTextLength = true,
            LayoutMode = 2,
            CaretBlink = true,
        };

        Button broadcastSpepecific = new Button()
        {
            Text = "Broadcast to Selected Players",
        };

        Button broadcastAll = new Button()
        {
            Text = "Broadcast to All Players",
        };

        broadcastSpepecific.Pressed += () => admin.RunWithSelectedPlayers($"{Command} {{0}} {(string.IsNullOrEmpty(durationBox.Text) ? "5" : durationBox.Text)} {messageBox.Text}");
        broadcastAll.Pressed += () => admin.RunWithSelectedPlayers($"{Command} * {(string.IsNullOrEmpty(durationBox.Text) ? "5" : durationBox.Text)} {messageBox.Text}");

        container.AddChild(messageBox);
        container.AddChild(durationBox);
        container.AddChild(broadcastSpepecific);
        container.AddChild(broadcastAll);

        return container;
    }
     
    // From Callvote idk what I was on when I made this but I don't want to bother with making another one lmao - gl to who will try to debug this :steamhappy: !
    private static List<string> JoinWordsBetweenQuotes(string[] args)
    {
        List<string> result = [];
        bool inQuotes = false;
        int parenthesesDepth = 0;

        List<string> buffer = [];

        foreach (string arg in args)
        {
            if (arg.StartsWith("\""))
            {
                inQuotes = true;
                buffer.Clear();
            }

            if (arg.Contains("("))
            {
                parenthesesDepth += arg.Count(c => c == '(');
                buffer.Clear();
            }

            if (inQuotes || parenthesesDepth > 0)
            {
                buffer.Add(arg.Trim('"'));

                if (arg.EndsWith("\"") && inQuotes)
                {
                    inQuotes = false;
                }

                if (arg.Contains(")"))
                {
                    parenthesesDepth -= arg.Count(c => c == ')');
                }

                if (!inQuotes && parenthesesDepth == 0)
                {
                    result.Add(string.Join(" ", buffer));
                    buffer.Clear();
                }
            }
            else
            {
                result.Add(arg);
            }
        }

        return result;
    }
}