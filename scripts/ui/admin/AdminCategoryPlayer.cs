using System;
using Godot;

public partial class AdminCategoryPlayer : AdminCategoryCommandBase
{
    [Export]
    public LineEdit ReasonBox;

    public enum Container
    {
        Teleporting,
        Movement,
        Stats,
        Moderation,
        Props
    }
}