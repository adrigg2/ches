using Godot;
using System;

public partial class GameSetup : Control
{
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
        GetTree().ChangeSceneToFile("res://scenes/chess_game.tscn");
    }

    private void EnableTimer(bool toggled)
    {
        if (toggled)
        {
            GetNode<SpinBox>("GameSettings/Minutes").Editable = true;
            GetNode<SpinBox>("GameSettings/SecondsAdded").Editable = true;
        }
        else
        {
            GetNode<SpinBox>("GameSettings/Minutes").Editable = false;
            GetNode<SpinBox>("GameSettings/SecondsAdded").Editable = false;
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
