namespace MysteryMud.ConsoleApp3.Commands.v2;

public interface IArgumentParser
{
    bool TryParse(string input, out object result);
}
