using ObjectModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrentBuildingData", menuName = "Elevator/BuildingData")]
public class BuildingSO : ScriptableObject
{
    public List<FloorData> floorData = new List<FloorData>();
    public int numberOfFloors = 0;

    public async Task LoadData()
    {
        List<CreatorItem> linkedFloor;
        FloorData floorData;
        foreach (var floorPlan in NewBuildingController.GetBuilding().children)
        {
            linkedFloor = LinkedFloorPlan.GetChildItems(floorPlan);
            floorData = new FloorData();
            foreach (var item in linkedFloor)
            {
                //Debug.LogError("linkedFloor " + item.name + "\nChildren: " + item.children.Count, this);
            }

            numberOfFloors++;
        }
        return;
    }

    [System.Serializable]
    public class FloorData
    {
        public string name;
        public Transform floorGo;
        public ElevatorDoor elevator;
    }
}
