using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotkeyMenu : MonoBehaviour
{
    const string MENUITEM_PANEL_NAME = "HotKeyPanel";
    const string MENUITEM_HELPTEXT_NAME = "HelpText";
    const string MENUITEM_KEYTEXT_NAME = "HotKey";

    const int    MENUITEM_HEIGHT_PADDING = 2;
    const int    MENUITEM_WIDTH_PADDING = 18;

    //  Configured in Inspector
    public GameObject itemPrefab;

    //  Private metrics
    private VerticalLayoutGroup layoutGroup;
    private Vector2 itemSize;
    private float padding;

    Dictionary<Key, GameObject> items = new Dictionary<Key, GameObject>();

    public enum Key
    {
        Build,
        FlyDown,
        FlyUp,
        HotkeyMenu,
        GoBack,
        Lobby,
        MainMenu,
        Roof,
        ToggleDetails,
        ToggleFly,
        ToggleLocation,
    }

    struct KeyInfo
    {
        public KeyInfo(string menuText, string helpText)
        {
            this.menuText = menuText;
            this.helpText = helpText;
        }

        public string menuText;
        public string helpText;
    }

    private static readonly Dictionary<Key, KeyInfo> s_HotKeys = new Dictionary<Key, KeyInfo>()
    {
        { Key.Build,          new KeyInfo("E",   "Build") },
        { Key.FlyDown,        new KeyInfo("LSH", "Fly Down") },
        { Key.FlyUp,          new KeyInfo("SPC", "Fly Up") },
        { Key.GoBack,         new KeyInfo("ESC", "Go Back") },
        { Key.HotkeyMenu,     new KeyInfo("H",   "Hotkey Help") },
        { Key.Lobby,          new KeyInfo("L",   "Lobby") },
        { Key.MainMenu,       new KeyInfo("ESC", "MainMenu") },
        { Key.Roof,           new KeyInfo("R",   "Roof") },
        { Key.ToggleDetails,  new KeyInfo("T",   "+/- Details") },
        { Key.ToggleFly,      new KeyInfo("F",   "Toggle Flying") },
        { Key.ToggleLocation, new KeyInfo("L",   "+/- Location") },
    };

    private void InitMetrics(GameObject menuItemObj)
    {
        itemSize = menuItemObj.GetComponent<RectTransform>().sizeDelta;
        VerticalLayoutGroup layoutGroup = GetComponent<VerticalLayoutGroup>();
        padding = layoutGroup.padding.bottom;
        RectTransform rt = GetComponent<RectTransform>();
        rt.pivot = new Vector2(2, 1);
    }

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
    
    public void SetKeyText(Key k, GameObject keyTextObj, GameObject helpObj)
    {
        TextMeshProUGUI tmp;

        tmp = keyTextObj.GetComponent<TextMeshProUGUI>();
        tmp.text = s_HotKeys[k].menuText;

        tmp = helpObj.GetComponent<TextMeshProUGUI>();
        tmp.text = s_HotKeys[k].helpText;
    }

    private void Clear()
    {
        foreach (Transform child in transform)
        {
            child.transform.parent = null;
            Destroy(child);
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

    // Start is called before the first frame update
    void Awake()
    {
        //  Subscribe to events that affect hotkeys
        PlayerController.OnPlayerInteractionModeChanged += OnPlayerInteractionModeChanged;
        PlayerController.OnPlayerLookingAtEnter += OnPlayerLookingAtEnter;
        PlayerController.OnPlayerLookingAtLeave += OnPlayerLookingAtLeave;

        List<Key> keys = new List<Key>() 
        { 
            Key.Build,
            Key.ToggleDetails,
            Key.ToggleFly, 
            Key.FlyUp, 
            Key.FlyDown, 
            Key.ToggleLocation,
            Key.MainMenu, 
            Key.HotkeyMenu 
        };
        Populate(keys);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnPlayerInteractionModeChanged(
        PlayerController.IAMode modeNew, 
        PlayerController.IAMode modePrev)
    {
        ShowKey(Key.FlyUp, modeNew == PlayerController.IAMode.MinecraftFlyAlways);
        ShowKey(Key.FlyDown, modeNew == PlayerController.IAMode.MinecraftFlyAlways);
    }

    private void OnPlayerLookingAtEnter(ref GameObject gameObject, ref RaycastHit hit)
    {
        TerrainEngine.ProceduralBuilding pb = gameObject.GetComponent<TerrainEngine.ProceduralBuilding>();

        ShowKey(Key.Build, pb != null);
        ShowKey(Key.ToggleDetails, pb != null);
    }

    private void OnPlayerLookingAtLeave(ref GameObject gameObject, ref RaycastHit hit)
    {
        ShowKey(Key.Build, false);
        ShowKey(Key.ToggleDetails, false);
    }
}
