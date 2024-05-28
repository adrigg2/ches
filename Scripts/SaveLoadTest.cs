using Godot;

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
        load.Size = new Vector2(768, 384);
        AddChild(load);
    }
}
