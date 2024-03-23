using Godot;

namespace Ches;
[GlobalClass]
public partial class PieceTextures : Resource
{
    [Export]
    private Godot.Collections.Dictionary<Godot.Collections.Array, Texture2D> _textures;

    public PieceTextures() : this(null) { }

    public PieceTextures(Godot.Collections.Dictionary<Godot.Collections.Array, Texture2D> textures)
    {
        _textures = textures;
    }

    public Texture2D GetTexture(Godot.Collections.Array key)
    {
        return _textures[key];
    }

    public void AddTexture(Godot.Collections.Array key, Texture2D texture)
    {
        _textures.Add(key, texture);
    }
}
