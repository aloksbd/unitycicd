using UnityEngine;

public class PrefabFinder
{
    public static GameObject Find(string name)
    {
        GameObject gameObject = Object.Instantiate(Resources.Load("Prefabs/Items/" + name, typeof(GameObject))) as GameObject;
        return gameObject;
    }
}