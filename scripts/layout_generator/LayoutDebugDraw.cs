using Godot;
using System;
using static Godot.GeometryInstance3D;

public partial class LayoutDebugDraw : Node
{
    [Export]
    public Layout TargetLayout;

    [Export]
    public bool Active = true;

    [Export]
    public float OffsetY = 0.1f;

    [Export]
    public Node3D world;


    public static Color GridColor = new Color(0, 0, 255);
    public static Color ConnectionColor = new Color(0, 255, 0);
    public static Color BlockedColor = new Color(255, 0, 0);

    private StandardMaterial3D material;

    private MeshInstance3D meshInstance;
    private Label3D label;

    public override void _Ready()
    {
        this.meshInstance = new MeshInstance3D();
        this.AddChild(this.meshInstance);
        this.meshInstance.Position = new Vector3(0, this.OffsetY, 0);
        this.meshInstance.Rotation = Vector3.Zero;
		this.meshInstance.Mesh = new ImmediateMesh();
		this.meshInstance.CastShadow = ShadowCastingSetting.Off;

		this.material = new StandardMaterial3D();
		this.material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		this.material.VertexColorUseAsAlbedo = true;
		this.material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
		this.meshInstance.MaterialOverride = this.material;

        this.label = new Label3D();
        this.AddChild(this.label);
        this.label.GlobalPosition = Vector3.Zero;
		this.label.GlobalRotation = Vector3.Zero;
		this.label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        this.label.NoDepthTest = true;
        this.label.HorizontalAlignment = HorizontalAlignment.Left;
    }

    public ImmediateMesh GetImmediateMesh()
    {
        return this.meshInstance.Mesh as ImmediateMesh;
    }

    public override void _Process(double delta)
    {
        if (!this.Active || this.TargetLayout == null) return;
        this.GetImmediateMesh().ClearSurfaces();
        this.GetImmediateMesh().SurfaceBegin(Mesh.PrimitiveType.Lines);

        for(int x = 0; x < this.TargetLayout.GridSize.X; x++)
        {
            for (int y = 0; y < this.TargetLayout.GridSize.Y; y++)
            {
                LayoutCell currentCell = this.TargetLayout.GetCell(new Vector2I(x, y));
                Vector3 pointA = new Vector3(1, 0, 1) * this.TargetLayout.CellSize / 2;
                Vector3 pointB = new Vector3(-1, 0, 1) * this.TargetLayout.CellSize / 2;
                Vector3 pointC = new Vector3(-1, 0, -1) * this.TargetLayout.CellSize / 2;
                Vector3 pointD = new Vector3(1, 0, -1) * this.TargetLayout.CellSize / 2;

                // Draw grid outline
                this.drawLine(currentCell.GetGlobalPosition() + pointA, currentCell.GetGlobalPosition() + pointB, GridColor);
                this.drawLine(currentCell.GetGlobalPosition() + pointB, currentCell.GetGlobalPosition() + pointC, GridColor);
                this.drawLine(currentCell.GetGlobalPosition() + pointC, currentCell.GetGlobalPosition() + pointD, GridColor);
                this.drawLine(currentCell.GetGlobalPosition() + pointD, currentCell.GetGlobalPosition() + pointA, GridColor);

                // Draw connections
                if (currentCell.Connections.HasFlag(ERoomDirectionFlags.PositiveX))
                {
                    Vector3 pointEnd = new Vector3(0.8f, 0, 0) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, ConnectionColor);
                }

                if (currentCell.Connections.HasFlag(ERoomDirectionFlags.PositiveZ))
                {
                    Vector3 pointEnd = new Vector3(0, 0, 0.8f) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, ConnectionColor);
                }

                if (currentCell.Connections.HasFlag(ERoomDirectionFlags.NegativeX))
                {
                    Vector3 pointEnd = new Vector3(-0.8f, 0, 0) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, ConnectionColor);
                }

                if (currentCell.Connections.HasFlag(ERoomDirectionFlags.NegativeZ))
                {
                    Vector3 pointEnd = new Vector3(0, 0, -0.8f) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, ConnectionColor);
                }

                // Draw blocked neighbours
                if (currentCell.BlockedNeighbours.HasFlag(ERoomDirectionFlags.PositiveX))
                {
                    Vector3 pointEnd = new Vector3(0.4f, 0, 0) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, BlockedColor);
                }

                if (currentCell.BlockedNeighbours.HasFlag(ERoomDirectionFlags.PositiveZ))
                {
                    Vector3 pointEnd = new Vector3(0, 0, 0.4f) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, BlockedColor);
                }

                if (currentCell.BlockedNeighbours.HasFlag(ERoomDirectionFlags.NegativeX))
                {
                    Vector3 pointEnd = new Vector3(-0.4f, 0, 0) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, BlockedColor);
                }

                if (currentCell.BlockedNeighbours.HasFlag(ERoomDirectionFlags.NegativeZ))
                {
                    Vector3 pointEnd = new Vector3(0, 0, -0.4f) * this.TargetLayout.CellSize / 2;
                    this.drawLine(currentCell.GetGlobalPosition(), currentCell.GetGlobalPosition() + pointEnd, BlockedColor);
                }
            }
        }

        this.GetImmediateMesh().SurfaceEnd();

        // Draw cell information
        Camera3D activeCamera = world?.GetViewport()?.GetCamera3D() ?? GetViewport().GetCamera3D();
        if (activeCamera != null)
        {
            LayoutCell debugCell = this.TargetLayout.FindCellFromGlobalPositon(activeCamera.GlobalPosition);
            if (debugCell != null)
            {
                this.label.Position = debugCell.GetGlobalPosition();
				string debugText = "";
                debugText += $"Node Name: {debugCell.Name}\n";
                debugText += $"Grid Position: X={debugCell.GridPosition.X} Y={debugCell.GridPosition.Y}\n";
                debugText += $"Grid Rotation: {debugCell.GridRotation}\n";
                debugText += $"Global Position: {debugCell.GetGlobalPosition()}\n";
                debugText += $"Global Rotation (deg): {debugCell.GetGlobalRotationDegrees()}\n";
                debugText += $"Is Generated: {debugCell.IsGenerated}\n";
                debugText += $"Is Blocked by Neighbour: {debugCell.IsBlockedByNeighbour()}\n";
				debugText += $"Is Required to Generate: {debugCell.IsRequiredToGenerate()}\n";
				debugText += $"Unique Cell Seed: {debugCell.UniqueCellSeed}\n";
                debugText += $"Room Generation Resource: {(debugCell.RoomGenerationResource != null ? debugCell.RoomGenerationResource.ResourcePath : "Null")}\n";
                debugText += $"Scene Path: {(debugCell.RoomSceneResource != null ? debugCell.RoomSceneResource.ResourcePath : "Null")}\n";
                this.label.Text = debugText;
            }
        }
    }

    private void drawLine(Vector3 globalStart, Vector3 globalEnd, Color color)
    {
        this.GetImmediateMesh().SurfaceSetColor(color);
        this.GetImmediateMesh().SurfaceAddVertex(globalStart);
        this.GetImmediateMesh().SurfaceAddVertex(globalEnd);
    }
}
