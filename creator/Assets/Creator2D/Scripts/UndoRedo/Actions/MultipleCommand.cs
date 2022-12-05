using System.Collections.Generic;

public class MultipleCommand : ICommand
{
    private List<ICommand> _commands;

    public MultipleCommand(List<ICommand> commands)
    {
        _commands = commands;
    }
    public void Execute()
    {
        foreach (var command in _commands)
        {
            command.Execute();
        }
    }

    public void UnExecute()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            ICommand command = _commands[i];
            command.UnExecute();
        }
    }
}