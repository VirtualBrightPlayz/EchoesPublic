using DeBroglie;
using DeBroglie.Constraints;

public partial class LayoutConstraints : ITileConstraint
{
    public Tile tile;
    public Tile empty;
    private LayoutGeneratorDeBroglie.RoomSettingsRotated room;

    public void Check(TilePropagator propagator)
    {
        var topology = propagator.Topology;
        for (var z = 0; z < topology.Depth; z++)
        {
            for (var y = 2; y < topology.Height - 2; y++)
            {
                for (var x = 2; x < topology.Width - 2; x++)
                {
                    var index = topology.GetIndex(x, y, z);
                    if (topology.Mask != null)
                    {
                        if (!topology.Mask[index])
                            continue;
                    }

                    var t = propagator.GetTile(index);
                    if (t == tile)
                    {
                        var rot = LayoutGeneratorDeBroglie.GetDirection(room.Rotation);
                        int var1 = 2;
                        int var2 = 1;
                        int var3 = 0;
                        switch (rot)
                        {
                            case DeBroglie.Topo.Direction.XPlus:
                                propagator.Select(x+var1, y, z, empty);
                                propagator.Select(x+var1, y+var2, z, empty);
                                propagator.Select(x+var1, y-var2, z, empty);
                                propagator.Select(x+var3, y+var2, z, empty);
                                propagator.Select(x+var3, y-var2, z, empty);
                                break;
                            case DeBroglie.Topo.Direction.YPlus:
                                propagator.Select(x, y+var1, z, empty);
                                propagator.Select(x+var2, y+var1, z, empty);
                                propagator.Select(x-var2, y+var1, z, empty);
                                propagator.Select(x+var2, y+var3, z, empty);
                                propagator.Select(x-var2, y+var3, z, empty);
                                break;
                            case DeBroglie.Topo.Direction.XMinus:
                                propagator.Select(x-var1, y, z, empty);
                                propagator.Select(x-var1, y+var2, z, empty);
                                propagator.Select(x-var1, y-var2, z, empty);
                                propagator.Select(x-var3, y+var2, z, empty);
                                propagator.Select(x-var3, y-var2, z, empty);
                                break;
                            case DeBroglie.Topo.Direction.YMinus:
                                propagator.Select(x, y-var1, z, empty);
                                propagator.Select(x+var2, y-var1, z, empty);
                                propagator.Select(x-var2, y-var1, z, empty);
                                propagator.Select(x+var2, y-var3, z, empty);
                                propagator.Select(x-var2, y-var3, z, empty);
                                break;
                        }
                    }
                }
            }
        }
    }

    public void Init(TilePropagator propagator)
    {
        room = (LayoutGeneratorDeBroglie.RoomSettingsRotated)tile.Value;
    }
}
