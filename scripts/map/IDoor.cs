using System;

public interface IDoorStatus
{
    DoorStatus Status { get; }
    float StatusPulse { get; }
    Action StatusChanged { get; set; }
}

public enum DoorStatus : byte
{
    Ok = 0,
    Warn = 1,
    Err = 2,
    Invalid = 3,
    Wait = 4,
    Idle = 5,
}