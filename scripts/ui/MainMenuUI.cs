using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

public partial class MainMenuUI : Control
{
    [Export]
    public RichTextLabel[] menus = new RichTextLabel[0];
    [Export]
    public RichTextLabel[] extraMenus = new RichTextLabel[0];
    [Export]
    public Control messagePopup;
    [Export]
    public Control[] pages = [];

    [Export]
    public string IssuesLink;

    public Action<string> MenuChanged;

    public List<string> placeholderMenus = new List<string>();
    public List<string> placeholderExtraMenus = new List<string>();

    public override void _Ready()
    {
        placeholderMenus.Clear();
        for (int i = 0; i < menus.Length; i++)
        {
            placeholderMenus.Add(menus[i].Text);
            menus[i].MetaClicked += SwitchMenu;
        }
        placeholderExtraMenus.Clear();
        for (int i = 0; i < extraMenus.Length; i++)
        {
            placeholderExtraMenus.Add(extraMenus[i].Text);
            extraMenus[i].MetaClicked += SwitchMenu;
        }
        TranslateAll();
        SwitchMenu(string.Empty);
        MenuManager.Instance.OnSettingsChanged += TranslateAll;
    }

    public override void _ExitTree()
    {
        MenuManager.Instance.OnSettingsChanged -= TranslateAll;
        foreach (var menu in menus)
        {
            if (menu == null)
                continue;
            menu.MetaClicked -= SwitchMenu;
        }

        foreach (var menu in extraMenus)
        {
            if (menu == null)
                continue;
            menu.MetaClicked -= SwitchMenu;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            TranslateAll();
        }
    }

    public static string[] GetTranslationMessages()
    {
        HashSet<string> list = new HashSet<string>();
        string[] langs = TranslationServer.GetLoadedLocales();
        for (int i = 0; i < langs.Length; i++)
        {
            var obj = TranslationServer.GetTranslationObject(langs[i]);
            if (IsInstanceValid(obj))
            {
                list.UnionWith(obj.GetMessageList());
            }
        }
        return list.ToArray();
    }

    public static string TranslateText(string placeholder)
    {
        string text = placeholder;
        var list = GetTranslationMessages();
        for (int j = 0; j < list.Length; j++)
        {
            if (text.Equals(list[j]))
            {
                text = TranslationServer.Translate(list[j]);
                return text;
            }
        }
        for (int j = 0; j < list.Length; j++)
        {
            var idx = text.IndexOf(list[j]);
            if (idx != -1)
            {
                text = text.Replace(list[j], TranslationServer.Translate(list[j]));
            }
        }
        return text;
    }

    public static string TranslateText(Translation obj, string placeholder)
    {
        if (!IsInstanceValid(obj))
            return placeholder;
        string text = placeholder;
        var list = obj.GetMessageList();
        for (int j = 0; j < list.Length; j++)
        {
            var idx = text.IndexOf(list[j], StringComparison.Ordinal);
            if (idx != -1)
            {
                text = text.Replace(list[j], TranslationServer.Translate(list[j]));
            }
        }
        return text;
    }

    public void TranslateAll()
    {
        if (!IsNodeReady())
            return;
        var list = GetTranslationMessages();
        for (int i = 0; i < menus.Length; i++)
        {
            if (i >= placeholderMenus.Count)
            {
                Log.PrintWarn("Missing menu translation.");
            }
            else
            {
                menus[i].Text = placeholderMenus[i];
            }
        }
        for (int i = 0; i < extraMenus.Length; i++)
        {
            if (i >= placeholderExtraMenus.Count)
            {
                Log.PrintWarn("Missing extra menu translation.");
            }
            else
            {
                extraMenus[i].Text = placeholderExtraMenus[i];
            }
        }
        for (int i = 0; i < menus.Length; i++)
        {
            for (int j = 0; j < list.Length; j++)
            {
                var idx = menus[i].Text.IndexOf(list[j], StringComparison.Ordinal);
                if (idx != -1)
                {
                    menus[i].Text = menus[i].Text.Replace(list[j], Tr(list[j]));
                }
            }
        }
        for (int i = 0; i < extraMenus.Length; i++)
        {
            for (int j = 0; j < list.Length; j++)
            {
                var idx = extraMenus[i].Text.IndexOf(list[j], StringComparison.Ordinal);
                if (idx != -1)
                {
                    extraMenus[i].Text = extraMenus[i].Text.Replace(list[j], Tr(list[j]));
                }
            }
        }
    }

    public void CreateServer()
    {
        MenuManager.Instance.Host(27015);
    }

    public void DirectConnect(Node data)
    {
        if (data is LineEdit edit)
        {
            if (string.IsNullOrWhiteSpace(edit.Text))
                MenuManager.Instance.Join(null, null);
            else
                MenuManager.Instance.Join(edit.Text, edit.Text);
        }
    }

    public void RefreshServers(Node data)
    {
        if (data is ServerListUI ui)
        {
            ui.Refresh();
        }
    }

    public void Quit()
    {
        // OS.Alert("Memory access violation", "Error!");
        GetTree().Quit();
    }

    public void Editor()
    {
        MenuManager.Instance.LoadLayoutEditor();
    }

    public void WorkshopEditor()
    {
        MenuManager.Instance.LoadWorkshopEditor();
    }

    public void OpenIssueTracker()
    {
        OS.ShellOpen(IssuesLink);
    }

    public void OpenUserFolder()
    {
        OS.ShellShowInFileManager(OS.GetUserDataDir());
    }

    public RichTextLabel GetActiveMenu()
    {
        foreach (var menu in menus)
        {
            if (menu.Visible)
                return menu;
        }
        return null;
    }

    public void HideMenu()
    {
        foreach (var menu in menus)
        {
            menu.Visible = false;
        }
        messagePopup.Visible = true;
    }

    public void SwitchPage(string name)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].Name.ToString().Equals(name))
            {
                pages[i].Visible = true;
            }
            else
            {
                pages[i].Visible = false;
            }
        }
    }

    public void SwitchMenu(Variant meta)
    {
        string name = meta.AsString();
        // Log.Print(name);

        if (name.StartsWith("_"))
        {
            string[] arr = name.Substring(1).Split(' ');
            Variant[] args = new Variant[arr.Length - 1];
            for (int i = 0; i < args.Length; i++)
            {
                if (arr[i+1].StartsWith("$"))
                {
                    Node n = GetNode(arr[i+1].Substring(1));
                    args[i] = Variant.From(n);
                }
                else
                {
                    args[i] = Variant.From(arr[i+1]);
                }
            }
            Call(arr[0], args);
            return;
        }

        bool found = false;
        foreach (var menu in menus)
        {
            if (!found && menu.Name.ToString().Equals(name))
            {
                found = true;
                menu.Visible = true;
                continue;
            }

            menu.Visible = false;
        }
        messagePopup.Visible = false;

        if (!found && menus.Length > 0)
        {
            menus[0].Visible = true;
        }
        MenuChanged?.Invoke(name);
    }
}
