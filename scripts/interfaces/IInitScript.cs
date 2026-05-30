using Godot;

public interface IInitScript
{
    public static IInitScript Instance { get; protected set; }
    public static bool IsServerOnly { get; protected set; }
    public static bool IsTestClient { get; protected set; }
    public static bool IsHeadless { get; protected set; }
    public static SceneTree SceneTree => (SceneTree)Engine.GetMainLoop();

    static IInitScript()
    {
        IsHeadless = string.IsNullOrEmpty(RenderingServer.GetVideoAdapterName());
        IsTestClient = OS.HasFeature("clientonly");
        IsServerOnly = (IsHeadless || OS.HasFeature("serveronly")) && !IsTestClient;
    }

    string Username { get; }
    bool IsXR { get; }
    GameData Data { get; }
}