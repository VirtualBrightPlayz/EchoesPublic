using System;
using Godot;

public interface IAdminConsoleCommand
{
    Type Category { get; }

    string Container { get; }

    Control GenerateUi(AdminHUD admin);
}