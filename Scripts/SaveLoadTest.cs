using Godot;
using System;

namespace Ches;
public partial class SaveLoadTest : Node2D
{
	public override void _Ready()
	{
		GetNode<Button>("Save").Pressed += () => SaveManager.SaveGame(this);
		GetNode<Button>("Load").Pressed += () => LoadGames();
	}

	private void LoadGames()
	{
		PackedScene loadScreen = (PackedScene)ResourceLoader.Load("res://scenes/load_screen.tscn");
		LoadScreen load = (LoadScreen)loadScreen.Instantiate();
		AddChild(load);
	}
}
