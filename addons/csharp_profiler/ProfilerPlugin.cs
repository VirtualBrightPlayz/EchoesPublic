#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Godot;
// using HarmonyLib;

[Tool]
public partial class ProfilerPlugin : EditorPlugin
{
    #if false
    [Tool]
    public partial class ProfilerEditorUI : VBoxContainer
    {
        public struct FrameUIData
        {
            public string TypeName;
            public string MethodName;
            public ulong TotalTimeUSec;
            public uint TotalCalls;
        }

        public CheckButton btn;
        public Tree tree;

        public override void _Ready()
        {
            Name = "C# Profiler";
            btn = new CheckButton();
            btn.Text = "Full View";
            AddChild(btn);

            tree = new Tree();
            tree.SizeFlagsVertical = SizeFlags.ExpandFill;
            tree.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            tree.Columns = 4;
            tree.ColumnTitlesVisible = true;
            tree.HideRoot = true;
            tree.SetColumnTitle(0, "Type");
            tree.SetColumnTitle(1, "Method");
            tree.SetColumnTitle(2, "Time (ms)");
            tree.SetColumnTitle(3, "Calls");
            AddChild(tree);
        }

        public void SetFrameData(CSharpProfiler.FrameData[] frames)
        {
            if (btn.ButtonPressed)
            {
                tree.Clear();
                var root = tree.CreateItem();
                for (int i = 0; i < frames.Length; i++)
                {
                    if (frames[i].IsPostfix)
                        continue;
                    for (int j = i + 1; j < frames.Length; j++)
                    {
                        if (frames[i].TypeName == frames[j].TypeName && frames[i].MethodName == frames[j].MethodName && frames[j].IsPostfix)
                        {
                            var data = frames[j];
                            var item = tree.CreateItem(root);
                            item.SetText(0, data.TypeName);
                            item.SetText(1, data.MethodName);
                            item.SetText(2, ((data.TimeUSec - frames[i].TimeUSec) / 1000f).ToString());
                            item.SetText(3, "N/A");
                            break;
                        }
                    }
                }
            }
            else
            {
                List<FrameUIData> datas = new List<FrameUIData>();
                for (int i = 0; i < frames.Length; i++)
                {
                    if (frames[i].IsPostfix)
                        continue;
                    for (int j = i + 1; j < frames.Length; j++)
                    {
                        if (frames[i].TypeName == frames[j].TypeName && frames[i].MethodName == frames[j].MethodName && frames[j].IsPostfix)
                        {
                            var data = frames[j];
                            var idx = datas.FindIndex(x => x.TypeName == data.TypeName && x.MethodName == data.MethodName);
                            if (idx == -1)
                            {
                                datas.Add(new FrameUIData()
                                {
                                    TypeName = data.TypeName,
                                    MethodName = data.MethodName,
                                    TotalTimeUSec = data.TimeUSec - frames[i].TimeUSec,
                                    TotalCalls = 1,
                                });
                            }
                            else
                            {
                                var frame = datas[idx];
                                frame.TotalTimeUSec += data.TimeUSec - frames[i].TimeUSec;
                                frame.TotalCalls++;
                                datas[idx] = frame;
                            }
                            break;
                        }
                    }
                }

                tree.Clear();
                var root = tree.CreateItem();
                foreach (var data in datas.OrderByDescending(x => x.TotalTimeUSec))
                {
                    var item = tree.CreateItem(root);
                    item.SetText(0, data.TypeName);
                    item.SetText(1, data.MethodName);
                    item.SetText(2, (data.TotalTimeUSec / 1000f).ToString());
                    item.SetText(3, data.TotalCalls.ToString());
                }
            }
        }
    }

    [Tool]
    public partial class CustomEditorProfiler : EditorDebuggerPlugin
    {
        public ProfilerEditorUI profiler;

        public override void _SetupSession(int sessionId)
        {
            Del();
            var session = GetSession(sessionId);
            session.ToggleProfiler(ProfilerName, true);
        }

        ~CustomEditorProfiler()
        {
            Del();
        }

        public void Del()
        {
            foreach (var item in GetSessions())
            {
                if (IsInstanceValid(profiler))
                {
                    item.As<EditorDebuggerSession>().RemoveSessionTab(profiler);
                    profiler.Free();
                }
            }
        }

        public void CreateTab(EditorDebuggerSession session)
        {
            if (IsInstanceValid(profiler))
            {
                session.RemoveSessionTab(profiler);
                profiler.Free();
            }
            profiler = new ProfilerEditorUI();
            session.AddSessionTab(profiler);
        }

        public override bool _HasCapture(string capture)
        {
            return capture == ProfilerPrefix;
        }

        public override bool _Capture(string message, Godot.Collections.Array data, int sessionId)
        {
            if (message == ProfilerName)
            {
                if (!IsInstanceValid(profiler))
                {
                    CreateTab(GetSession(sessionId));
                }
                var arr = data.Select(x => new CSharpProfiler.FrameData(x.AsGodotArray())).ToArray();
                profiler.SetFrameData(arr);
                return true;
            }
            return false;
        }
    }

    public const string ProfilerPrefix = "CSharp";
    public const string ProfilerName = "CSharp:Profiler";

    public CustomEditorProfiler debugger;
    #endif

    public override void _EnterTree()
    {
        // debugger = new CustomEditorProfiler();
        // AddDebuggerPlugin(debugger);
        AddAutoloadSingleton("CSharpProfiler", "res://addons/csharp_profiler/CSharpProfilerNode.cs");
    }

    public override void _ExitTree()
    {
        RemoveAutoloadSingleton("CSharpProfiler");
        // RemoveDebuggerPlugin(debugger);
        // debugger.Del();
        // debugger = null;
    }
}
#endif
