using Unity.Collections;
using UnityEngine;
using TMPro;
using System.Collections;

public class ElevatorDoor : MonoBehaviour
{
    [SerializeField] private Transform playerPos;
    [SerializeField] private TMP_Text floorNoTxt;
    public Transform PlayerT => playerPos;
    public int FloorNumber => myFloorNo;

    private bool allowdToUseElevator = false;
    private int myFloorNo;
    [SerializeField] private Transform buildingRef;

    private IEnumerator Start()
    {
        yield return null;
        string floorName = transform.parent.parent.parent.name;
        floorName = floorName.Substring(WHConstants.FLOOR_PLAN.Length);
        _ = int.TryParse(floorName, out myFloorNo);
        floorNoTxt.text = (myFloorNo == 1) ? "Lobby" : "Floor " + myFloorNo;

        myFloorNo -= 1;
        buildingRef = transform.parent.parent.parent.parent;
        name = buildingRef.name + "_" + myFloorNo;
    }

    private void Update()
    {
        if (allowdToUseElevator && Input.GetKeyUp(KeyCode.G))
        {
            ElevatorController.EnterElevator(buildingRef, -1, myFloorNo, true, true);

            allowdToUseElevator = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        allowdToUseElevator = true && string.Equals("Player", other.name);
        ElevatorController.OnPlayerEnterElevatorDoor(myFloorNo, allowdToUseElevator);
        ElevatorController.LoadBuildingAndFloorData(buildingRef, myFloorNo);
    }

    private void OnTriggerExit(Collider other)
    {
        allowdToUseElevator = false;
        ElevatorController.OnPlayerEnterElevatorDoor(myFloorNo, allowdToUseElevator);
    }

}
