namespace MysteryMud.Infrastructure.Network;

public class TelnetState
{
    public int Width = 80;
    public int Height = 24;

    public bool Color = true;
    public bool Echo = true;

    public List<string> Terminals = [];

    public bool Mccp;
    public bool Gmcp;

    public ColorMode ColorMode = ColorMode.ANSI16;
}
