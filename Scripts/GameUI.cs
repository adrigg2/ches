using Godot;
using System;

namespace Ches;
public partial class GameUI : Control
{
    [Signal]
    public delegate void GameRestartedEventHandler();

    [Signal]
    public delegate void DrawSelectedEventHandler();

    [Signal]
    public delegate void GameSavedEventHandler();

    [Signal]
    public delegate void GameRevertedEventHandler(int index);


    [Export] private Button _restart;
    [Export] private Button _draw;
    [Export] private Button _revert;
    [Export] private Button _reject;
    [Export] private Button _saveGame;
    [Export] private Label _endGame;
    [Export] private Label _timerLabel1;
    [Export] private Label _timerLabel2;
    [Export] private RevertMenu _revertMenu;

    private Timer _timer1;
    private Timer _timer2;

    public override void _Ready()
	{
        _restart.Pressed += Reset;
        _draw.Pressed += () => EmitSignal(SignalName.DrawSelected);
        _revert.Pressed += Revert;
        _reject.Pressed += Reject;
        _saveGame.Pressed += () => EmitSignal(SignalName.GameSaved);
        _revertMenu.PreviousBoardSelected += (index) => EmitSignal(SignalName.GameReverted, index);


        if (Main.Settings.Timer)
        {
            _timerLabel1.Visible = true;
            _timerLabel2.Visible = true;

            _timerLabel1.Text = $"{Main.Settings.Minutes} : 00";
            _timerLabel2.Text = $"{Main.Settings.Minutes} : 00";

            SetProcess(true);
        }
        else
        {
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        if (_timer1.TimeLeft != 0)
        {
            _timerLabel1.Text = $"{(int)_timer1.TimeLeft / 60} : {(int)_timer1.TimeLeft % 60}";
        }

        if (_timer2.TimeLeft != 0)
        {
            _timerLabel2.Text = $"{(int)_timer2.TimeLeft / 60} : {(int)_timer2.TimeLeft % 60}";
        }
    }

    private void Revert()
    {
        _revertMenu.Visible = true;
        _revertMenu.SetUp();
    }

    private void Reject()
    {
        _reject.Visible = false;
        _draw.Position = new Vector2(20, 220);
        _endGame.Visible = false;
    }

    public void ChangeTurn(int turn, int situationCount)
    {
        if (turn == 2)
        {
            Scale = new Vector2(-1, -1);
            Position = new Vector2(768, 384);
            _revertMenu.Camera.Zoom *= new Vector2(-1, -1);
        }
        else if (turn == 1)
        {
            Scale = new Vector2(1, 1);
            Position = new Vector2(0, 0);
            _revertMenu.Camera.Zoom *= new Vector2(-1, -1);
        }

        if (situationCount >= 3 && situationCount < 5)
        {
            _endGame.Text = "Draw by repetition?";
            _draw.Position = new Vector2(316, 215);
            _restart.Visible = false;
            _revert.Visible = false;
            _reject.Visible = true;
            _endGame.Visible = true;
        }
    }

    public void GameEnded(int loser)
    {
        _endGame.Visible = true;
        _endGame.MoveToFront();
        _restart.MoveToFront();
        _draw.Visible = false;
        _revert.Visible = false;
        _reject.Visible = false;
        _saveGame.Visible = false;
        _restart.Visible = true;

        _endGame.Position = new Vector2(0, 0);
        _endGame.Scale = new Vector2(1, 1);
        _restart.Position = new Vector2(316, 215);
        _restart.Scale = new Vector2(1, 1);

        if (loser == 1)
        {
            _endGame.Text = Tr("BLACK");
        }
        else if (loser == 2)
        {
            _endGame.Text = Tr("WHITE");
        }
        else if (loser == 0)
        {
            _endGame.Text = "Draw";
        }
    }

    private void Reset()
    {
        _endGame.Visible = false;

        _restart.Position = new Vector2(20, 293);
        _draw.Position = new Vector2(20, 220);
        _revert.Position = new Vector2(20, 147);
        _draw.Visible = true;
        _revert.Visible = true;
        _reject.Visible = false;

        Scale = new Vector2(1, 1);
        Position = new Vector2(0, 0);
        _revertMenu.Camera.Zoom = new Vector2(1, 1);

        _timerLabel1.Text = $"{Main.Settings.Minutes} : 00";
        _timerLabel2.Text = $"{Main.Settings.Minutes} : 00";

        EmitSignal(SignalName.GameRestarted);
    }

    public void SetTimers(Timer timer, int player)
    {
        GD.Print("Setting timers");
        
        if (player == 1)
        {
            _timer1 = timer;
        }
        else 
        {
            _timer2 = timer;
        }
    }
}
