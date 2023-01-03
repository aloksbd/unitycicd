using System;

public class DeleteItemCommand : ICommand
{
    protected System.Guid _name;
    protected bool _isFloorPlan;
    private Guid _id;

    public DeleteItemCommand(System.Guid name, bool isFloorPlan)
    {
        _name = name;
        _isFloorPlan = isFloorPlan;
    }

    public virtual void Execute()
    {
        try
        {
            var item = CreatorItemFinder.FindById(this._name);
            item.Destroy();
            if (_isFloorPlan)
            {
                CreatorUIController.SetupAddFloorDropdown();
                if (NewBuildingController.CurrentFloorPlan() != null && NewBuildingController.CurrentFloorPlan().name == item.name)
                {
                    NewBuildingController.SetCurrentFloorPlan(null);
                    BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
                    buildingInventoryController.currentBlock = null;
                    buildingInventoryController.DeSelectAllObject();
                    //TODO: handle naming convention
                }
            }
        }
        catch (CreatorItemNotFoundException)
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
