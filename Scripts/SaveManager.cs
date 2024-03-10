using Godot;
using System;

namespace Ches;
public partial class SaveManager : Node
{
	public static void SaveGame(Node caller)
	{
		using var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);

		var saveNodes = caller.GetTree().GetNodesInGroup("to_save");
		foreach (var node in saveNodes)
		{
			if (string.IsNullOrEmpty(node.SceneFilePath)) continue;

			if (!node.HasMethod("Save")) continue;

			var nodeData = node.Call("Save");

			var jsonString = Json.Stringify(nodeData, "    ");

			saveGame.StoreLine(jsonString);
		}
	}
}
