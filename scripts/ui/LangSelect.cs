using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LangSelect : OptionButton
{
    public List<string> langs = new List<string>();
    public static Translation[] loadedTranslations = null;

    public override void _Ready()
    {
        GetPopup().MinSize = Vector2I.Zero;
        VisibilityChanged += VisChanged;
        ItemSelected += Change;
        Pressed += Press;
    }

    private void Press()
    {
        if (!GetViewport().GuiEmbedSubwindows)
            GetPopup().Position = DisplayServer.MouseGetPosition();
    }

    private void Change(long index)
    {
        Settings.User.LangFile = langs[(int)index];
        ReadTranslations();
        MenuManager.Instance.WriteSettings();
    }

    private void VisChanged()
    {
        DirAccess dir = DirAccess.Open("user://langs/");
        if (dir == null)
        {
            langs.Clear();
            langs.Insert(0, "Default English");
        }
        else
        {
            langs = dir.GetDirectories().ToList();
            langs.Insert(0, "Default English");
        }

        Clear();
        foreach (var lang in langs)
            AddItem(lang);
        Selected = langs.IndexOf(Settings.User.LangFile);
    }

    public static void ReadTranslations()
    {
        if (loadedTranslations != null)
            foreach (var lang in loadedTranslations) TranslationServer.RemoveTranslation(lang);
        loadedTranslations = LoadTranslationsFile($"user://langs/{Settings.User.LangFile}/lang.csv");
        if (loadedTranslations != null)
            foreach (var lang in loadedTranslations) TranslationServer.AddTranslation(lang);
    }

    public static Translation[] LoadTranslationsFile(string path)
    {
        if (!FileAccess.FileExists(path))
            return null;
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string[] top = file.GetCsvLine();
        if (top.Length >= 2 && top[0] == "keys")
        {
            Translation[] translations = new Translation[top.Length - 1];
            for (var i = 0; i < translations.Length; i++)
            {
                translations[i] = new Translation();
                translations[i].Locale = top[i + 1];
                if (translations.Length == 1)
                    translations[i].Locale = OS.GetLocale();
            }
            while (!file.EofReached())
            {
                string[] line = file.GetCsvLine();
                if (line.Length >= 2)
                {
                    for (var i = 1; i < line.Length; i++)
                    {
                        translations[i - 1].AddMessage(line[0], line[i].Replace("\\n", "\n"));
                    }
                }
            }
            return translations;
        }
        return null;
    }
}