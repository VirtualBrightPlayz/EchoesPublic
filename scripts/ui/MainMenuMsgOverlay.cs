using System;
using Godot;

public partial class MainMenuMsgOverlay : RichTextLabel
{
    [Export]
    public Control root;
    [Export(PropertyHint.MultilineText)]
    public string placeholderText;

    private string metaMenu;

    public override void _Ready()
    {
        root.Visible = false;
        if (IInitScript.Instance.IsXR)
            return;
        MetaClicked += Meta;
        MenuManager.Instance.OnMenuMessage += Message;
    }

    public override void _ExitTree()
    {
        MetaClicked -= Meta;
        MenuManager.Instance.OnMenuMessage -= Message;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("menu_pause") && root.Visible)
        {
            MenuManager.Instance.loadingTokenSource?.Cancel();
            NetworkManager.Instance.Shutdown();
            if (MenuManager.Instance.menu is MainMenu m)
            {
                m.ui.SwitchMenu(metaMenu);
            }
            root.Visible = false;
        }
    }

    private void Meta(Variant meta)
    {
        // NetworkManager.Instance.Shutdown();
    }

    private void Message(string obj)
    {
        if (string.IsNullOrEmpty(obj))
        {
            root.Visible = false;
            return;
        }
        root.Visible = true;
        metaMenu = "Main";
        if (MenuManager.Instance.menu is MainMenu m)
        {
            var menu = m.ui.GetActiveMenu();
            metaMenu = menu?.Name ?? "Main";
        }
        Text = MainMenuUI.TranslateText(string.Format(placeholderText, obj));
    }
}
