using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Vosk;

public partial class VoskProcessor : Node
{
    public static VoskProcessor Instance;

    [Export]
    public NetworkPlayer player;
    [Export]
    public SpeechRecognizer speech;
    [Export]
    public Timer endTimer;

    private bool isRecording = false;

    [Export]
    public string recentText = string.Empty;

    static VoskProcessor()
    {
        NativeLibrary.SetDllImportResolver(typeof(Vosk.Vosk).Assembly, Resolver);
    }

    private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.Equals("libvosk"))
        {
            var dir = Directory.GetCurrentDirectory();
            if (OS.HasFeature("editor"))
            {
                dir = Path.Combine(dir, "addons", "Vosk");
            }

            if (OS.GetName().Equals("Windows"))
            {
                string path = Path.Combine(dir, "libvosk.dll");
                return NativeLibrary.Load(path);
            }

            if (OS.GetName().Equals("Linux"))
            {
                string path = Path.Combine(dir, "libvosk.so");
                return NativeLibrary.Load(path);
            }
        }

        return IntPtr.Zero;
    }

    public override void _EnterTree()
    {
        if (player.IsLocalPlayer)
        {
            Instance = this;
            string model;
            if (OS.HasFeature("editor"))
                model = Path.Combine("addons", "Vosk", "vosk-model-small-en-us-0.15");
            else
                model = Path.Combine(OS.GetExecutablePath().GetBaseDir(), "vosk-model-small-en-us-0.15");
            // player.voiceChat.OnSpeak += OnSpeak;
            speech.modelPath = model;
            speech.OnPartialResult += OnPartial;
            speech.OnFinalResult += OnPartial;
            endTimer.Timeout += OnTimeout;
            // speech.RequestReady();
            // speech.recordBusName = "VoiceMicRecord";
        }
    }

    private void OnPartial(string partialResults)
    {
        var j = Json.ParseString(partialResults);
        if (j.VariantType == Variant.Type.Nil)
            return;
        var dict = j.AsGodotDictionary();
        string textFinal = string.Empty;
        if (dict.TryGetValue("text", out var text))
            textFinal = text.AsString().ToLower();
        else if (dict.TryGetValue("partial", out var partial))
            textFinal = partial.AsString().ToLower();
        if (textFinal.Length > recentText.Length)
            recentText = textFinal;
        // Log.PrintS("Recent Text:", textFinal);
    }

    private void OnTimeout()
    {
        OnPartial(speech.StopSpeechRecoginition());
    }

    public override void _Process(double delta)
    {
        if (player.IsLocalPlayer)
        {
            bool rec = player.voiceChat.IsSpeaking;
            if (rec != isRecording)
            {
                if (rec)
                {
                    endTimer.Stop();
                    speech.StartSpeechRecognition();
                    recentText = string.Empty;
                }
                else
                {
                    endTimer.Start();
                    // GD.Print(speech.StopSpeechRecoginition());
                    // OnPartial(speech.StopSpeechRecoginition());
                    // playback.PlayMessageOn(recentText);
                }
                isRecording = rec;
            }
        }
    }

}
