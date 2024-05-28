using Godot;

namespace Ches;
public interface ISaveable
{
    Godot.Collections.Dictionary<string, Variant> Save();
    void Load(Godot.Collections.Dictionary<string, Variant> data);
}
