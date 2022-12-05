using System;

public interface ICommand
{
    void Execute();
    void UnExecute();
}

// This is a tryout for unexecuting delete
// Idea is to find all the Icommand with Name and run them all in order just before the delete command
public interface ICreatorItemCommand : ICommand
{
    Guid Id { get; }
}