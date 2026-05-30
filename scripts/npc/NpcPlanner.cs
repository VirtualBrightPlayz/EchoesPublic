using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

[GlobalClass]
public partial class NpcPlanner : Node
{
    [Export] public string[] StateNames = Array.Empty<string>();
    [Export] public Godot.Collections.Array<NpcAction> Actions = new();
    [Export] public Godot.Collections.Dictionary<string, bool> CurrentWorldState = new();
    [Export] public Godot.Collections.Dictionary<string, bool> DesiredWorldState = new();
    [Export] public long CurrentState;
    [Export] public long DesiredState;
    [Export] public long DesiredStateMask;
    [Export] public bool PlanDirty = true;
    public Dictionary<string, bool> inputs = new();
    public Dictionary<string, long> stateLookup = new();
    public Dictionary<long, NpcAction> actionLookup = new();
    public AStar2D astar = new();
    public long AllStateBits;

    public struct TreeState : IEquatable<TreeState>
    {
        public NpcAction Action;
        public string[] Names;
        public long WorldState;
        public long Mask;

        public TreeState(NpcAction act, string[] names, long state, long mask)
        {
            Action = act;
            Names = names;
            WorldState = state;
            Mask = mask;
        }

        public bool Equals(TreeState other)
        {
            // if (Action == other.Action)
            {
                return WorldState == other.WorldState;
                // return (WorldState & other.WorldState) != 0;
                // return (WorldState & other.Mask) == (other.WorldState & Mask);
                bool found = false;
                for (int i = 0; i < Names.Length; i++)
                {
                    bool hasState = (WorldState & (1L << i)) != 0;
                    bool otherHasState = (other.WorldState & (1L << i)) != 0;
                    if (hasState != otherHasState)
                    {
                        found = true;
                        break;
                    }
                }
                return !found;
            }
            return false;
        }
    }

