using Godot;

namespace Chess;

[GlobalClass]
public partial class PieceTextures : Resource
{
    [Export]
    private Godot.Collections.Dictionary<string, Texture2D> WhiteTextures = new();
    
    [Export]
    private Godot.Collections.Dictionary<string, Texture2D> BlackTextures = new();

    public Texture2D GetWhiteTexture(string key)
    {
        return WhiteTextures[key];
    }

    public Texture2D GetBlackTexture(string key)
    {
        return BlackTextures[key];
    }
}
