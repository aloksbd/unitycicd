using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MetaBlock", fileName = "new MetaBlock")]
public class MetaBlock : ScriptableObject
{
    public string BlockName;
    public Sprite BlockSprite;
    public Texture2D BlockTexture;
    public GameObject BlockObject;
    public string AssetType;
    public string CategoryName;
    public string Amount;
    public string IconName;
    public string TwoDViewIcon;
}