using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class GraphGenerator : Node3D
{
    public class GraphGenNode
    {
        public Vector3I position;
        public string name;
        public PresetRoom room;
        public List<GraphGenNode> connections = new List<GraphGenNode>();
    }

    public const string NAME_PLACEHOLDER = "Placeholder";

    private RandomNumberGenerator rng;
    private List<GraphGenNode> nodes = new List<GraphGenNode>();
    private List<GraphGenNode> topLevel = new List<GraphGenNode>();
    private MapGraph graph;

    [Export(PropertyHint.File)]
    public string graphFile;
    [Export]
    public Node target;
    [Export]
    public PresetRoom[] rooms;
    [Export]
    public PresetRoom[] interestRooms;
    [Export]
    public GameData gameData;
    [Export]
    public float mapSize = 20.8f;
    [Export]
    public float spread = 1f;
    [Export]
    public int pointCount = 10;

    public readonly Vector3I[] directions = new Vector3I[]
    {
        Vector3I.Forward,
        Vector3I.Right,
        Vector3I.Back,
        Vector3I.Left,
        // Vector3I.Up,
        // Vector3I.Down,
    };
    public readonly Dictionary<PresetRoom.RoomClass, List<int>> roomClassToDirections = new Dictionary<PresetRoom.RoomClass, List<int>>()
    {
        { PresetRoom.RoomClass.Endoff, new() { 0 } },
        { PresetRoom.RoomClass.Corner, new() { 0, 1 } },
        { PresetRoom.RoomClass.Hall, new() { 0, 2 } },
        { PresetRoom.RoomClass.TRoom, new() { 0, 1, 2 } },
        { PresetRoom.RoomClass.XRoom, new() { 0, 1, 2, 3 } },
        // { PresetRoom.RoomClass.Endoff, new() { Vector3I.Forward } },
        // { PresetRoom.RoomClass.Corner, new() { Vector3I.Forward, Vector3I.Right } },
        // { PresetRoom.RoomClass.Hall, new() { Vector3I.Forward, Vector3I.Back } },
        // { PresetRoom.RoomClass.TRoom, new() { Vector3I.Forward, Vector3I.Right, Vector3I.Back } },
        // { PresetRoom.RoomClass.XRoom, new() { Vector3I.Forward, Vector3I.Right, Vector3I.Back, Vector3I.Left } },
    };

    public readonly Vector3[] rotations = new Vector3[]
    {
        Vector3.Up * Mathf.DegToRad(180),
        Vector3.Up * Mathf.DegToRad(90),
        Vector3.Up * Mathf.DegToRad(0),
        Vector3.Up * Mathf.DegToRad(270),
    };
    public readonly Vector3[] rotationsTroom = new Vector3[]
    {
        Vector3.Up * Mathf.DegToRad(0),
        Vector3.Up * Mathf.DegToRad(270),
        Vector3.Up * Mathf.DegToRad(180),
        Vector3.Up * Mathf.DegToRad(90),
    };
    public readonly ERoomDirectionFlags[] directionFlags = new ERoomDirectionFlags[]
    {
        ERoomDirectionFlags.NegativeZ,
        ERoomDirectionFlags.PositiveX,
        ERoomDirectionFlags.PositiveZ,
        ERoomDirectionFlags.NegativeX,
    };
    public Func<PackedScene, Vector3, Vector3, Node> customSpawn = null;
    public List<Node> allSpawned = new List<Node>();

    public override void _Ready()
    {
        // uint i = GD.Randi();
        // i = 2589413777;
        // i = 1045588732;
        // i = 616249541;
        // Log.Print(i);
        // GenerateOld(i, MapGraph.ParseMd(FileAccess.GetFileAsString(graphFile), true));
    }

    public bool GenerateFile(ulong seed, string graphFile)
    {
        return GenerateOld(seed, MapGraph.ParseMd(FileAccess.GetFileAsString(graphFile), true));
    }

    public void Generate(ulong seed, string mapBase)
    {
        if (IsInstanceValid(gameData))
        {
            rooms = gameData.Rooms.Where(x => x.isGeneric).ToArray();
            interestRooms = gameData.Rooms.Where(x => !x.isGeneric).ToArray();
        }
        nodes.Clear();
        topLevel.Clear();
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        graph = MapGraph.ParseMd(mapBase, true);

        AStar2D pathfind = new AStar2D();

        // start with points of interest, place them at random
        Vector2[] points = new Vector2[pointCount];
        for (int i = 0; i < points.Length; i++)
        {
            int x = (int)(rng.Randf() * spread);
            int z = (int)(rng.Randf() * spread);
            points[i] = new Vector2(x, z);
            pathfind.AddPoint(i, points[i]);
        }

        // connect them via triangulation
        int[] tris = Geometry2D.TriangulateDelaunay(points);
        Dictionary<int, List<int>> lookup = new Dictionary<int, List<int>>();
        for (int i = 0; i < points.Length; i++)
        {
            lookup[i] = new List<int>();
        }
        List<int> pairs = new List<int>();
        List<int> used = new List<int>();
        for (int i = 0; i < tris.Length; i+=3)
        {
            int pt0 = tris[i];
            int pt1 = tris[i+1];
            int pt2 = tris[i+2];

            lookup[pt0].Add(pt1);
            lookup[pt0].Add(pt2);

            lookup[pt1].Add(pt0);
            lookup[pt1].Add(pt2);

            lookup[pt2].Add(pt1);
            lookup[pt2].Add(pt0);

            if (i == 0)
            {
                used.Add(pt0);
                used.Add(pt1);
                used.Add(pt2);

                pairs.Add(pt0);
                pairs.Add(pt1);

                pairs.Add(pt0);
                pairs.Add(pt2);
            }
            else
            {
                for (int j = 0; j < 3; j++)
                {
                    int id = tris[i+j];
                    if (used.Contains(id))
                    {
                        if (!used.Contains(pt0))
                        {
                            pairs.Add(id);
                            pairs.Add(pt0);
                            used.Add(pt0);
                        }
                        else if (!used.Contains(pt1))
                        {
                            pairs.Add(id);
                            pairs.Add(pt1);
                            used.Add(pt1);
                        }
                        else if (!used.Contains(pt2))
                        {
                            pairs.Add(id);
                            pairs.Add(pt2);
                            used.Add(pt2);
                        }
                    }
                }
                /*
                int? id = null;
                if (pairs[^1] == pt0)
                {
                    id = pt0;
                }
                else if (pairs[^1] == pt1)
                {
                    id = pt1;
                }
                else if (pairs[^1] == pt2)
                {
                    id = pt2;
                }
                if (id.HasValue)
                {
                    if (!used.Contains(pt0))
                    {
                        pairs.Add(id.Value);
                        pairs.Add(pt0);
                        used.Add(id.Value);
                    }
                    else if (!used.Contains(pt1))
                    {
                        pairs.Add(id.Value);
                        pairs.Add(pt1);
                        used.Add(id.Value);
                    }
                    else if (!used.Contains(pt2))
                    {
                        pairs.Add(id.Value);
                        pairs.Add(pt2);
                        used.Add(id.Value);
                    }
                }
                */
            }
            continue;
            pathfind.ConnectPoints(pt0, pt1);
            pathfind.ConnectPoints(pt1, pt2);
            pathfind.ConnectPoints(pt2, pt0);
        }

        for (int i = 0; i < points.Length; i++)
        {
            lookup[i] = lookup[i].Distinct().OrderBy(x => points[x].DistanceSquaredTo(points[i])).ToList();
        }

        // GD.Print(new Godot.Collections.Dictionary<int, int[]>(lookup.ToDictionary(x => x.Key, x => x.Value.ToArray())));

        Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();
        Queue<int> toExpand = new Queue<int>();
        toExpand.Enqueue(0);
        while (toExpand.TryDequeue(out int next))
        {
            if (!connections.ContainsKey(next))
            {
                connections[next] = new List<int>();
            }
            for (int i = 0; i < lookup[next].Count; i++)
            {
                int pointIndex = lookup[next][i];
                if (connections.ContainsKey(pointIndex))
                {
                    continue;
                }
                connections[pointIndex] = new List<int>();
                connections[next].Add(pointIndex);
                toExpand.Enqueue(pointIndex);
                pathfind.ConnectPoints(next, pointIndex);
            }
        }

        // GD.Print(new Godot.Collections.Dictionary<int, int[]>(connections.ToDictionary(x => x.Key, x => x.Value.ToArray())));

        for (int i = 0; i < pairs.Count; i+=2)
        {
            // pathfind.ConnectPoints(pairs[i], pairs[i+1]);
            // Vector2 pos0 = pathfind.GetPointPosition(pairs[i]);
            // Vector2 pos1 = pathfind.GetPointPosition(pairs[i+1]);
            // AddPath((Vector2I)pos0, (Vector2I)pos1);
        }

        for (int i = 0; i < points.Length; i++)
        {
            // GraphGenNode node = AddPlaceholder((Vector2I)points[i], true);
            // node.name = graph.nodes[i].name;
            // node.room = FindRoomByName(node.name);
        }

        for (int i = 0; i < points.Length; i++)
        {
            if (connections.ContainsKey(i))
            {
                Vector2I from = (Vector2I)points[i];
                for (int j = 0; j < connections[i].Count; j++)
                {
                    int other = connections[i][j];
                    if (other == i)
                    {
                        continue;
                    }
                    Vector2I to = (Vector2I)points[other];
                    AddPath((Vector2I)points[i], (Vector2I)points[other]);
                    // GraphGenNode node0 = GetNodeAt(new Vector3I(from.X, 0, from.Y));
                    // GraphGenNode node1 = GetNodeAt(new Vector3I(to.X, 0, to.Y));
                }
            }
        }

        // connect everything to be one map
        for (int i = 0; i < points.Length; i++)
        {
            Vector2I from = (Vector2I)points[i];
            GraphGenNode node0 = GetNodeAt(new Vector3I(from.X, 0, from.Y));
            // ConnectNodes(node0, false);
        }
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3I[] dirs = GetConnectedDirectionsSorted(nodes[i]);
            PresetRoom.RoomClass cl = GetRoomClass(dirs);
            if (nodes[i].connections.Count == 0 || nodes[i].connections.Count > 4 || cl == PresetRoom.RoomClass.Endoff)
            {
                ConnectNodes(nodes[i], false);
            }
        }

        /*for (int i = 0; i < graph.nodes.Count; i++)
        {
            MapGraph.MapGraphNode graphNode0 = graph.nodes[i];
            for (int j = 0; j < graph.nodes.Count; j++)
            {
                MapGraph.MapGraphNode graphNode1 = graph.nodes[j];
                /*Queue<int> toPath = new Queue<int>();
                toPath.Enqueue(i);
                while (toPath.TryDequeue(out int next))
                {
                    for (int k = 0; k < connections[next].Count; k++)
                    {
                        toPath.Enqueue(connections[next][k]);
                        AddPath((Vector2I)points[next], (Vector2I)points[connections[next][k]]);
                    }
                }
                * /
                long[] path = pathfind.GetIdPath(i, j);
                // GD.Print(new Godot.Collections.Array<long>(path));
                for (int k = 0; k < path.Length - 1; k++)
                {
                    Vector2 pos0 = points[path[k]]; //pathfind.GetPointPosition(path[k]);
                    Vector2 pos1 = points[path[k+1]]; //pathfind.GetPointPosition(path[k+1]);
                    // AddPath((Vector2I)pos0, (Vector2I)pos1);
                }
            }
        }*/
        List<MapGraph.MapGraphNode> graphNodes = graph.nodes.ToList();
        // for (int i = 0; i < graph.nodes.Count; i++)
        for (int i = 0; i < nodes.Count; i++)
        {
            // MapGraph.MapGraphNode graphNode = graph.nodes[i];
            // Vector2 nodePos = points[i];
            GraphGenNode node = nodes[i];
            if (graphNodes.Count == 0)
                break;
            MapGraph.MapGraphNode graphNode = graphNodes[(int)(rng.Randi() % graphNodes.Count)];
            graphNodes.Remove(graphNode);
            // GraphGenNode node = GetNodeAt(new Vector3I((int)nodePos.X, 0, (int)nodePos.Y));
            if (node != null)
            {
                string name = graphNode.name;
                PresetRoom room = FindRoomByName(name);
                Vector3I[] dirs = GetConnectedDirectionsSorted(node);
                PresetRoom.RoomClass cl = GetRoomClass(dirs);
                if (room.type == cl)
                {
                    node.name = name;
                    node.room = room;
                }
            }
        }
        // merge placeholders if possible
        for (int i = 0; i < nodes.Count; i++)
        {
            break;
            if (nodes[i].name != NAME_PLACEHOLDER)
                continue;
            Vector3I[] dirs = GetConnectedDirectionsSorted(nodes[i]);
            PresetRoom.RoomClass cl = GetRoomClass(dirs);
            if (cl == PresetRoom.RoomClass.Hall)
            {
                ConnectNodes(nodes[i], true);
            }
        }

        // done
        SpawnRooms();
        Display();
    }

    private void ConnectNodes(GraphGenNode node, bool placeholderOnly)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3I otherPos = node.position + directions[i];
            GraphGenNode other = GetNodeAt(otherPos);
            if (other != null && (!placeholderOnly || other.name == NAME_PLACEHOLDER))
            {
                if (!other.connections.Contains(node))
                    other.connections.Add(node);
                if (!node.connections.Contains(other))
                    node.connections.Add(other);
            }
        }
    }

    private void AddPath(Vector2I from, Vector2I to)
    {
        GraphGenNode node = GetNodeAt(new Vector3I(from.X, 0, from.Y));
        if (node == null)
        {
            node = AddPlaceholder(from, true);
        }
        GraphGenNode otherNode = GetNodeAt(new Vector3I(to.X, 0, to.Y));
        if (otherNode == null)
        {
            otherNode = AddPlaceholder(to, true);
        }
        // if (rng.Randi() % 2 == 0)
        if (true)
        {
            // GD.Print("Start");
            for (int x = from.X; x != to.X; x+=Mathf.Sign(to.X - from.X))
            {
                // GD.PrintS(x, from.Y);
                GraphGenNode child = AddPlaceholder(new Vector2I(x, from.Y), true);
                // GD.Print(child);
                if (child != null && node != null)
                {
                    if (!child.connections.Contains(node))
                        child.connections.Add(node);
                    if (!node.connections.Contains(child))
                        node.connections.Add(child);
                }
                node = child;
            }
            {
                // GD.PrintS(to.X, from.Y);
                GraphGenNode child = AddPlaceholder(new Vector2I(to.X, from.Y), true);
                // GD.Print(child);
                if (child != null && node != null)
                {
                    if (!child.connections.Contains(node))
                        child.connections.Add(node);
                    if (!node.connections.Contains(child))
                        node.connections.Add(child);
                }
                node = child;
            }
            for (int y = from.Y; y != to.Y; y+=Mathf.Sign(to.Y - from.Y))
            {
                // GD.PrintS(to.X, y);
                GraphGenNode child = AddPlaceholder(new Vector2I(to.X, y), true);
                // GD.Print(child);
                if (child != null && node != null)
                {
                    if (!child.connections.Contains(node))
                        child.connections.Add(node);
                    if (!node.connections.Contains(child))
                        node.connections.Add(child);
                }
                node = child;
            }
            if (node != null && otherNode != null)
            {
                otherNode.connections.Add(node);
                node.connections.Add(otherNode);
            }
            // GD.Print("End");
        }
        else
        {
            for (int y = from.Y; y != to.Y; y+=Mathf.Sign(to.Y - from.Y))
            {
                GraphGenNode child = AddPlaceholder(new Vector2I(from.X, y), false);
                // child.connections.Add(node);
                // node.connections.Add(child);
                node = child;
            }
            for (int x = from.X; x != to.X; x+=Mathf.Sign(to.X - from.X))
            {
                GraphGenNode child = AddPlaceholder(new Vector2I(x, to.Y), false);
                // child.connections.Add(node);
                // node.connections.Add(child);
                node = child;
            }
        }
    }

    private GraphGenNode AddPlaceholder3D(Vector3I position, GraphGenNode parent)
    {
        GraphGenNode node = new GraphGenNode();
        node.name = NAME_PLACEHOLDER;
        node.room = null;
        node.position = position;
        if (parent != null)
        {
            node.connections.Add(parent);
            parent.connections.Add(node);
        }
        nodes.Add(node);
        return node;
    }

    private GraphGenNode AddPlaceholder(Vector2I position, bool connectNear)
    {
        Vector3I pos = new Vector3I(position.X, 0, position.Y);
        GraphGenNode node = GetNodeAt(pos);
        if (node != null)
        {
            // if (connectNear)
            //     return node;
            // else
                return null;
        }
        node = new GraphGenNode();
        node.name = NAME_PLACEHOLDER;
        node.room = null;
        node.position = pos;
        if (false)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3I otherPos = node.position + directions[i];
                GraphGenNode other = GetNodeAt(otherPos);
                if (other != null /*&& other.name == NAME_PLACEHOLDER*/)
                {
                    other.connections.Add(node);
                    node.connections.Add(other);
                }
            }
        }
        /*
        if (node.connections.Count > 2)
        {
            GraphGenNode other = node.connections[^1];
            other.connections.Remove(node);
            node.connections.Remove(other);
        }
        */
        nodes.Add(node);
        return node;
    }

    private GraphGenNode SpawnNodeEndoff(int idx, Vector2 point)
    {
        if (nodes.FindAll(x => x.name == graph.nodes[idx].name).Count >= 1)
            return null;
        GraphGenNode node = new GraphGenNode();
        node.name = graph.nodes[idx].name;
        node.room = FindRoomByName(node.name);
        if (node.room.type != PresetRoom.RoomClass.Endoff)
            return null;
        int x = (int)point.X;
        int z = (int)point.Y;
        node.position = new Vector3I(x, 0, z);
        nodes.Add(node);
        topLevel.Add(node);
        return node;
    }

    public bool GenerateOld(ulong seed, MapGraph mapBase)
    {
        if (IsInstanceValid(gameData))
        {
            rooms = gameData.Rooms.Where(x => x.isGeneric).ToArray();
            interestRooms = gameData.Rooms.Where(x => !x.isGeneric).ToArray();
        }
        nodes.Clear();
        topLevel.Clear();
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        graph = mapBase;

        // start
        GraphGenNode placeholder = AddPlaceholder3D(Vector3I.Zero, null);
        List<(GraphGenNode, int)> queue = new List<(GraphGenNode, int)>();
        queue.Add((placeholder, 0));
        queue.Add((placeholder, 1));
        queue.Add((placeholder, 2));
        queue.Add((placeholder, 3));
        int iter = 0;
        while (queue.Count != 0 && iter++ < 1000)
        {
            int idx = (int)(rng.Randi() % queue.Count);
            var result = queue[idx];
            if (iter > graph.nodes.Count)
            {
                for (int i = 0; i < nodes.Count; i++)
                // int i = (int)(rng.Randi() % nodes.Count);
                {
                    if (!IsInstanceValid(nodes[i].room))
                    {
                        Vector3I[] dirs = GetConnectedDirectionsSorted(nodes[i]);
                        PresetRoom.RoomClass cl = GetRoomClass(dirs);
                        PresetRoom[] roomArr = interestRooms.Where(x => x.type == cl && (nodes.FindAll(y => y.room == x).Count == 0)).ToArray();
                        if (roomArr.Length != 0)
                        {
                            nodes[i].room = roomArr[rng.Randi() % roomArr.Length];
                            nodes[i].name = nodes[i].room.ResourceName;
                        }
                    }
                }
            }
            PresetRoom[] unspawnedRooms = interestRooms.Where(x => nodes.FindAll(y => y.room == x).Count == 0).ToArray();
            if (unspawnedRooms.Length == 0)
                break;
            GraphGenNode node = GetNodeAt(result.Item1.position + directions[result.Item2]);
            if (node == null && !IsInstanceValid(result.Item1.room))
            {
                placeholder = AddPlaceholder3D(result.Item1.position + directions[result.Item2], result.Item1);
                // int rngDir = rng.Randi() % 3 == 0 ? 0 : (rng.Randi() % 2 == 0 ? 1 : 3);
                // queue.Enqueue((placeholder, (result.Item2 + rngDir) % 4));
                queue.Add((placeholder, 0));
                queue.Add((placeholder, 1));
                queue.Add((placeholder, 2));
                queue.Add((placeholder, 3));
            }
            queue.RemoveAt(idx);
        }

        SpawnRooms();
        // Display();
        var unspawned = interestRooms.Where(x => nodes.FindAll(y => y.room == x).Count == 0).ToArray();
        // GD.PrintS("Unspawned", new Godot.Collections.Array<string>(unspawned.Select(x => x.ResourceName)));
        return unspawned.Length == 0;
    }

    public bool GenerateOldOld(ulong seed, MapGraph mapBase)
    {
        if (IsInstanceValid(gameData))
        {
            rooms = gameData.Rooms.Where(x => x.isGeneric).ToArray();
            interestRooms = gameData.Rooms.Where(x => !x.isGeneric).ToArray();
        }
        nodes.Clear();
        topLevel.Clear();
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        graph = mapBase;//MapGraph.ParseMd(mapBase, true);

        // start
        AddNode(0, null);

        SpawnRooms();
        Display();
        var unspawned = interestRooms.Where(x => nodes.FindAll(y => y.room == x).Count == 0).ToArray();
        GD.PrintS("Unspawned", new Godot.Collections.Array<string>(unspawned.Select(x => x.ResourceName)));
        return unspawned.Length == 0;
    }

    private bool SpawnRooms()
    {
        allSpawned.Clear();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].connections.Count > 0 && nodes[i].connections.Count <= 4)
            {
                PresetRoom room = nodes[i].room;
                var dirs = GetConnectedDirectionsSorted(nodes[i]);
                if (!IsInstanceValid(room) || room.isGeneric)
                {
                    // failed to find a room
                    PresetRoom.RoomClass cl = GetRoomClass(dirs);
                    var roomArr = interestRooms.Where(x => x.type == cl && (nodes.FindAll(y => y.name == x.ResourceName).Count == 0)).ToArray();
                    if (roomArr.Length == 0)
                        roomArr = rooms.Where(x => x.type == cl && x.isGeneric).ToArray();
                    if (roomArr.Length == 0)
                        return false;
                    room = roomArr[rng.Randi() % roomArr.Length];
                    nodes[i].room = room;
                    nodes[i].name = room.isGeneric ? NAME_PLACEHOLDER : room.ResourceName;
                }
                int idx = Array.IndexOf(directions, dirs[0]);
                if (idx == -1)
                {
                    // failed to find a rotation
                    return false;
                }
                if (room.type == PresetRoom.RoomClass.Corner)
                {
                    if (idx == 0)
                    {
                        int idx2 = Array.IndexOf(directions, dirs[1]);
                        if (idx2 == 3)
                        {
                            idx = 3;
                        }
                    }
                    idx = (idx + 1) % 4;
                }
                else if (room.type == PresetRoom.RoomClass.TRoom)
                {
                    // idx = -1;
                    for (int j = 0; j < dirs.Length; j++)
                    {
                        bool foundOtherSide = false;
                        for (int k = 0; k < dirs.Length; k++)
                        {
                            if (j == k)
                                continue;
                            if (dirs[j] == -dirs[k])
                            {
                                foundOtherSide = true;
                                break;
                            }
                        }
                        if (!foundOtherSide)
                        {
                            idx = Array.IndexOf(directions, dirs[j]);
                            break;
                        }
                    }
                }
                // Node spawned = SpawnRoom(room, (Vector3)nodes[i].position * mapSize, room.type == PresetRoom.RoomClass.TRoom ? rotationsTroom[idx] : rotations[idx]);
                Node spawned = SpawnRoom(room, (Vector3)nodes[i].position * mapSize, rotations[idx]);
                allSpawned.Add(spawned);
            }
        }
        return true;
    }

    private bool SpawnRoomsOld()
    {
        allSpawned.Clear();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].connections.Count > 0)
            {
                PresetRoom room = nodes[i].room;
                var dirs = GetConnectedDirectionsSorted(nodes[i]);
                if (!IsInstanceValid(room))
                {
                    // failed to find a room
                    PresetRoom.RoomClass cl = GetRoomClass(dirs);
                    var roomArr = rooms.Where(x => x.type == cl).ToArray();
                    if (roomArr.Length == 0)
                        return false;
                    room = roomArr[rng.Randi() % roomArr.Length];
                }
                int idx = Array.IndexOf(directions, dirs[0]);
                if (idx == -1)
                {
                    // failed to find a rotation
                    return false;
                }
                if (room.type == PresetRoom.RoomClass.Corner)
                {
                    if (idx == 0)
                    {
                        int idx2 = Array.IndexOf(directions, dirs[1]);
                        if (idx2 == 3)
                        {
                            idx = 3;
                        }
                    }
                    idx = (idx + 1) % 4;
                }
                else if (room.type == PresetRoom.RoomClass.TRoom)
                {
                    // idx = -1;
                    for (int j = 0; j < dirs.Length; j++)
                    {
                        bool foundOtherSide = false;
                        for (int k = 0; k < dirs.Length; k++)
                        {
                            if (j == k)
                                continue;
                            if (dirs[j] == -dirs[k])
                            {
                                foundOtherSide = true;
                                break;
                            }
                        }
                        if (!foundOtherSide)
                        {
                            idx = Array.IndexOf(directions, dirs[j]);
                            break;
                        }
                    }
                }
                // Node spawned = SpawnRoom(room, (Vector3)nodes[i].position * mapSize, room.type == PresetRoom.RoomClass.TRoom ? rotationsTroom[idx] : rotations[idx]);
                Node spawned = SpawnRoom(room, (Vector3)nodes[i].position * mapSize, rotations[idx]);
                allSpawned.Add(spawned);
            }
        }
        return true;
    }

    public Node SpawnRoom(PresetRoom selectedRoom, Vector3 position, Vector3 rotation)
    {
        var scn = selectedRoom.SceneCached;
        if (customSpawn == null)
        {
            var newRoom = scn.Instantiate<Node3D>();
            newRoom.Position = ToGlobal(position);
            newRoom.Rotation = rotation;
            target.CallDeferred(Node3D.MethodName.AddChild, newRoom, true);
            return newRoom;
        }
        else
            return customSpawn(scn, ToGlobal(position), rotation);
    }

    public PresetRoom FindRoomByName(string name)
    {
        if (name == NAME_PLACEHOLDER)
        {
            // return null;
            uint idx = rng.Randi();
            PresetRoom[] roomArr = rooms.Where(x => x.type != PresetRoom.RoomClass.Endoff).ToArray();
            return roomArr[idx % roomArr.Length];
        }
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].ResourceName == name)
            {
                return rooms[i];
            }
        }
        for (int i = 0; i < interestRooms.Length; i++)
        {
            if (interestRooms[i].ResourceName == name)
            {
                return interestRooms[i];
            }
        }
        return null;
    }

    private void Display()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Label3D label = new Label3D();
            label.Modulate = nodes[i].name == NAME_PLACEHOLDER ? Colors.White : Colors.Green;
            if (nodes[i].connections.Count == 0 || nodes[i].connections.Count > 4)
                label.Modulate = Colors.Red;
            label.Text = nodes[i].name;
            label.Position = nodes[i].position;
            label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            // label.NoDepthTest = true;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            AddChild(label);
        }
    }

    public GraphGenNode GetNodeAt(Vector3I pos)
    {
        return nodes.Find(x => x.position == pos);
    }

    private Vector3 CalcAvgPosition()
    {
        Vector3 sum = Vector3.Zero;
        for (int i = 0; i < nodes.Count; i++)
        {
            sum += nodes[i].position;
        }
        sum /= nodes.Count;
        return sum;
    }

    private Vector3I GetDirection(Vector3 normal)
    {
        int id = 0;
        float dot = normal.Dot(directions[0]);
        for (int i = 1; i < directions.Length; i++)
        {
            float d = normal.Dot(directions[i]);
            if (d > dot)
            {
                id = i;
                dot = d;
            }
        }
        return directions[id];
    }

    private int FindNode(string name, bool skipPlaceholder)
    {
        if (skipPlaceholder && name == NAME_PLACEHOLDER)
        {
            return -1;
        }
        for (int i = 0; i < graph.nodes.Count; i++)
        {
            if (graph.nodes[i].name == name)
            {
                return i;
            }
        }
        return -1;
    }

    private Vector3I[] GetConnectedDirectionsSorted(GraphGenNode node)
    {
        List<Vector3I> list = new List<Vector3I>();
        for (int i = 0; i < directions.Length; i++)
        {
            // GetNodeAt(node.position + directions[i]);
            for (int j = 0; j < node.connections.Count; j++)
            {
                if (node.connections[j].position == node.position + directions[i])
                {
                    list.Add(directions[i]);
                }
            }
            // if (GetNodeAt(node.position + directions[i]) != null)
            // {
                // list.Add(directions[i]);
            // }
        }
        return list.ToArray();
    }

    private PresetRoom.RoomClass GetRoomClass(Vector3I[] dirs)
    {
        if (dirs.Length == 1)
        {
            return PresetRoom.RoomClass.Endoff;
        }
        else if (dirs.Length == 2)
        {
            if (dirs[0] == -dirs[1])
            {
                return PresetRoom.RoomClass.Hall;
            }
            else
            {
                return PresetRoom.RoomClass.Corner;
            }
        }
        else if (dirs.Length == 3)
        {
            return PresetRoom.RoomClass.TRoom;
        }
        return PresetRoom.RoomClass.XRoom;
    }

    private bool IsDirectionsValid(Vector3I pos, Vector3I[] dirs, PresetRoom.RoomClass roomClass)
    {
        // this function assumes "dirs" is sorted in order of the "directions" const
        switch (roomClass)
        {
            case PresetRoom.RoomClass.Endoff:
            {
                if (dirs.Length == 1)
                {
                    return true;
                }
                break;
            }
            case PresetRoom.RoomClass.Corner:
            {
                if (dirs.Length == 2)
                {
                    int idx = Array.IndexOf(directions, dirs[0]);
                    Vector3I dPrev = directions[(idx+3) % 4];
                    Vector3I dNext = directions[(idx+1) % 4];
                    if (dPrev == dirs[1] || dNext == dirs[1])
                    {
                        return true;
                    }
                }
                break;
            }
            case PresetRoom.RoomClass.Hall:
            {
                if (dirs.Length == 2)
                {
                    int idx = Array.IndexOf(directions, dirs[0]);
                    Vector3I dNext = directions[(idx+2) % 4];
                    if (dNext == dirs[1])
                    {
                        return true;
                    }
                }
                break;
            }
            case PresetRoom.RoomClass.TRoom:
            {
                if (dirs.Length == 3)
                {
                    int idx = Array.IndexOf(directions, dirs[0]);
                    Vector3I dNext = directions[(idx+1) % 4];
                    Vector3I dPrev = directions[(idx+2) % 4];
                    // if ((dNext == dirs[1] || dPrev == dirs[1]) && (dNext == dirs[2] || dPrev == dirs[2]) && dirs[1] != dirs[2])
                    if ((dNext == dirs[1] && dPrev == dirs[2]) || (dNext == dirs[2] && dPrev == dirs[1]))
                    {
                        return true;
                    }
                }
                break;
            }
            case PresetRoom.RoomClass.XRoom:
            {
                return dirs.Length == 4;
            }
        }
        return false;
    }

    private GraphGenNode AddNode(int idx, GraphGenNode parent, Vector3I nextDirection = default)
    {
        if (idx != -1 && graph.nodes[idx].name == NAME_PLACEHOLDER && parent != null)
        {
            // idx = -1;
        }
        if (idx != -1 && nodes.FindAll(x => x.name == graph.nodes[idx].name).Count >= graph.nodes[idx].instances)
            return null;
        GraphGenNode node = new GraphGenNode();
        if (idx == -1)
        {
            node.name = NAME_PLACEHOLDER;
            node.room = null;
            if (parent == null)
            {
                node.position = Vector3I.Zero;
            }
            else
            {
                node.position = parent.position + nextDirection;
                parent.connections.Add(node);
                node.connections.Add(parent);
            }
            nodes.Add(node);
        }
        else
        {
            node.name = graph.nodes[idx].name;
            node.room = FindRoomByName(node.name);
            Vector3I dirToParent = Vector3I.Zero;
            if (parent == null)
            {
                node.position = Vector3I.Zero;
            }
            else
            {
                Vector3 normal = parent.position - CalcAvgPosition();
                // if (normal.IsZeroApprox())
                {
                    // there isn't a normal vector, choose a random direction.
                    dirToParent = directions[rng.Randi() % directions.Length];
                    node.position = parent.position + dirToParent;
                }
                // else
                if (!normal.IsZeroApprox() && GetNodeAt(node.position) != null)
                {
                    Vector3I dir = GetDirection(normal.Normalized());
                    dirToParent = dir;
                    node.position = parent.position + dir;
                }
                // if (GetNodeAt(node.position) != null)
                {
                    dirToParent = nextDirection;
                    node.position = parent.position + nextDirection;
                }
                parent.connections.Add(node);
                node.connections.Add(parent);
            }
            nodes.Add(node);
            List<int> validDirections = roomClassToDirections[PresetRoom.RoomClass.XRoom];
            if (IsInstanceValid(node.room))
                validDirections = roomClassToDirections[node.room.type];
            List<Vector3I> selectedDirs = new List<Vector3I>();
            bool selectedSet = false;
            for (int j = 0; j < 4; j++)
            {
                selectedDirs.Clear();
                bool foundParent = false;
                for (int i = 0; i < validDirections.Count; i++)
                {
                    Vector3I dir = directions[(validDirections[i] + j) % 4];
                    if (dir == -dirToParent)
                    {
                        foundParent = true;
                    }
                    else if (GetNodeAt(node.position + dir) != null)
                    {
                        foundParent = false;
                        break;
                    }
                    else
                    {
                        selectedDirs.Add(dir);
                    }
                }
                if (foundParent)
                {
                    selectedSet = true;
                    break;
                }
            }
            if (!selectedSet)
            {
                selectedDirs.Clear();
                for (int i = 0; i < validDirections.Count; i++)
                {
                    selectedDirs.Add(directions[validDirections[i]]);
                }
            }
            List<string> connections = graph.nodes[idx].connections.ToList();
            // connections.RemoveAll(x => x == NAME_PLACEHOLDER);
            List<(GraphGenNode, Vector3I)> placeholders = new List<(GraphGenNode, Vector3I)>();
            List<(GraphGenNode, Vector3I)> halls = new List<(GraphGenNode, Vector3I)>();
            int dirLen = selectedDirs.Count;
            for (int i = 0; i < dirLen; i++)
            {
                GraphGenNode placeholderNode = AddNode(-1, node, selectedDirs[i]);
                int dirIdx = Array.IndexOf(directions, selectedDirs[i]);
                int rngDir = rng.Randi() % 2 == 0 ? 3 : 1;
                // int rngDir = rng.Randi() % 5 == 0 ? 0 : (rng.Randi() % 2 == 0 ? 3 : 1);
                Vector3I dir = directions[(dirIdx + rngDir) % 4];
                if (rngDir != 0)
                {
                    halls.Add((placeholderNode, dir));
                }
                placeholderNode = AddNode(-1, placeholderNode, selectedDirs[i]);
                placeholders.Add((placeholderNode, selectedDirs[i]));
            }
            if (placeholders.Count == 0)
            {
                return node;
            }
            if (connections.Count > placeholders.Count)
            {
                int len = connections.Count - placeholders.Count;
                for (int j = 0; j < len; j++)
                {
                    int dirIdx = Array.IndexOf(directions, placeholders[j].Item2);
                    int rngDir = rng.Randi() % 2 == 0 ? 3 : 1;
                    // int rngDir = rng.Randi() % 5 == 0 ? 0 : (rng.Randi() % 2 == 0 ? 3 : 1);
                    if (rngDir == 0)
                        continue;
                    Vector3I dir = directions[(dirIdx + rngDir) % 4];
                    if (rngDir != 0 && GetNodeAt(placeholders[j].Item1.position + dir) != null && (GetNodeAt(placeholders[j].Item1.position + dir * 2) != null || GetNodeAt(placeholders[j].Item1.position + dir * 3) != null))
                    {
                        dir = -dir;
                    }
                    int connIdx = (int)(rng.Randi() % connections.Count);
                    // GraphGenNode placeholderNode = AddNode(FindNode(connections[connIdx], true), placeholders[j].Item1, dir);
                    GraphGenNode placeholderNode = AddNode(-1, placeholders[j].Item1, dir);
                    // connections.RemoveAt(connIdx);
                    halls.Add((placeholderNode, placeholders[j].Item2));
                    // halls.Add((placeholderNode, dir));
                }
            }
            for (int i = 0, j = 0; i < graph.nodes[idx].connections.Count; i++, j++)
            {
                int next = FindNode(graph.nodes[idx].connections[i], false);
                if (next == -1)
                {
                    // GD.PrintErr($"FindNode = -1 on {graph.nodes[idx].connections[i]}");
                    j--;
                    continue;
                }
                if (j >= halls.Count)
                    break;
                AddNode(next, halls[j].Item1, halls[j].Item2);
            }
            Vector3I[] dirs = GetConnectedDirectionsSorted(node);
            if (IsInstanceValid(node.room) && !IsDirectionsValid(node.position, dirs, node.room.type))
            {
                Log.PrintErr($"Node {node.position} {node.name} {node.room.type} is invalid!");
            }
        }
        return node;
    }
}