using System.Collections.Generic;
using System;

public class NewUndoRedo
{
    private static Stack<ICommand> _undoStack = new Stack<ICommand>();
    private static Stack<ICommand> _redoStack = new Stack<ICommand>();

    public static void Redo()
    {
        if (_redoStack.Count != 0)
        {
            ICommand command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
        }
    }

    public static void Undo()
    {
        if (_undoStack.Count != 0)
        {
            ICommand command = _undoStack.Pop();
            command.UnExecute();
            _redoStack.Push(command);
        }
    }

    public static void AddAndExecuteCommand(ICommand command)
    {
        _undoStack.Push(command);
        _redoStack.Clear();
        command.Execute();
    }

    //For undoing delete command
    public static List<ICommand> CommandsForItem(Guid id)
    {
        List<ICommand> commands = new List<ICommand>();
        foreach (var command in _undoStack)
        {
            if (typeof(ICreatorItemCommand).IsAssignableFrom(command.GetType()))
            {
                var commandId = ((ICreatorItemCommand)command).Id;

                if (commandId == id)
                {
                    commands.Add(command);
                }
            }
        }
        return commands;
    }
}