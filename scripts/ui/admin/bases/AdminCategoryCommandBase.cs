using System.Linq;
using Godot;

public partial class AdminCategoryCommandBase : Node
{
    private AdminHUD Admin => GetParent().GetMeta(AdminHUD.META_NAME).As<AdminHUD>();
    
    public override void _Ready()
    {
        foreach (IConsoleCommand command in GameConsole.Instance.RegisteredCommands.OrderByDescending(c => c.Command))
        {
            if (command is not IAdminConsoleCommand adminConsoleCommand)
                continue;

            if (adminConsoleCommand.Category != GetType())
                continue;

            if (!IsInstanceValid(GetParent()))
                continue;

            Control container = GetParent().GetNodeOrNull<Control>(adminConsoleCommand.Container);

            if (container == null)
            {
                Log.PrintErr($"[{nameof(GetType)}] {command} admin command has wrong container name {adminConsoleCommand.Container}");
                continue;
            }
            
            container.AddChild(adminConsoleCommand.GenerateUi(Admin));
        }
    }
}