using Godot;
using System;

namespace Chess;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
        Button local = GetNode<Button>("Local");
        Button online = GetNode<Button>("Online");
        Button options = GetNode<Button>("Options");
        local.Pressed += LocalSelected;
        online.Pressed += OnlineSelected;
        options.Pressed += OptionsSelected;
    }

    private void LocalSelected()
    {
        GetTree().ChangeSceneToFile("res://scenes/chess_game.tscn");
    }

    private void OnlineSelected()
    {

    }

    private void OptionsSelected()
    {

    }
}
