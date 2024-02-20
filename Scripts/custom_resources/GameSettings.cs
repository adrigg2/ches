using Godot;

namespace Ches;
public partial class GameSettings : Resource
{
    public bool Timer { get; set; }
    public double Minutes { get; set; }
    public double Seconds { get; set; }

    public GameSettings(bool timer, double minutes = 0, double seconds = 0)
    {
        Timer = timer;

        if (timer)
        {
            Minutes = minutes;
            Seconds = seconds;
        }
    }
}
