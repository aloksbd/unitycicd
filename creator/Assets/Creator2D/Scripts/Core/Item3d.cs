using UnityEngine;

public class Item3d : MonoBehaviour
{
    public static GameObject building;
    void OnEnable()
    {
        building = SceneObject.Find(SceneObject.Mode.Player, ObjectName.BUILDING_CONTAINER);
        if (NewBuildingController.buildingCreated)
            getBuildingGameObject();
    }

    public static GameObject getBuildingGameObject()
    {

        if (building != null && building.transform.childCount > 0)
        {
            UnityEngine.GameObject.Destroy(building.transform.GetChild(0).gameObject); // Destroy existing building
        }
        return GameObject3DCreator.Create(NewBuildingController.GetBuilding(), SceneObject.Find(SceneObject.Mode.Player, ObjectName.BUILDING_CONTAINER));
    }
}
