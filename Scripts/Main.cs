using Godot;
using System;

namespace Ches;
public partial class Main : Node
{
	public override void _Ready()
	{
		GetNode<GameSetup>("MainMenu/GameSetup").GameStarted += GameStarted;
	}

	private void GameStarted(GameSettings settings)
	{
		GetNode<Control>("MainMenu").QueueFree();
		PackedScene gameScene = (PackedScene)ResourceLoader.Load("res://scenes/chess_game.tscn");
		PackedScene gameUIScene = (PackedScene)ResourceLoader.Load("res://scenes/game_ui.tscn");

		Node game = gameScene.Instantiate();
		Node gameUI = gameUIScene.Instantiate();

		AddChild(gameUI);
		AddChild(game);
    }
}