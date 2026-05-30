using System;
using Godot;

public abstract class AdminConsoleCommandButton : SimpleAdminCommandBase, IAdminConsoleCommand
{
    public abstract string ButtonLabel { get; }
    
    public abstract Type Category { get; }
    
    public abstract string Container { get; }

    public virtual void OnPressed(AdminHUD admin)
    {
        admin.RunWithSelectedPlayers($"{Command} {{0}}");
    }
    
    public virtual Control GenerateUi(AdminHUD admin)
    {
        var button = new Button();
        button.Text = ButtonLabel;

        button.Pressed += () => OnPressed(admin);

        return button;
    }
}