using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class BuildingDetailPanel : MonoBehaviour
{
    const string DETAILITEM_PANEL_NAME = "BuildingDetailItem";
    const string DETAILITEM_KEY_NAME = "Key";
    const string DETAILITEM_VALUE_NAME = "Value";

    //  Configured in Inspector
    public GameObject itemPrefab;

    //  Private metrics
    private Vector2 itemSize;
    private float padding;
    private int itemCount;
    private bool isOn = true;

    public enum Key
    {
        Name,
        Address,
        Location,
        Height,
        Levels,
        Status
    }

    struct KeyInfo
    {
        public KeyInfo(string keyText)
        {
            this.keyText = keyText;
        }
        public string keyText;
    }

    private static readonly Dictionary<Key, KeyInfo> s_keyInfo = new Dictionary<Key, KeyInfo>()
    {
        { Key.Name,     new KeyInfo("Name:") },
        { Key.Address,  new KeyInfo("Address:") },
        { Key.Location, new KeyInfo("Location:") },
        { Key.Height,   new KeyInfo("Height") },
        { Key.Levels,   new KeyInfo("Levels") },
        { Key.Status,   new KeyInfo("Status") },
    };

    private void InitMetrics(GameObject detailItemObj)
    {
        itemSize = detailItemObj.GetComponent<RectTransform>().sizeDelta;
        VerticalLayoutGroup layoutGroup = GetComponent<VerticalLayoutGroup>();
        padding = layoutGroup.padding.bottom;
    }

    public void Populate(ref TerrainEngine.ProceduralBuilding pb)
    {
        Clear();

        if (isOn)
        {
            OsmBuildingData data = pb.buildingData;

            string value;

            if (!Empty(data.details.name))
            {
                AddItem(Key.Name, data.details.name);
            }

            AddItem(Key.Location, String.Format("{0}, {1}", data.center.coordinates[1], data.center.coordinates[0]));

            if (!EmptyOrZero(data.details.buildingLevels, out value))
            {
                AddItem(Key.Levels, value);
            }

            if (!EmptyOrZero(data.details.height, out value))
            {
                AddItem(Key.Height, String.Format("{0}m", value));
            }

            if (!Empty(pb.statusDescription))
            {
                AddItem(Key.Status, pb.statusDescription);
            }
            DoLayout();
        }
    }

    public void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        itemCount = 0;
        DoLayout();
    }

    private void AddItem(Key k, string value)
    {
        GameObject detailItemObj = Instantiate(itemPrefab, gameObject.transform);
        if (itemSize.magnitude == 0)
        {
            InitMetrics(detailItemObj);
        }
        GameObject keyTextObj = detailItemObj.transform.Find(DETAILITEM_KEY_NAME).gameObject;
        GameObject valueTextObj = detailItemObj.transform.Find(DETAILITEM_VALUE_NAME).gameObject;

        SetKeyText(k, keyTextObj, value, valueTextObj);
        itemCount++;

        DoLayout();
    }

    public void SetKeyText(
        Key k, GameObject keyTextObj,
        string valueText, GameObject valueTextObj)
    {
        TextMeshProUGUI tmp;

        tmp = keyTextObj.GetComponent<TextMeshProUGUI>();
        tmp.text = s_keyInfo[k].keyText;

        tmp = valueTextObj.GetComponent<TextMeshProUGUI>();
        tmp.text = valueText;
    }

    private void DoLayout()
    {
        float sizeX = itemSize.x + (padding * 2);
        float sizeY = padding + (itemCount * itemSize.y);

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(sizeX, sizeY);

        gameObject.SetActive(itemCount != 0 && isOn);
    }

    private bool Empty(string value)
    {
        return value == null || value == "";
    }

    private bool EmptyOrZero(string value, out string fixedup)
    {
        if (value == null || value == "")
        {
            fixedup = "";
            return true;
        }

        fixedup = Regex.Replace(value, "[^0-9.]", "");
        if (Convert.ToDouble(fixedup) == 0)
        {
            return true;
        }
        return false;
    }

    public void OnToggleBuildingDetails(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            isOn = !isOn;
            DoLayout();
        }
    }

    public bool TryPopulate(GameObject gameObjectHit)
    {
        if (isOn && (!gameObject.activeSelf || itemCount == 0))
        {
            TerrainEngine.ProceduralBuilding pb = gameObjectHit.GetComponent<TerrainEngine.ProceduralBuilding>();
            if (pb != null)
            {
                Populate(ref pb);
            }
        }
        return false;
    }
}
