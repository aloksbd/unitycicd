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
            //ConnectNodes(node);
            AttachNode(i, node);
        }
    }

    //Attach the common nodes while creating the wall footprint
    public void AttachNode(int position, Node node)
    {
        foreach (var n in Node.allNodeList)
        {
            if (n.Value != node && n.Value.nodeGO.activeInHierarchy)
            {
                var isClose = Vector3.Distance(n.Value.nodeGO.transform.position, node.nodeGO.transform.position) < HarnessConstant.NODE_ATTACH_DISTANCE;

                if (isClose)
                {
                    node.nodeGO.SetActive(false);
                    node = n.Value;
                    break;
                }
            }
        }
        nodes.Add(position, node);
        UpdateNodeListner(node);
    }

    public void HandleHovered(Node node, GameObject attachableNodeGO)
    {
        _attachableNodeGO = attachableNodeGO;
    }

    public void HandleExit(Node node)
    {
        ConnectNodes(node);
        Trace.Log($"WALL {this.wallGO.name} found DRAG Exit ");
    }

    public void DragStart(Vector3 data, Node node, GameObject attachableNodeGO)
    {
        _attachableNodeGO = attachableNodeGO;

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
            n.Value.nodeGO.transform.position = new Vector3(points.x, points.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
            n.Value.nodeGO.transform.localScale = new Vector3((wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, (wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, 0.05f);
            n.Value.nodeGO.transform.rotation = new Quaternion(0, 0, 0, 0);
        }
    }

    public void HandleReleased(Node node)
    {
        ConnectNodes(node);

        var pos0 = wallRenderer.GetPosition(0);
        var pos1 = wallRenderer.GetPosition(1);
        float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;

        NewBuildingController.UpdateWallHandle(this.wallItem.name, this.nodes, pos0, pos1, angle);
    }

    private void ConnectNodes(Node node)
    {
        if (_attachableNodeGO == null)
        {
            return;
        }
        var attachableNode = Node.allNodeList[_attachableNodeGO];

        foreach (var n in nodes)
        {
            if (n.Value == node)
            {
                if (n.Value != attachableNode)
                {
                    Trace.Log($"Remove {n.Value.nodeGO.name} Add {attachableNode.nodeGO.name}");
                    n.Value.nodeGO.SetActive(false);
                    nodes[n.Key] = attachableNode;

                    UpdateNodeListner(attachableNode);

                    break;
                }
            }
        }
    }

    private void UpdateNodeListner(Node node)
    {
        node.onNodeHovered += HandleHovered;
        node.onNodeExit += HandleExit;
        node.onNodeDrag += DragStart;
        node.onNodeReleased += HandleReleased;
    }
}