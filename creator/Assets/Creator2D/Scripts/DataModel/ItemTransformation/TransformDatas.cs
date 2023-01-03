using UnityEngine;
using System.Collections.Generic;

public class TransformDatas
{
    public static Dictionary<GameObject, Node> allNodeList = new Dictionary<GameObject, Node>();
    public static Dictionary<GameObject, WallListener> wallListenersList = new Dictionary<GameObject, WallListener>();

    private static WallTransform wall;
    public static WallTransform SelectedWall
    {
        set { wall = value; }
        get { return wall; }
    }

    private static Node node;
    public static Node SelectedNode
    {
        set { node = value; }
        get { return node; }
    }

    private static WallObjectTransformHandler wallObject;
    public static WallObjectTransformHandler SelectedWallObject
    {
        set { wallObject = value; }
        get { return wallObject; }
    }

    private static ObjectTransformHandler obj;
    public static ObjectTransformHandler SelectedObject
    {
        set { obj = value; }
        get { return obj; }
    }

    private static CreatorItem clipboard;
    public static CreatorItem ClipboardItem
    {
        set { clipboard = value; }
        get { return clipboard; }
    }
}