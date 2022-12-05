using UnityEngine;
using UnityEngine.UIElements;
public class HarnessOptions
{
    public void flipHorizontally(GameObject item)
    {
        var itemRenderer = item.GetComponent<Renderer>();
        var bounds = itemRenderer.bounds;
        item.transform.RotateAround(bounds.center, Vector3.forward, 180);
    }

    public void flipVertically(GameObject item)
    {
        var itemRenderer = item.GetComponent<Renderer>();
        var bounds = itemRenderer.bounds;
        item.transform.RotateAround(bounds.center, Vector3.left, 180);
    }

    public void copy(GameObject item)
    {

    }

    public void delete(GameObject item)
    {

    }
}