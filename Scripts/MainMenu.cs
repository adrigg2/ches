using Godot;

namespace Ches;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
        Button local = GetNode<Button>("Main/Local");
        Button online = GetNode<Button>("Main/Online");
        Button options = GetNode<Button>("Main/Options");
        Button returnToMenu = GetNode<Button>("Options/Button");
        OptionButton languageSelection = GetNode<OptionButton>("Options/OptionButton");
        local.Pressed += LocalSelected;
        online.Pressed += OnlineSelected;
        options.Pressed += OptionsSelected;
        returnToMenu.Pressed += ReturnSelected;
        languageSelection.ItemSelected += LanguageSelected;
    }

    private void LocalSelected()
    {
        GetTree().ChangeSceneToFile("res://scenes/chess_game.tscn");
    }

    private void OnlineSelected()
    {

    }

    private void OptionsSelected()
    {
        GetNode<Control>("Main").Visible = false;
        GetNode<Control>("Options").Visible = true;
    }

    private void ReturnSelected()
    {
        GetNode<Control>("Options").Visible = false;
        GetNode<Control>("Main").Visible = true;
    }

    private void LanguageSelected(long index)
    {
        if (index == 0)
        {
            TranslationServer.SetLocale("en");
        }
        else if (index == 1)
        {
            TranslationServer.SetLocale("es");
        }
    }
}
