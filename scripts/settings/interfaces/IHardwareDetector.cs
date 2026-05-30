using System.Collections.Generic;

public interface IHardwareDetector<T>
{
    IEnumerable<T> GetAvailableHardware();

    IEnumerable<string> GetAvailableHardwareNames();

    HardwareDetectorType DetectorType { get; }

    T Default { get; }

    string DefaultName { get; }

    int ConvertToIndex(T value);
}

public enum HardwareDetectorType
{
    Screen,
    AudioOut,
    AudioIn,
    Graphics,
}
