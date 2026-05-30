using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class AsyncTools : Node
{
    public static AsyncTools Instance { get; private set; }

    public class State
    {
        public Node Bind;
        public CancellationToken Token;
        public IEnumerator Enumerator;
        public Thread NewThread = null;
        public bool Done = false;
        public Action<Exception> OnException;

        public bool Tick(double delta)
        {
            if (Done)
                return true;
            try
            {
                Token.ThrowIfCancellationRequested();
                bool shouldMove = false;
                switch (Enumerator.Current)
                {
                    case null:
                        shouldMove = true;
                        break;
                    case WaitForSeconds sec:
                        shouldMove = sec.Tick(this, delta);
                        break;
                    case SwitchToNewThread toNewThread:
                        shouldMove = toNewThread.Tick(this, delta);
                        return true;
                    case SwitchToMainThread toMainThread:
                        shouldMove = toMainThread.Tick(this, delta);
                        if (!shouldMove)
                            return true;
                        break;
                }
                if (shouldMove)
                {
                    if (!GodotObject.IsInstanceValid(Bind))
                    {
                        Done = true;
                        return true;
                    }
                    if (!Enumerator.MoveNext())
                    {
                        Done = true;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                throw;
                Done = true;
                OnException?.Invoke(e);
                return true;
            }
        }
    }

    private static List<State> states = new List<State>();
    private static ConcurrentQueue<KeyValuePair<Action, TaskCompletionSource>> physicsTicks = new ConcurrentQueue<KeyValuePair<Action, TaskCompletionSource>>();

    public override void _EnterTree()
    {
        if (IsInstanceValid(Instance))
            QueueFree();
        else
            Instance = this;
    }

    public override void _Process(double delta)
    {
        if (Instance != this)
            return;
        Tick(delta, false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Instance != this)
            return;
        while (physicsTicks.TryDequeue(out var call))
        {
            call.Key.Invoke();
            call.Value.SetResult();
        }
    }

    private static void Tick(double delta, bool thread)
    {
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] == null || states[i].Done)
            {
                continue;
            }
            if (states[i].NewThread == null)
            {
                states[i].Tick(delta);
            }
        }
        if (!thread)
            states.RemoveAll(x => x == null || x.Done);
    }

    public static async Task RunPhysics(Action action)
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        physicsTicks.Enqueue(new KeyValuePair<Action, TaskCompletionSource>(action, tcs));
        await tcs.Task;
    }

    public static async Task RunAsync(Node bind, IEnumerator func)
    {
        State state = new State()
        {
            Bind = bind,
            Enumerator = func,
            OnException = e => throw e,
        };
        states.Add(state);
        while (!state.Done)
        {
            await Task.Delay(100);
        }
    }

    public static void Run(Node bind, IEnumerator func, CancellationToken token)
    {
        states.Add(new State()
        {
            Bind = bind,
            Token = token,
            Enumerator = func,
            OnException = e => throw e,
        });
    }
}

public class SwitchToMainThread
{
    public SwitchToMainThread()
    {
    }

    public bool Tick(AsyncTools.State state, double delta)
    {
        bool v = state.NewThread == null;
        state.NewThread = null;
        return v;
    }
}

public class SwitchToNewThread
{
    public Thread NewThread { get; private set; }

    public SwitchToNewThread(ThreadPriority priority = ThreadPriority.Normal)
    {
        NewThread = new Thread(Run);
        // NewThread.IsBackground = false;
        NewThread.Priority = priority;
    }

    private void Run(object obj)
    {
        if (obj is AsyncTools.State state && GodotObject.IsInstanceValid(state.Bind))
        {
            if (!state.Enumerator.MoveNext())
                return;
            int ms = 1;
            double delta = ms / 1000d;
            while (!state.Tick(delta))
            {
                Thread.Sleep(ms);
            }
        }
    }

    public bool Tick(AsyncTools.State state, double delta)
    {
        state.NewThread = NewThread;
        NewThread.Start(state);
        return false;
    }
}

public class WaitForSeconds
{
    public double Time { get; private set; }
    public double TimeLeft { get; private set; }

    public WaitForSeconds(double time)
    {
        Time = time;
        TimeLeft = Time;
    }

    public bool Tick(AsyncTools.State state, double delta)
    {
        TimeLeft -= delta;
        return TimeLeft <= 0d;
    }
}