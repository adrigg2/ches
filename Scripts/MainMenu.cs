using Godot;
using System.Transactions;

namespace Ches;
public partial class MainMenu : Control
{
	public override void _Ready()
	{
        Button returnToMenu = GetNode<Button>("Return");
        returnToMenu.Pressed += ReturnSelected;

        // OPTIONS
        Button local = GetNode<Button>("Main/Local");
        Button online = GetNode<Button>("Main/Online");
        Button options = GetNode<Button>("Main/Options");
        OptionButton languageSelection = GetNode<OptionButton>("Options/Language");
        local.Pressed += LocalSelected;
        online.Pressed += OnlineSelected;
        options.Pressed += OptionsSelected;
        languageSelection.ItemSelected += LanguageSelected;
    }

    private void LocalSelected()
    {
        GetNode<Control>("Main").Visible = false;
        GetNode<Control>("Options").Visible = false;
        GetNode<Button>("Return").Visible = true;
        GetNode<Control>("GameSetup").Visible = true;
    }

    private void OnlineSelected()
    {
        
    }

    private void OptionsSelected()
    {
        GetNode<Control>("Main").Visible = false;
        GetNode<Control>("GameSetup").Visible = false;
        GetNode<Button>("Return").Visible = true;
        GetNode<Control>("Options").Visible = true;
    }

    private void ReturnSelected()
    {
        GetNode<Control>("Options").Visible = false;
        GetNode<Control>("GameSetup").Visible = false;
        GetNode<Button>("Return").Visible = false;
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
