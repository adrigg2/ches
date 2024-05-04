using Ches;
using Godot;
using System;

public partial class SaveLoadTest : Node2D
{
	public override void _Ready()
	{
		GetNode<Button>("Save").Pressed += () => SaveManager.SaveGame(this);
		GetNode<Button>("Load").Pressed += () => SaveManager.LoadGame(this);
	}
}
