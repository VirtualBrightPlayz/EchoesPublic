using Godot;

public partial class NukeTextCtrl : Node
{
    [Export]
    public bool isCancel = false;

    public string GetText()
    {
        if (!IsInstanceValid(RoundManager.Instance))
            return string.Empty;
        else if (RoundManager.Instance.nukeActive && isCancel)
            return RoundManager.Instance.nukeTimer.ToString("00.0") + "\nCancel";
        else if (RoundManager.Instance.nukeActive)
            return RoundManager.Instance.nukeTimer.ToString("00.0");
        else if (RoundManager.Instance.nukeOnCooldown)
            return "Restarting...\nPlease Wait";
        else if (isCancel)
            return "Online.";
        else
            return "Online.\nAwaiting input";
    }

    public override void _Process(double delta)
    {
        switch (GetParent())
        {
            case Label3D label3d:
                label3d.Text = GetText();
                break;
            case Label label:
                label.Text = GetText();
                break;
            case RichTextLabel richLabel:
                richLabel.Text = GetText();
                break;
        }
    }
}
