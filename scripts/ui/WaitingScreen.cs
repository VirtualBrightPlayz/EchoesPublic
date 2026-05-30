using Godot;

public partial class WaitingScreen : Node
{
    [Export]
    public Control root;
    [Export]
    public Label stateLabel;
    [Export]
    public Label playerCountLabel;
    [Export]
    public ProgressBar progressBar;

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(RoundManager.Instance))
        {
            root.Visible = false;
            return;
        }
        root.Visible = RoundManager.Instance.state != RoundManager.RoundState.InGame;
        switch (RoundManager.Instance.state)
        {
            case RoundManager.RoundState.WaitingForPlayers:
                progressBar.Indeterminate = false;
                progressBar.MaxValue = RoundManager.Instance.syncTimeToStart;
                progressBar.Value = Mathf.Lerp(progressBar.Value, progressBar.MaxValue - RoundManager.Instance.syncRoundTimer, delta * 2d);
                stateLabel.Text = "Waiting for players...";
                playerCountLabel.Text = $"{RoundManager.Instance.players.PlayerList.Count} connected players";
                break;
            case RoundManager.RoundState.Loading:
                progressBar.Indeterminate = true;
                stateLabel.Text = "Now loading...";
                playerCountLabel.Text = $"{RoundManager.Instance.players.PlayerList.Count} connected players";
                break;
            case RoundManager.RoundState.End:
                progressBar.Indeterminate = true;
                stateLabel.Text = "Match complete!";
                playerCountLabel.Text = RoundManager.Instance.winningTeam;
                break;
        }
    }
}