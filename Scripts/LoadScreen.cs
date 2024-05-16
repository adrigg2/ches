using Godot;
using System;

namespace Ches;
public partial class LoadScreen : Control
{
    [Export] private VBoxContainer _saveGames;

    public override void _Ready()
    {
        using var dir = DirAccess.Open("user://saves/");

        if (dir == null)
        {
            throw new Exception("An error ocurred when loading saved games");
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (dir.CurrentIsDir())
            {
                GD.Print("Found a directory in save games folder");
            }
            else if (fileName.EndsWith(".save"))
            {
                GD.Print($"Found a save game: {fileName}");
                CreateSaveButton(fileName);
            }
            fileName = dir.GetNext();
        }
    }

    private void CreateSaveButton(string saveName)
    {
        Button button = new Button();
        _saveGames.AddChild(button);
        button.Pressed += () =>
        {
            SaveManager.LoadGame(this, saveName);
            QueueFree();
        };

        button.Text = saveName[..saveName.LastIndexOf(".")];
    }
}
