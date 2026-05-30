using System;
using Godot;

public class PlayersCommand : IConsoleCommand
{
    public string Command => "Players";

    public string[] Alias { get; } = new string[] { "list" };

    public string CommandDescription { get; } = "Shows a list of all players on the server";

    public bool Execute(ICommandSender sender, string[] args, out string response)
    {
        bool isAdmin = sender is PlayerConsole console && console.IsAdmin;
        var list = IPlayerList.List((Node)sender).PlayerList;
        int idSize = 5;
        int nameSize = 5;
        int roleSize = 5;
        for (int i = 0; i < list.Count; i++)
        {
            // TODO: an error can be thrown around here.
            string name = list[i].username;
            nameSize = Mathf.Max(nameSize, name.Length);

            string id = list[i].PlayerId.ToString();
            idSize = Mathf.Max(idSize, id.Length);

            string role = list[i].Role?.PlainDisplayName;
            roleSize = Mathf.Max(roleSize, role.Length);
        }

        response = "Players\n";

        string nameTitle = "Name";
        for (int j = nameTitle.Length; j < nameSize; j++)
            nameTitle += ' ';

        string idTitle = "Id";
        for (int j = idTitle.Length; j < idSize; j++)
            idTitle += ' ';

        string roleTitle = "Role";
        for (int j = roleTitle.Length; j < roleSize; j++)
            roleTitle += ' ';

        if (isAdmin)
            response += $"{idTitle}|{nameTitle}|Admin|{roleTitle}\n";
        else
            response += $"{idTitle}|{nameTitle}\n";

        for (int i = 0; i < list.Count; i++)
        {
            string name = list[i].username;
            if (name.Length > nameSize)
                name = name.Substring(0, nameSize);
            else
                for (int j = name.Length; j < nameSize; j++)
                    name += ' ';

            string id = list[i].PlayerId.ToString();
            if (id.Length > idSize)
                id = id.Substring(0, idSize);
            else
                for (int j = id.Length; j < idSize; j++)
                    id += ' ';

            string role = list[i].Role?.PlainDisplayName;
            if (role.Length > roleSize)
                role = role.Substring(0, roleSize);
            else
                for (int j = role.Length; j < roleSize; j++)
                    role += ' ';
            role = $"[color=#{list[i].Role?.RoleColor.ToHtml()}]{role}[/color]";

            if (isAdmin)
                response += $"{id}|{name}|{(list[i].console.IsAdmin ? 1 : 0)}    |{role}\n";
            else
                response += $"{id}|{name}\n";
        }
        return true;
    }
}
