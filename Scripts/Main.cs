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

		AddChild(_game);
		AddChild(_gameUI);

		_gameUI.GameRestarted += () => _game.Reset();
		_gameUI.DrawSelected += () => _game.AgreedDraw();
		_gameUI.GameSaved += SaveGame;
		_gameUI.GameReverted += (index) => _game.RevertGameStatus(index);

		_game.TurnChanged += (turn, count) => _gameUI.ChangeTurn(turn, count);
		_game.GameEnded += (loser) => _gameUI.GameEnded(loser);
    }

    private void SaveGame()
    {
        using var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);

        var nodesToSave = GetTree().GetNodesInGroup("to_save");
        foreach (Node node in nodesToSave)
        {
            if (string.IsNullOrEmpty(node.SceneFilePath))
            {
                continue;
            }

            if (!node.HasMethod("Save"))
            {
                continue;
            }

            var nodeData = node.Call("Save");

            var jsonString = Json.Stringify(nodeData);

            saveGame.StoreLine(jsonString);
        }
    }
}
