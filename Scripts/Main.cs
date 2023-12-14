using Godot;
using System.Reflection.Metadata.Ecma335;

namespace Ches;
public partial class Main : Node
{
	public static GameSettings Settings { get; set; }

	private ChessGame _game;
	private GameUI _gameUI;

	public override void _Ready()
	{
		GetNode<GameSetup>("MainMenu/GameSetup").GameStarted += GameStarted;
	}

	private void GameStarted(GameSettings settings)
	{
		GetNode<Control>("MainMenu").QueueFree();
		PackedScene gameScene = (PackedScene)ResourceLoader.Load("res://scenes/chess_game.tscn");
		PackedScene gameUIScene = (PackedScene)ResourceLoader.Load("res://scenes/game_ui.tscn");

		_game = (ChessGame)gameScene.Instantiate();
		_gameUI = (GameUI)gameUIScene.Instantiate();

		Settings = settings;

		AddChild(_gameUI);
		AddChild(_game);

		_gameUI.GameRestarted += (a, b) => { };
    }
}
