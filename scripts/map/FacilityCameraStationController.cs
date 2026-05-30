using System.Linq;
using Godot;

public partial class FacilityCameraStationController : Node3D
{
    [Export]
    public FacilityCameraStation[] stations;

    [Export]
    public bool autoDiscoverStations = true;

    [Export]
    public double delayBetweenCams = 0.2f;
    
    private double _counter = 0.0f;
    
    private int _currentIndex = 0;
    
    public override void _Ready()
    {
        base._Ready();
        if (autoDiscoverStations)
        {
            TryFindSubStations();
        }
    }

    private void TryFindSubStations()
    {
        foreach (var child in GetChildren())
        {
            if (child is FacilityCameraStation)
            {
                stations = stations.Append(child as FacilityCameraStation).ToArray();
            }
        }
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_counter < delayBetweenCams)
        {
            _counter += delta;
            return;
        }
        else
        {
            _counter = 0f;
        }
        IterateToNextStation();
        if (_currentIndex >= stations.Length)
            return;
        FacilityCameraStation station = stations[_currentIndex];
        if (!stations.Any(IsInstanceValid))
        {
            //QueueFree();
            return;
        }
        while (!IsInstanceValid(station) || station.CameraStationState.HasFlag(FacilityCamera.SecurityCameraState.NoVideo))
        {
            IterateToNextStation();
            station = stations[_currentIndex];
        }
        station.RenderNextFrame();
    }

    void IterateToNextStation()
    {
        if (stations.Length == 1)
        {
            _currentIndex = 0;
            return;
        }
        if (_currentIndex >= stations.Length)
        {
            _currentIndex = 0;
            return;
        }
        _currentIndex++;
    }
}