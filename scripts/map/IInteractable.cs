using Godot;

public interface IInteractable
{
    public static StringName META_NAME = "interactable";

    public enum InteractType
    {
        Touch,
        Grab,
        Hit,
        Drag,
        DragCapture,
    }

    void Use(IItemHolder holder, ItemObject item);
    void UseEnd(IItemHolder holder, ItemObject item);
    bool CanUse(IItemHolder holder, ItemObject item);

    Vector3 WorldInteractPosition { get; }
    InteractType ActionType { get; }
}
