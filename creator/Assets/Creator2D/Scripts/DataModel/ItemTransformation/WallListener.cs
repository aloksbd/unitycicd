using UnityEngine;
using System.Collections.Generic;

public class WallListener
{
    public GameObject wallGO;
    public NewWall wallItem;
    public CreatorItem floorPlan;
    public LineRenderer wallRenderer;
    public Dictionary<int, Node> nodes = new Dictionary<int, Node>();
    private bool _isAttachable = false;

    public WallListener(GameObject go, NewWall item)
    {
        this.wallGO = go;
        this.wallItem = item;
        this.wallRenderer = this.wallGO.GetComponent<LineRenderer>();
    }

    //Attach the common nodes while creating the wall footprint
    public void AttachNode(int position, Node checking_node, CreatorItem floorPlan, bool attach = true)
    {
        foreach (var allnodes in TransformDatas.allNodeList)
        {
            if (allnodes.Value != checking_node && floorPlan == allnodes.Value.floor && attach)
            {
                var distance = Vector3.Distance(allnodes.Value.nodeGO.transform.position, checking_node.nodeGO.transform.position);
                var isClose = distance < HarnessConstant.NODE_ATTACH_THRESHOLD;

                if (isClose)
                {
                    TransformDatas.allNodeList.Remove(checking_node.nodeGO);
                    UnRegisterEvents(checking_node);
                    GameObject.Destroy(checking_node.nodeGO);
                    checking_node = allnodes.Value;
                    break;
                }
            }
        }

        if (!nodes.ContainsKey(position))
        {
            nodes.Add(position, checking_node);
        }
        else
        {
            nodes[position] = checking_node;
        }
        UpdateNodeListner(checking_node);

        //Reposition the wall end after updating the node
        checking_node.NodeDragged(checking_node.nodeGO.transform.position);
    }

    public void HandleHovered(Node node)
    {
    }

    public void HandleExit(Node node)
    {

    }

    public void Dragging(Vector3 data, Node node)
    {
        foreach (var n in nodes)
        {
            var points = wallRenderer.GetPosition(n.Key);

            if (n.Value == node)
            {
                wallRenderer.SetPosition(n.Key, new Vector3(data.x, data.y, WHConstants.DefaultZ));
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

    public void HandleReleased(Node node)
    {
        var pos0 = wallRenderer.GetPosition(0);
        var pos1 = wallRenderer.GetPosition(1);

        var dist = Vector3.Distance(pos0, pos1);
        bool attach = pos0 != pos1 && dist > HarnessConstant.WALL_LENGTH_THRESHOLD;

        float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;
        NewBuildingController.UpdateWallHandle(this.wallItem.name, pos0, pos1, angle, attach);

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
        //This makes sure that no multiple subscription is done to the same node
        UnRegisterEvents(node);

        node.onNodeHovered += HandleHovered;
        node.onNodeExit += HandleExit;
        node.onNodeDrag += Dragging;
        node.onNodeReleased += HandleReleased;
        node.OnNodeDetach += DetachNodes;
    }

    public void UnRegisterEvents(Node node)
    {
        node.onNodeHovered -= HandleHovered;
        node.onNodeExit -= HandleExit;
        node.onNodeDrag -= Dragging;
        node.onNodeReleased -= HandleReleased;
        node.OnNodeDetach -= DetachNodes;
    }
}