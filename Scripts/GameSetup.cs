using Ches;
using Godot;
using System;

public partial class GameSetup : Control
{
    [Signal]
    public delegate void GameStartedEventHandler(GameSettings settings);

    private bool _timer;

    public override void _Ready()
    {
        //START GAME MENU
        Button newGame = GetNode<Button>("LoadOrNew/NewGame");
        Button loadGame = GetNode<Button>("LoadOrNew/LoadGame");
        newGame.Pressed += NewGame;
        loadGame.Pressed += LoadGame;

        // GAME SETTINGS
        CheckButton enableTimer = GetNode<CheckButton>("GameSettings/EnableTimer");
        Button start = GetNode<Button>("GameSettings/Start");
        enableTimer.Toggled += EnableTimer;
        start.Pressed += StartGame;

        enableTimer.ButtonPressed = true;
    }

    private void StartGame()
    {
        if (_timer)
        {
            double minutes = GetNode<SpinBox>("GameSettings/Minutes").Value;
            double seconds = GetNode<SpinBox>("GameSettings/SecondsAdded").Value;

            GameSettings settings = new GameSettings(_timer, minutes, seconds);
            EmitSignal(SignalName.GameStarted, settings);
        }
        else
        {
            GameSettings settings = new GameSettings(_timer);
            EmitSignal(SignalName.GameStarted, settings);
        }
    }

    private void EnableTimer(bool toggled)
    {
        if (toggled)
        {
            GetNode<SpinBox>("GameSettings/Minutes").Editable = true;
            GetNode<SpinBox>("GameSettings/SecondsAdded").Editable = true;
            _timer = true;
        }
        else
        {
            GetNode<SpinBox>("GameSettings/Minutes").Editable = false;
            GetNode<SpinBox>("GameSettings/SecondsAdded").Editable = false;
            _timer = false;
        }
    }

    private void NewGame()
    {
        GetNode<Control>("LoadOrNew").Visible = false;
        GetNode<Control>("GameSettings").Visible = true;
    }

    private void LoadGame()
    {
        GetNode<Control>("LoadOrNew").Visible = false;
        GetNode<Control>("GameSettings").Visible = false;
        GetNode<Control>("GameSettings/LoadMenu").Visible = true;
    }
}
