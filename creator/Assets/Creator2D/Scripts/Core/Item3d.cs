using UnityEngine;

public class Item3d : MonoBehaviour
{
    public static GameObject building;
    void OnEnable()
    {
        if (NewBuildingController.buildingCreated)
        {
            CreatorUIController.buildingGO = getBuildingGameObject();
            CreatorUIController.buildingGO.name = "Building_EDIT_" + CreatorUIController.buildingID;
            TerrainEngine.TerrainController terrainObj = TerrainEngine.TerrainController.Get();
            terrainObj.buildingGenerator.ReplaceBuilding();
        }
    }

    public static GameObject getBuildingGameObject()
    {
        building = SceneObject.Find(SceneObject.Mode.Player, ObjectName.BUILDING_CONTAINER);
        if (building != null && building.transform.childCount > 0)
        {
            UnityEngine.GameObject.Destroy(building.transform.GetChild(0).gameObject); // Destroy existing building
        }
        return GameObject3DCreator.Create(NewBuildingController.GetBuilding(), building);
    }
}
