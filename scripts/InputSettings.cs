using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class InputSettings
{
    public const string UserSettingsFilePath = "user://input_settings.json";

    public struct InputMapping
    {
        public const string KEY_BUTTON = "KeyButton";
        public const string MOUSE_BUTTON = "MouseButton";

        public string Action { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }

        public InputMapping(string act, InputEventKey key)
        {
            Action = act;
            Type = KEY_BUTTON;
            Data = key.PhysicalKeycode.ToString();
        }

        public InputMapping(string act, InputEventMouseButton mButton)
        {
            Action = act;
            Type = MOUSE_BUTTON;
            Data = mButton.ButtonIndex.ToString();
        }

        public void ApplyMapping()
        {
            switch (Type)
            {
                case KEY_BUTTON:
                    Key key = Enum.Parse<Key>(Data, true);
                    InputMap.ActionAddEvent(Action, new InputEventKey() { PhysicalKeycode = key, Location = key == Key.Shift || key == Key.Ctrl || key == Key.Alt ? KeyLocation.Left : KeyLocation.Unspecified });
                    break;
                case MOUSE_BUTTON:
                    InputMap.ActionAddEvent(Action, new InputEventMouseButton() { ButtonIndex = Enum.Parse<MouseButton>(Data, true) });
                    break;
            }
        }

        public override string ToString()
        {
            return $"{GetName()}: {GetButtonName()}";
        }

        public string GetName()
        {
            return TranslationServer.Translate(Action.ToUpperInvariant());
        }

        public string GetButtonName()
        {
            switch (Type)
            {
                default:
                case KEY_BUTTON:
                    return Data;
                case MOUSE_BUTTON:
                    switch (Enum.Parse<MouseButton>(Data, true))
                    {
                        default:
                            return Data;
                        case MouseButton.Left:
                            return "Mouse1";
                        case MouseButton.Right:
                            return "Mouse2";
                        case MouseButton.Middle:
                            return "Mouse3";
                        case MouseButton.Xbutton1:
                            return "Mouse4";
                        case MouseButton.Xbutton2:
                            return "Mouse5";
                    }
            }
        }
    }

    public static InputMapping[] Mappings
    {
        get
        {
            List<InputMapping> mappings = new List<InputMapping>();
            foreach (var act in InputMap.GetActions())
            {
                if (act.ToString().StartsWith("ui_"))
                    continue;
                foreach (var ev in InputMap.ActionGetEvents(act))
                {
                    switch (ev)
                    {
                        case InputEventMouseButton mouseButton:
                            mappings.Add(new InputMapping(act, mouseButton));
                            break;
                        case InputEventKey key:
                            mappings.Add(new InputMapping(act, key));
                            break;
                    }
                }
            }
            return mappings.ToArray();
        }
        set
        {
            foreach (var map in value)
            {
                if (map.Action.ToString().StartsWith("ui_"))
                    continue;
                foreach (var ev in InputMap.ActionGetEvents(map.Action))
                {
                    switch (ev)
                    {
                        case InputEventMouseButton:
                        case InputEventKey:
                            InputMap.ActionEraseEvent(map.Action, ev);
                            break;
                    }
                }
                try
                {
                    map.ApplyMapping();
                }
                catch (Exception e)
                {
                    Log.PrintErr($"Error applying input mapping. {e}");
                }
            }
        }
    }

    public static string TranslateSlow(string text)
    {
        foreach (var mapping in Mappings)
        {
            text = text.Replace("%INPUT_" + mapping.Action.ToUpperInvariant() + "_BIND%", mapping.GetButtonName());
        }
        return text;
    }

    public static InputMapping GetMapping(StringName act)
    {
        // List<InputMapping> mappings = new List<InputMapping>();
        foreach (var ev in InputMap.ActionGetEvents(act))
        {
            switch (ev)
            {
                case InputEventMouseButton mouseButton:
                    return new InputMapping(act, mouseButton);
                case InputEventKey key:
                    return new InputMapping(act, key);
            }
        }
        return default;
    }

    public static void Read()
    {
        try
        {
            if (!FileAccess.FileExists(UserSettingsFilePath))
            {
                Write();
            }
            using var file = FileAccess.Open(UserSettingsFilePath, FileAccess.ModeFlags.Read);
            Mappings = JsonSerializer.Deserialize<InputMapping[]>(file.GetAsText(true));
        }
        catch (Exception e)
        {
            Log.PrintErr($"Error reading input settings, using defaults. {e}");
        }
    }

    public static void Write()
    {
        try
        {
            using var file = FileAccess.Open(UserSettingsFilePath, FileAccess.ModeFlags.Write);
            file.StoreString(JsonSerializer.Serialize(Mappings, new JsonSerializerOptions()
            {
                WriteIndented = true,
            }));
        }
        catch (Exception e)
        {
            Log.PrintErr($"Error writing input settings. {e}");
        }
    }

    public static void Reset()
    {
        InputMap.LoadFromProjectSettings();
        Write();
    }
}
