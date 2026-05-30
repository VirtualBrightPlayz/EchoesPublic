#if TOOLS
using System.Collections.Generic;
using Godot;

[Tool]
public partial class MapRoomGizmo : EditorNode3DGizmo
{
    public StandardMaterial3D material;

    public override void _Redraw()
    {
        if (!IsInstanceValid(material))
        {
            material = new StandardMaterial3D();
        }
        Clear();
        var node = GetNode3D() as MapRoom;
        List<Vector3> lines = new List<Vector3>();
        lines.Add(new Vector3(0, 1, 0));
        lines.Add(new Vector3(0, 0, 0));
        AddLines(lines.ToArray(), material);
    }
}
#endif