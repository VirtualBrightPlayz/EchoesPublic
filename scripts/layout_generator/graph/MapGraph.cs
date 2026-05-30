using System;
using System.Collections.Generic;
using Godot;

public partial class MapGraph
{
    public class MapGraphNode
    {
        public string name;
        public int instances;
        public List<string> connections = new List<string>();
    }

    public List<MapGraphNode> nodes = new List<MapGraphNode>();

    public static MapGraph ParseMd(string content, bool bidi = false)
    {
        if (string.IsNullOrEmpty(content))
            return null;
        MapGraph graph = new MapGraph();
        string[] lines = content.ReplaceLineEndings("\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("# "))
            {
                string[] room = lines[i].Substring(2).Split(':');
                graph.nodes.Add(new MapGraphNode()
                {
                    name = room[0],
                    instances = int.Parse(room[1]),
                });
            }
            if (lines[i].StartsWith("- "))
            {
                string room = lines[i].Substring(2);
                graph.nodes[^1].connections.Add(room);
                if (bidi)
                {
                    int idx = graph.nodes.FindIndex(x => x.name == room);
                    if (idx != -1)
                    {
                        graph.nodes[idx].connections.Add(graph.nodes[^1].name);
                    }
                }
            }
        }
        return graph;
    }
}