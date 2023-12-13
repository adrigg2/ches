using Godot;

namespace Ches;
[GlobalClass]
public partial class PieceTextures : Resource
{
    [Export]
    private Godot.Collections.Dictionary<string, Texture2D> _whiteTextures = new();
    
    [Export]
    private Godot.Collections.Dictionary<string, Texture2D> _blackTextures = new();

    public PieceTextures() : this(null, null) { }

    public PieceTextures(Godot.Collections.Dictionary<string, Texture2D> whiteTextures, Godot.Collections.Dictionary<string, Texture2D> blackTextures)
    {
        _whiteTextures = whiteTextures;
        _blackTextures = blackTextures;
    }

    public Texture2D GetWhiteTexture(string key)
    {
        return _whiteTextures[key];
    }

    public Texture2D GetBlackTexture(string key)
    {
        return _blackTextures[key];
    }
}
