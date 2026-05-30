using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class UseHardwareDetectorAttribute : Attribute
{
    public HardwareDetectorType DetectorType { get; }

    public UseHardwareDetectorAttribute(HardwareDetectorType detectorType)
    {
        DetectorType = detectorType;
    }
}