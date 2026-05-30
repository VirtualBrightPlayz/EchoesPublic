using System;
using Godot;

public partial class MainMenuMsgUI : RichTextLabel
{
    [Export]
    public MainMenuUI ui;
    [Export(PropertyHint.MultilineText)]
    public string placeholderText;

    public RichTextLabel lastMenu;

    public override void _Ready()
    {
        if (!MenuManager.Instance.IsXR)
            return;
        MetaClicked += Meta;
        MenuManager.Instance.OnMenuMessage += Message;
    }

    public override void _ExitTree()
    {
        MetaClicked -= Meta;
        MenuManager.Instance.OnMenuMessage -= Message;
    }

    private void Meta(Variant meta)
    {
        NetworkManager.Instance.Shutdown();
        ui.SwitchMenu(meta);
    }

    private void Message(string obj)
    {
        if (string.IsNullOrEmpty(obj))
        {
            ui.messagePopup.Visible = false;
            // ui.SwitchMenu(lastMenu?.Name ?? "");
            return;
        }
        var menu = ui.GetActiveMenu();
        if (menu == this)
            menu = lastMenu;
        else if (menu != null)
            lastMenu = menu;
        ui.HideMenu();
        Text = MainMenuUI.TranslateText(string.Format(placeholderText, obj, menu?.Name ?? "Main"));
    }
}
