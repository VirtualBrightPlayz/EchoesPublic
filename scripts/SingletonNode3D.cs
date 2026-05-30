using Godot;

public abstract partial class SingletonNode3D<SingletonType> : Node3D where SingletonType : SingletonNode3D<SingletonType>
{
	public static SingletonType Instance { get; private set; }

	public override void _EnterTree()
	{
		base._EnterTree();
        if (Engine.IsEditorHint())
            return;

		if (IsInstanceValid(Instance) && Instance != this)
		{
			Log.PrintWarn($"Singleton {typeof(SingletonType).Name} is already active in scene! Active path: {Instance.GetPath()}");
			QueueFree();
			return;
		}

		Instance = (SingletonType)this;
	}

	public override void _Ready()
	{
        if (Engine.IsEditorHint())
            return;

		if (!IsInstanceValid(Instance))
			Log.PrintErr($"Singleton {typeof(SingletonType).Name} does not call base._EnterTree - singleton instance not set!");
	}
}