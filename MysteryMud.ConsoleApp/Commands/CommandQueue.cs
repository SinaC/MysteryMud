namespace MysteryMud.ConsoleApp.Commands;

class CommandQueue
{
    public Queue<ICommand> Commands = new();
}
