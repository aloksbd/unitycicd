using UnityEngine;
using System.Collections.Generic;

public class WallListener
{
    public GameObject wallGO;
    public NewWall wallItem;
    public LineRenderer wallRenderer;
    public Dictionary<int, Node> nodes = new Dictionary<int, Node>();
    private bool _isAttachable = false;
    private GameObject _attachableNodeGO;

    public WallListener(GameObject go, NewWall item)
    {
        this.wallGO = go;
        this.wallItem = item;
        this.wallRenderer = this.wallGO.GetComponent<LineRenderer>();

        for (int i = 0; i < wallRenderer.positionCount; i++)
        {
            Node node = new Node(i, wallGO);
            AttachNode(i, node);
        }
    }

    //Attach the common nodes while creating the wall footprint
    public void AttachNode(int position, Node node)
    {
        foreach (var n in Node.allNodeList)
        {
            try
            {
                if (n.Value != node && n.Value.nodeGO.activeInHierarchy && !CreatorEventManager._lineRender)
                {
                    var isClose = Vector3.Distance(n.Value.nodeGO.transform.position, node.nodeGO.transform.position) < HarnessConstant.NODE_ATTACH_DISTANCE;

                    if (isClose)
                    {
                        Node.allNodeList.Remove(node.nodeGO);
                        GameObject.Destroy(node.nodeGO);
                        node = n.Value;
                        break;

                    }
                }
            }
            catch
            {
                Trace.Log($"Couldnot find wall ref");
            }
        }

        nodes.Add(position, node);
        UpdateNodeListner(node);
    }

    public void HandleHovered(Node node, GameObject attachableNodeGO)
    {
        _attachableNodeGO = attachableNodeGO;
    }

    public void HandleExit(Node node, GameObject attachableNodeGO)
    {
        _attachableNodeGO = attachableNodeGO;
    }

    public void DragStart(Vector3 data, Node node)
    {
        foreach (var n in nodes)
        {
            var points = wallRenderer.GetPosition(n.Key);

            if (n.Value == node)
            {
                wallRenderer.SetPosition(n.Key, new Vector3(data.x, data.y, -0.2f));
                this.wallGO.transform.position = new Vector3(data.x, data.y, HarnessConstant.HOVER_NODE_ZOFFSET);

                var pos0 = wallRenderer.GetPosition(0);
                var pos1 = wallRenderer.GetPosition(1);
                float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;

                var length = Vector3.Distance(pos0, pos1);
                float lineWidth = wallRenderer.endWidth;
                wallRenderer.SetPositions(new Vector3[] { pos0, pos1 });

                BoxCollider lineCollider = this.wallGO.gameObject.GetComponent<BoxCollider>();
                lineCollider.transform.parent = wallRenderer.transform;
                lineCollider.center = new Vector3(length / 2, 0.0f, 0.0f);
                lineCollider.size = new Vector3(length, lineWidth, 1f);

                this.wallGO.gameObject.transform.position = pos0;
                this.wallGO.gameObject.transform.eulerAngles = new Vector3(0, 0, angle);
            }

            //Update the position of the nodes based on the wall's new position
            n.Value.nodeGO.transform.position = new Vector3(points.x, points.y, HarnessConstant.HOVER_NODE_ZOFFSET);
            n.Value.nodeGO.transform.localScale = new Vector3((wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, (wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, 0.05f);
            n.Value.nodeGO.transform.rotation = new Quaternion(0, 0, 0, 0);
        }
    }

    public void HandleReleased(Node node, GameObject attachableNodeGO)
    {
        _attachableNodeGO = attachableNodeGO;
        ConnectNodes(node, attachableNodeGO);

        var pos0 = wallRenderer.GetPosition(0);
        var pos1 = wallRenderer.GetPosition(1);
        float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;

        NewBuildingController.UpdateWallHandle(this.wallItem.name, pos0, pos1, angle);
    }

    public void ConnectNodes(Node node, GameObject attachableGO)
    {
        if (attachableGO == null)
        {
            return;
        }
        var attachableNode = Node.allNodeList[attachableGO];

        foreach (var n in nodes)
        {
            if (n.Value == node)
            {
                if (n.Value != attachableNode)
                {
                    Node.allNodeList.Remove(n.Value.nodeGO);
                    GameObject.Destroy(n.Value.nodeGO);
                    nodes[n.Key] = attachableNode;

                    UpdateNodeListner(attachableNode);

                    break;
                }
            }
        }
    }

    public void DetachNodes(Node node)
    {
        //Determine the position of the node which is to be detached
        foreach (var key in nodes.Keys)
        {
            if (nodes[key] == node)
            {
                NewBuildingController.DetachNodes(wallItem.name, key);
                break;
            }
        }
    }


    public void UpdateNodeListner(Node node)
    {
        node.onNodeHovered += HandleHovered;
        node.onNodeExit += HandleExit;
        node.onNodeDrag += DragStart;
        node.onNodeReleased += HandleReleased;
        node.onNodeClicked += DetachNodes;
    }
}