using Godot;
using System;

namespace Ches;
public partial class GameUI : Control
{
    [Signal]
    public delegate void GameRestartedEventHandler(int a, int b);

    [Signal]
    public delegate void DrawSelectedEventHandler();

    [Signal]
    public delegate void RevertMenuOpenedEventHandler();

    [Signal]
    public delegate void RejectedEventHandler();

    [Signal]
    public delegate void GameSavedEventHandler();

    [Signal]
    public delegate void GameRevertedEventHandler();


    [Export] private Button _restart;
    [Export] private Button _draw;
    [Export] private Button _revert;
    [Export] private Button _reject;
    [Export] private Button _saveGame;
    [Export] private Label _debugTracker;
    [Export] private Label _debugTracker2;
    [Export] private Label _endGame;
    [Export] private Camera2D _camera;
    [Export] private RevertMenu _revertMenu;
    [Export] private Control _ui;

    public override void _Ready()
	{
        _restart.Pressed += () => EmitSignal(SignalName.GameRestarted);
        _draw.Pressed += AgreedDraw;
        _revert.Pressed += Revert;
        _reject.Pressed += Reject;
        _saveGame.Pressed += SaveGame;
        _revertMenu.PreviousBoardSelected += RevertGameStatus;
    }

    public void Revert()
    {
        _revertMenu.Visible = true;
        _revertMenu.BoardHistory = _boardHistory;
        _revertMenu.SetUp();
    }
}
