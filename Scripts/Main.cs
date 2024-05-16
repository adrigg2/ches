using Godot;
using Ches.Chess;
using System;
using System.Linq;

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

		_gameUI.GameRestarted += () => _game.Reset();
		_gameUI.DrawSelected += () => _game.AgreedDraw();
		_gameUI.GameSaved += SaveGame;
		_gameUI.GameReverted += (index) => _game.RevertGameStatus(index);

		_game.TurnChanged += (turn, count) => _gameUI.ChangeTurn(turn, count);
		_game.GameEnded += (loser) => _gameUI.GameEnded(loser);
		_game.TimersSet += (timer, player) => _gameUI.SetTimers(timer, player);

		AddChild(_game);
		AddChild(_gameUI);
    }

    private void SaveGame(string name = "")
    {
        using (var saveDir = DirAccess.Open("user://saves/"))
        {
            if (saveDir == null)
            {
                DirAccess.Open("user://").MakeDir("saves");
            }

            if (saveDir.GetFiles().Contains(name))
            {
                name = GenerateSaveName(name);
            }
            else if (name == "")
            {
                name = GenerateSaveName();
            }
        }


        using var saveGame = FileAccess.Open($"user://saves/{name}", FileAccess.ModeFlags.Write);

        var saveNodes = GetTree().GetNodesInGroup("to_save");
        foreach (var node in saveNodes)
        {
            if (string.IsNullOrEmpty(node.SceneFilePath)) continue;

            if (!node.HasMethod("Save")) continue;

            var nodeData = node.Call("Save");

            var jsonString = Json.Stringify(nodeData);

            saveGame.StoreLine(jsonString);
        }
    }

    private void LoadGame(string save)
    {
        if (!FileAccess.FileExists($"user://saves/{save}"))
        {
            return;
        }

        var saveNodes = GetTree().GetNodesInGroup("to_save");
        foreach (Node saveNode in saveNodes)
        {
            saveNode.QueueFree();
        }

        using var saveGame = FileAccess.Open($"user://saves/{save}", FileAccess.ModeFlags.Read);

        while (saveGame.GetPosition() < saveGame.GetLength())
        {
            var jsonString = saveGame.GetLine();

            var json = new Json();
            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.Print($"JSON Parse Error: {json.GetErrorMessage()} in {jsonString} at line {json.GetErrorLine()}");
                continue;
            }

            var nodeData = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)json.Data);

            var newObjectScene = GD.Load<PackedScene>(nodeData["Filename"].ToString());
            var newObject = newObjectScene.Instantiate<Node>();

            if (!newObject.HasMethod("Load")) continue;

            newObject.Call("Load", nodeData);
            GetNode(nodeData["Parent"].ToString()).AddChild(newObject);
        }
    }

    public static string GenerateSaveName(string name = "savegame.save")
    {
        using var dir = DirAccess.Open("user://saves/");

        if (dir != null)
        {
            dir.ListDirBegin();
            string[] fileNames = dir.GetFiles();

            bool generated = false;
            int attempt = 1;
            while (!generated)
            {
                if (fileNames.Contains(name))
                {
                    name = $"savegame{attempt}.save";
                }
                else
                {
                    generated = true;
                }
                attempt++;
            }
            return name;
        }
        else
        {
            throw new Exception("An error ocurred when loading saved games");
        }
    }
}
