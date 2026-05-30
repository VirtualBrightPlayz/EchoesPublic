using System;

[HiddenCommand]
public class MatrixCommand : SimpleGameCommandBase
{
    public override string Command { get; } = "Matrix";

    public override string[] Alias { get; } = Array.Empty<string>();

    public override string CommandDescription => "Matrix";

    public override bool Execute(string[] args, out string response)
    {
        MenuManager.Instance.LocalMatrixState(!MenuManager.Instance.InMatrix);
        response = "You take the pill.";
        return true;
    }
}
