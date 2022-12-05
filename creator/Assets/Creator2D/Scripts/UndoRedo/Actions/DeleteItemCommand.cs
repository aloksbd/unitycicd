using System;

public class DeleteItemCommand : ICommand
{
    protected string _name;
    protected bool _isFloorPlan;
    private Guid _id;

    public DeleteItemCommand(string name, bool isFloorPlan)
    {
        _name = name;
        _isFloorPlan = isFloorPlan;
    }

    public virtual void Execute()
    {
        try
        {
            var item = CreatorItemFinder.FindByName(_name);
            _id = item.Id;
            item.Destroy();
            if (_isFloorPlan)
            {
                CreatorUIController.SetupAddFloorDropdown();
                if (NewBuildingController.CurrentFloorPlan() != null && NewBuildingController.CurrentFloorPlan().name == _name)
                {
                    NewBuildingController.SetCurrentFloorPlan(null);
                    BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
                    buildingInventoryController.currentBlock = null;
                    buildingInventoryController.DeSelectAllObject();
                    //TODO: handle naming convention
                }
            }
        }
        catch (CreatorItemNotFounndException)
        {
            Trace.Log("Creator Item with given Name not found");
        }
    }

    public virtual void UnExecute()
    {
        var commands = NewUndoRedo.CommandsForItem(_id);
        foreach (var command in commands)
        {
            command.Execute();
        }
    }
}
