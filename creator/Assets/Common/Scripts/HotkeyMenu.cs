using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotkeyMenu : MonoBehaviour
{
    //
    //  Master hotkey list
    //
    //  Note that there may be duplicates. That's ok.
    //  In some scenarios a different description provides better clarity for the hotkey's
    //  behavior in the applicable context.  However, two or more hotkeys having the same
    //  key value should never appear together at the same time

    public enum Key
    {
        Build,
        Cancel,
        FlyDown,
        FlyUp,
        HotkeyMenu,
        GoBack,
        Leave,
        Lobby,
        MainMenu,
        Roof,
        ToggleDetails,
        ToggleFly,
        ToggleLocation,
    }

    private static readonly Dictionary<Key, KeyInfo> s_HotKeys = new Dictionary<Key, KeyInfo>()
    {
        { Key.Build,          new KeyInfo("E",   "Build") },
        { Key.Cancel,         new KeyInfo("ESC", "Cancel") },
        { Key.FlyDown,        new KeyInfo("LSH", "Fly Down") },
        { Key.FlyUp,          new KeyInfo("SPC", "Fly Up") },
        { Key.GoBack,         new KeyInfo("ESC", "Go Back") },
        { Key.HotkeyMenu,     new KeyInfo("H",   "Hotkey Help") },
        { Key.Leave,          new KeyInfo("ESC", "Leave") },
        { Key.Lobby,          new KeyInfo("L",   "Lobby") },
        { Key.MainMenu,       new KeyInfo("ESC", "MainMenu") },
        { Key.Roof,           new KeyInfo("R",   "Roof") },
        { Key.ToggleDetails,  new KeyInfo("T",   "+/- Details") },
        { Key.ToggleFly,      new KeyInfo("F",   "Toggle Flying") },
        { Key.ToggleLocation, new KeyInfo("L",   "+/- Location") },
    };

    private struct KeyInfo
    {
        public KeyInfo(string menuText, string helpText)
        {
            this.menuText = menuText;
            this.helpText = helpText;
        }

        public string menuText;
        public string helpText;
    }

    //
    //  Public interface

    //  Configured in Inspector:
    public GameObject itemPrefab;

    public void Populate(List<Key> orderedKeyList)
    {
        Clear();

        foreach (Key k in orderedKeyList)
        {
            GameObject menuItemObj = Instantiate(itemPrefab, gameObject.transform);
            if (itemSize.magnitude == 0)
            {
                InitMetrics(menuItemObj);
            }
            GameObject panelObj = menuItemObj.transform.Find(MENUITEM_PANEL_NAME).gameObject;
            GameObject helpObj = menuItemObj.transform.Find(MENUITEM_HELPTEXT_NAME).gameObject;
            GameObject keyTextObj = panelObj.transform.GetChild(0).gameObject;

            SetKeyText(k, keyTextObj, helpObj);
            items[k] = menuItemObj;
        }

        DoLayout();
    }

    public void ShowKey(Key k, bool show)
    {
        GameObject item;
        if (items.TryGetValue(k, out item))
        {
            if (item.activeSelf != show)
            {
                item.SetActive(show);
                DoLayout();
            }
        }
    }

    //
    //  Private implementation
    private const string MENUITEM_PANEL_NAME = "HotKeyPanel";
    private const string MENUITEM_HELPTEXT_NAME = "HelpText";
    private const string MENUITEM_KEYTEXT_NAME = "HotKey";

    private const int MENUITEM_HEIGHT_PADDING = 2;
    private const int MENUITEM_WIDTH_PADDING = 18;

    //  metrics
    private VerticalLayoutGroup layoutGroup;
    private Vector2 itemSize;
    private float padding;

    //  Game object collection
    private Dictionary<Key, GameObject> items = new Dictionary<Key, GameObject>();

    private void SetKeyText(Key k, GameObject keyTextObj, GameObject helpObj)
    {
        TextMeshProUGUI tmp;

        tmp = keyTextObj.GetComponent<TextMeshProUGUI>();
        tmp.text = s_HotKeys[k].menuText;

        tmp = helpObj.GetComponent<TextMeshProUGUI>();
        tmp.text = s_HotKeys[k].helpText;
    }

    private void InitMetrics(GameObject menuItemObj)
    {
        itemSize = menuItemObj.GetComponent<RectTransform>().sizeDelta;
        VerticalLayoutGroup layoutGroup = GetComponent<VerticalLayoutGroup>();
        padding = layoutGroup.padding.bottom;
        RectTransform rt = GetComponent<RectTransform>();
        rt.pivot = new Vector2(2, 1);
    }

    private void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        items.Clear();
    }

    private void DoLayout()
    {
        int itemCount = 0;
        foreach (GameObject item in items.Values)
        {
            if (item.activeSelf)
            {
                itemCount++;
            }
        }

        float sizeX = itemSize.x + (padding * 2);
        float sizeY = padding + (itemCount * (itemSize.y + MENUITEM_HEIGHT_PADDING));

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(sizeX, sizeY);
        rt.anchoredPosition = new Vector2(sizeX, sizeY);
    }
}
