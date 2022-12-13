using UnityEngine;

public static class HarnessConstant
{
    public const float MOVEMENT_SENSITIVITY = 0.5f;
    public const float DEFAULT_NODE_ZOFFSET = -0.1f;
    public const float HOVER_NODE_ZOFFSET = -0.4f;
    public const float DEFAULT_NODE_SIZE = 3f;
    public const float HOVER_NODE_SIZE = 3.5f;
    public const float NODE_ATTACH_DISTANCE = 0.4f;
    public static Color DEFAULT_WALL_COLOR = Color.black;
    public static Color DEFAULT_NODE_COLOR = new Color(166 / 255f, 166 / 255f, 166 / 255f, 0.2f);
    public static Color HOVER_HIGHLIGHT_COLOR = new Color(153 / 255f, 221 / 255f, 255 / 255f, 1f);
}