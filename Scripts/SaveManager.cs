using Godot;
using System;
using System.Linq;

namespace Ches;
public partial class SaveManager : Node
{
	public static void SaveGame(Node caller, string name = "")
	{
        using (var saveDir = DirAccess.Open("user://saves/"))
        {
            if (saveDir == null)
            {
                DirAccess.Open("user://").MakeDir("saves");
            }
        }

        if (name == "")
        {
            name = GenerateSaveName();
        }

		using var saveGame = FileAccess.Open($"user://saves/{name}", FileAccess.ModeFlags.Write);

		var saveNodes = caller.GetTree().GetNodesInGroup("to_save");
		foreach (var node in saveNodes)
		{
			if (string.IsNullOrEmpty(node.SceneFilePath)) continue;

			if (!node.HasMethod("Save")) continue;

			var nodeData = node.Call("Save");

			var jsonString = Json.Stringify(nodeData);

			saveGame.StoreLine(jsonString);
		}
	}

    public static void LoadGame(Node caller, string save)
    {
        if (!FileAccess.FileExists($"user://saves/{save}"))
        {
            return;
        }

        var saveNodes = caller.GetTree().GetNodesInGroup("to_save");
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
            caller.GetNode(nodeData["Parent"].ToString()).AddChild(newObject);
        }
    }

    private static string GenerateSaveName()
    {
        using var dir = DirAccess.Open("user://saves/");

        if (dir != null)
        {
            dir.ListDirBegin();
            string[] fileNames = dir.GetFiles();

            bool generated = false;
            int attempt = 1;
            string name = "savegame.save";
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