    public class TreeComparer : IEqualityComparer<TreeState>
    {
        public bool Equals(TreeState x, TreeState y)
        {
            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] TreeState obj)
        {
            return obj.WorldState.GetHashCode();
        }
    }

    public class TreeStateOld
    {
        public Dictionary<string, bool> WorldState;
        public NpcAction Action;
        public long Id;
        public int Iter;

        public TreeStateOld(NpcAction action, long id, IDictionary<string, bool> state, int iter)
        {
            Action = action;
            Id = id;
            Iter = iter;
            WorldState = new(state);
            foreach (var kvp in action.PostEffects)
            {
                WorldState[kvp.Key] = kvp.Value;
            }
        }
    }

    public bool GetState(string stateName)
    {
        int idx = Array.IndexOf(StateNames, stateName);
        if (idx != -1)
        {
            return (CurrentState & (1L << idx)) != 0;
        }
        else
        {
            return false;
        }
    }

    public void SetState(string stateName, bool value, bool markChangesDirty = true)
    {
        int idx = Array.IndexOf(StateNames, stateName);
        if (idx != -1)
        {
            bool curValue = (CurrentState & (1L << idx)) != 0;
            if (value != curValue && markChangesDirty)
            {
                PlanDirty = true;
            }
            if (value)
            {
                CurrentState |= 1L << idx;
            }
            else
            {
                CurrentState &= ~(1L << idx);
            }
        }
        else
        {
            inputs[stateName] = value;
        }
    }

    public void UpdateDesiredState()
    {
        DesiredState = GetStateFor(DesiredWorldState);
        DesiredStateMask = GetStateMaskFor(DesiredWorldState.Keys);
    }

    public void Setup()
    {
        PlanDirty = true;
        actionLookup.Clear();
        stateLookup.Clear();
        astar.Clear();
        for (int i = 0; i < StateNames.Length; i++)
        {
            stateLookup[StateNames[i]] = 1L << i;
        }
        CurrentState = GetStateFor(CurrentWorldState);
        DesiredState = GetStateFor(DesiredWorldState);
        DesiredStateMask = GetStateMaskFor(DesiredWorldState.Keys);
        AllStateBits = 1L << (StateNames.Length - 1);
        // names.Clear();
        // names.UnionWith(CurrentWorldState.Keys);
        // names.UnionWith(DesiredWorldState.Keys);
        // for (int i = 0; i < Actions.Count; i++)
        // {
            // names.UnionWith(Actions[i].PreConditions.Keys);
            // names.UnionWith(Actions[i].PostEffects.Keys);
        // }
        /*
        HashSet<string> names = new HashSet<string>();
        long id = 0;
        astar.AddPoint(id, new Vector2(0f, 0f));
        actionLookup[id] = null;
        id++;
        Queue<TreeState> stateQueue = new Queue<TreeState>();
        for (int i = 0; i < Actions.Count; i++)
        {
            astar.AddPoint(id, new Vector2(1f, 0f), Actions[i].Cost);
            actionLookup[id] = Actions[i];
            stateQueue.Enqueue(new TreeState(Actions[i], id, CurrentWorldState, 1));
            names.UnionWith(Actions[i].PreConditions.Keys);
            names.UnionWith(Actions[i].PostEffects.Keys);
            id++;
        }

        while (stateQueue.TryDequeue(out TreeState state))
        {
            if (state.Iter >= 4 && id > 1000)
            {
                Log.PrintErr("NpcPlanner is recursive.");
                break;
            }
            stateLookup.Add(state);
            for (int i = 0; i < Actions.Count; i++)
            {
                bool areConditionsMet = true;
                foreach (var kvp in Actions[i].PreConditions)
                {
                    foreach (var kvp2 in state.WorldState)
                    {
                        if (kvp.Key == kvp2.Key && kvp.Value != kvp2.Value)
                        {
                            areConditionsMet = false;
                            break;
                        }
                    }
                }
                if (areConditionsMet)
                {
                    astar.AddPoint(id, new Vector2(1f * state.Iter, 0f), Actions[i].Cost);
                    actionLookup[id] = Actions[i];
                    astar.ConnectPoints(state.Id, id, false);
                    // stateQueue.Enqueue(new TreeState(Actions[i], id, state.WorldState, state.Iter + 1));
                    id++;
                }
            }
        }
        */
    }

    /*
    private long GetIdForState(IDictionary<string, bool> dict)
    {
        for (int i = 0; i < stateLookup.Count; i++)
        {
            bool areConditionsMet = true;
            foreach (var kvp in stateLookup[i].WorldState)
            {
                foreach (var kvp2 in dict)
                {
                    if (kvp.Key == kvp2.Key && kvp.Value != kvp2.Value)
                    {
                        areConditionsMet = false;
                        break;
                    }
                }
            }
            if (areConditionsMet)
            {
                return stateLookup[i].Id;
            }
        }
        return -1;
    }
    */

    private long GetStateFor(IDictionary<string, bool> dict)
    {
        long val = 0L;
        foreach (var kvp in dict)
        {
            if (!kvp.Value)
            {
                continue;
            }
            int idx = Array.IndexOf(StateNames, kvp.Key);
            if (idx != -1)
            {
                val |= 1L << idx;
            }
        }
        return val;
    }

    private long GetStateMaskFor(ICollection<string> list)
    {
        long val = 0L;
        foreach (var name in list)
        {
            int idx = Array.IndexOf(StateNames, name);
            if (idx != -1)
            {
                val |= 1L << idx;
            }
        }
        return val;
    }

    public bool AreInputsValid(IDictionary<string, bool> dict)
    {
        foreach (var kvp in dict)
        {
            if (inputs.TryGetValue(kvp.Key, out bool value) && value != kvp.Value)
            {
                return false;
            }
        }
        return true;
    }

    private long ApplyState(long pre, long post, long mask)
    {
        return (pre & ~mask) | (post & mask);
    }

    private void GetNextStates(TreeState state, List<TreeState> nextStates)
    {
        nextStates.Clear();
        for (int i = 0; i < Actions.Count; i++)
        {
            long preState = GetStateFor(Actions[i].PreConditions);
            long preStateMask = GetStateMaskFor(Actions[i].PreConditions.Keys);
            if ((state.WorldState & preStateMask) == preState /*&& AreInputsValid(Actions[i].PreConditions)*/)
            {
                nextStates.Add(new TreeState(Actions[i], StateNames, ApplyState(state.WorldState, GetStateFor(Actions[i].PostEffects), GetStateMaskFor(Actions[i].PostEffects.Keys)), AllStateBits));
            }
        }
    }

    private float GuessCost(TreeState from, TreeState to)
    {
        int guess = 0;
        for (int i = 0; i < from.Names.Length; i++)
        {
            bool hasState = (from.WorldState & (1L << i)) != 0;
            bool otherHasState = (to.WorldState & (1L << i)) != 0;
            if (hasState != otherHasState)
            {
                guess++;
            }
        }
        return guess * from.Names.Length * from.Names.Length;
    }

    private List<TreeState> tempStates = new();

    public bool NextAction(NpcAction current)
    {
        CurrentState = ApplyState(CurrentState, GetStateFor(current.PostEffects), GetStateMaskFor(current.PostEffects.Keys));
        if ((CurrentState & DesiredStateMask) == DesiredState)
        {
            return true;
        }
        return false;
    }

    public NpcAction[] GetPlan()
    {
        PlanDirty = false;
        TreeState start = new(null, StateNames, CurrentState, AllStateBits);
        TreeState goal = new(null, StateNames, DesiredState, DesiredStateMask);
        List<TreeState> openSetStates = new List<TreeState>();
        PriorityQueue<TreeState, float> openSet = new PriorityQueue<TreeState, float>();
        openSet.Enqueue(start, float.PositiveInfinity);
        openSetStates.Add(start);
        Dictionary<TreeState, TreeState> cameFrom = new Dictionary<TreeState, TreeState>(new TreeComparer());

        Dictionary<TreeState, float> gScore = new Dictionary<TreeState, float>(new TreeComparer());
        gScore[start] = 0f;

        Dictionary<TreeState, float> fScore = new Dictionary<TreeState, float>(new TreeComparer());
        fScore[start] = GuessCost(start, goal);

        int iter = 250;
        while (openSet.TryDequeue(out TreeState current, out float priority) && --iter > 0)
        {
            openSetStates.Remove(current);
            // GD.PrintS(current.Action?.ResourceName, current.WorldState, current.Mask);
            if ((current.WorldState & goal.Mask) == goal.WorldState)
            {
                // GD.Print("found");
                List<NpcAction> plan = new List<NpcAction>();
                if (IsInstanceValid(current.Action))
                    plan.Add(current.Action);
                while (cameFrom.TryGetValue(current, out TreeState next))
                {
                    current = next;
                    if (IsInstanceValid(current.Action))
                        plan.Add(current.Action);
                }
                return plan.ToArray();
            }

            GetNextStates(current, tempStates);
            for (int i = 0; i < tempStates.Count; i++)
            {
                // GD.Print("Next ", tempStates[i].Action?.ResourceName);
                TreeState neighbor = tempStates[i];
                float score = gScore[current] + tempStates[i].Action.Cost;
                if (!gScore.ContainsKey(neighbor) || score < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = score;
                    fScore[neighbor] = score + GuessCost(neighbor, goal);
                    bool found = false;
                    for (int j = 0; j < openSetStates.Count; j++)
                    {
                        if (openSetStates[j].Equals(neighbor))
                        {
                            found = true;
                            break;
                        }
                    }
                    // if (!openSetStates.Contains(neighbor))
                    if (!found)
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                        openSetStates.Add(neighbor);
                    }
                }
            }
        }
        if (iter <= 0 && openSet.TryDequeue(out TreeState current2, out float priority2))
        {
            List<NpcAction> plan = new List<NpcAction>();
            while (cameFrom.TryGetValue(current2, out TreeState next))
            {
                current2 = next;
                if (IsInstanceValid(current2.Action))
                    plan.Add(current2.Action);
            }
            return plan.ToArray();
        }

        /*
        long curId = GetIdForState(CurrentWorldState);
        long dstId = GetIdForState(DesiredWorldState);
        long[] path = astar.GetIdPath(curId, dstId, true);
        NpcAction[] plan = new NpcAction[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            plan[i] = actionLookup[path[i]];
        }
        */
        return Array.Empty<NpcAction>();
    }
}
