using Godot;
using System;

public partial class ExampleGeneratorManager : Node3D
{
    [Export]
    public Layout TargetLayout;

    [Export(PropertyHint.ResourceType, "RoomGenerationSettingsCollection")]
    public RoomGenerationSettingsCollection RoomGenerationSettingsCollection;

    [Export]
    public ulong Seed = 0;

    public override async void _Ready()
    {
        if (TargetLayout == null || RoomGenerationSettingsCollection == null) return;

        this.TargetLayout.InitializeLayout();
        LayoutGeneratorWaveFunctionCollapse generator = new LayoutGeneratorWaveFunctionCollapse();
        this.AddChild(generator);
        ulong internalSeed = this.Seed;

        // Generate until we find a layout that works
        while (true)
        {
            ELayoutGenerationResult result = await generator.GenerateAsync(this.TargetLayout, this.RoomGenerationSettingsCollection, internalSeed);

            if (result == ELayoutGenerationResult.Success)
            {
                break;
            }

            internalSeed += 73;
            TargetLayout.ResetLayout();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
