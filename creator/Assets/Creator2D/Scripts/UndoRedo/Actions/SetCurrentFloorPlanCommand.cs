using System;
public class SetCurrentFloorPlanCommand : ICreatorItemCommand
{
    private string _previousFloorPlanName;
    private string _floorPlanName;
    private bool _setFloorPlan;
    public string Name { get => _floorPlanName; }

    private Guid _floorPlanid;
    public Guid Id { get => _floorPlanid; }

    public SetCurrentFloorPlanCommand(string floorPlanName, string previousFloorPlanName, bool setFloorPlan)
    {
        _floorPlanName = floorPlanName;
        _previousFloorPlanName = previousFloorPlanName;
        _setFloorPlan = setFloorPlan;
    }

    public void Execute()
    {
        if (_setFloorPlan)
        {
            SetFloorPlan(_floorPlanName);
        }
        else
        {
            UnSetFloorPlan(_floorPlanName);
        }
    }

    public void UnExecute()
    {
        if (_setFloorPlan)
        {
            SetFloorPlan(_previousFloorPlanName);
        }
        else
        {
            UnSetFloorPlan(_previousFloorPlanName);
        }
    }

    private void SetFloorPlan(string name)
    {
        try
        {
            var floorPlan = CreatorItemFinder.FindByName(name);
            _floorPlanid = floorPlan.Id;
            NewBuildingController.SetCurrentFloorPlan(floorPlan);
            floorPlan.gameObject.SetActive(true);
            if (_previousFloorPlanName != "")
            {
                var previousFloorPlan = CreatorItemFinder.FindByName(_previousFloorPlanName);
                previousFloorPlan.IsSelected = false;
                previousFloorPlan.uiItem.ToggleSelection(false);
                previousFloorPlan.gameObject.SetActive(!previousFloorPlan.gameObject.active);
            }
        }
        catch
        {
            // NewBuildingController.SetCurrentFloorPlan(null);
            Trace.Log("FloorPlan with given Name not found");
        }
    }

    private void UnSetFloorPlan(string name)
    {
        try
        {
            var floorPlan = CreatorItemFinder.FindByName(name);
            _floorPlanid = floorPlan.Id;
            NewBuildingController.SetCurrentFloorPlan(null);
            floorPlan.gameObject.SetActive(false);
        }
        catch
        {
            // NewBuildingController.SetCurrentFloorPlan(null);
            Trace.Log("FloorPlan with given Name not found");
        }
    }
}