using UnityEngine;
using System;

public class GameObject3DCreator
{
    public static GameObject Create(CreatorItem item, GameObject parentObject)
    {
        var gameObject = new GameObject();
        gameObject.name = item.name;

        // While creating a child and adding to parent, 
        // transform adjust automatically so that child's world transform doesnot change.
        // If parent is in position 2,2,2 and child in position 0,0,0,
        // after setting child's transform.parent to parent's transform,
        // child's position will change to -2,-2,-2.
        // So we need to first set child's position then establish parent-child relation then set parent's position.

        foreach (var child in item.children)
        {
            var childGameObject = GameObject3DCreator.Create(child, gameObject);
        }

        try
        {
            var gameobjectCreator = item.GetComponent<IHas3DObject>();
            gameobjectCreator.GetGameObject().transform.parent = gameObject.transform;
        }
        catch
        {
            try
            {
                var meshGenerator = item.GetComponent<NewIHasMesh>();
                if (meshGenerator != null)
                {
                    MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    Material material = Resources.Load("Materials/WallMaterial") as Material;
                    meshRenderer.material = material;

                    meshFilter.mesh = meshGenerator.GetMesh();
                    MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = meshFilter.mesh;

                }
            }
            catch (Exception e)
            {
                Trace.Log(e.Message);
            }
        }

        var position = item.GetComponent<NewIHasPosition>().Position;
        position = new Vector3(position.x, position.z, position.y);
        gameObject.transform.position = position;
        try
        {
            var rotation = item.GetComponent<NewIHasRotation>().EulerAngles;
            rotation = new Vector3(rotation.x, rotation.y, rotation.z);
            gameObject.transform.Rotate(rotation);
        }
        catch (Exception e)
        {
            Trace.Log(e.Message);
        }

        try
        {
            var scale = item.GetComponent<NewIScalable>().Scale;
            gameObject.transform.localScale = scale;
        }
        catch (Exception e)
        {
            Trace.Log(e.Message);
        }

        try
        {
            var flipable = item.GetComponent<NewIFlipable>();
            position = flipable.GetAdjustedPositionFor3D();
            position = new Vector3(position.x, position.z, position.y);
            gameObject.transform.position = position;
        }
        catch (Exception e)
        {
            Trace.Log(e.Message);
        }

        gameObject.transform.parent = parentObject.transform;
        return gameObject;
    }
}
