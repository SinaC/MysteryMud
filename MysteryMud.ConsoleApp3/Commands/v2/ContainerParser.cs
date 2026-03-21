namespace MysteryMud.ConsoleApp3.Commands.v2;

public class ContainerParser : IArgumentParser
{
    public bool TryParse(string input, out object result)
    {
        result = new ContainerArg { Name = input.Trim() };
        return true;
    }
}
