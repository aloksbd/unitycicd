using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using TerrainEngine;
//using static System.Net.Mime.MediaTypeNames;

public class WelcomeUIController : MonoBehaviour
{
    private UIDocument m_UIDocument;
    private Label versionLabel;
    private VisualElement root;
    private VisualElement buttonsWrapper;

    private string[] buttonList = {
        "Resume Game",
        "Leaderboards",
#if UNITY_EDITOR
        "Elevator",
#endif
        "Exit"
    };


    private LatLonInput latLonInput;
    public static string buildingID = null;
    // Start is called before the first frame update

    void Start()
    {
        m_UIDocument = GetComponent<UIDocument>();

        root = m_UIDocument.rootVisualElement;

        VisualElement buttonContainer = root.Q<VisualElement>("menu-button-list");

        foreach (var buttonName in buttonList)
        {
            Button btn = SetupButton(buttonName);
            buttonContainer.Add(btn);
        }
#if UNITY_EDITOR
        latLonInput = new LatLonInput();
        SetupCreatorElement(buttonContainer);
        SetupGoElement(buttonContainer);
#endif
    }

    private void SetupCreatorElement(VisualElement buttonContainer)
    {
        VisualElement creatorElement = new VisualElement();
        creatorElement.AddToClassList("editor-element");
        TextField tf = new TextField();
        tf.name = "create-textfield";
        tf.value = "";
        tf.AddToClassList("editor-textfield");
        Button createbtn = SetupButton("Create");
        createbtn.style.width = StyleKeyword.Auto;
        createbtn.SetEnabled(false);
        tf.RegisterCallback<InputEvent>((evt) =>
        {
            if (evt.newData != "")
            {
                createbtn.SetEnabled(true);
            }
            else
            {
                createbtn.SetEnabled(false);
            }
        });
        creatorElement.Add(tf);
        creatorElement.Add(createbtn);
        buttonContainer.Add(creatorElement);
    }

    private void SetupGoElement(VisualElement buttonContainer)
    {
        VisualElement goElement = new VisualElement();
        goElement.AddToClassList("editor-element");
        TextField tf = new TextField();
        tf.name = "go-textfield";
        tf.value = "";
        tf.AddToClassList("editor-textfield");
        tf.AddToClassList("go-textfield");
        Button gobtn = SetupButton("Go");
        gobtn.style.width = StyleKeyword.Auto;
        gobtn.SetEnabled(false);
        tf.RegisterCallback<InputEvent>((evt) =>
        {
            if (latLonInput.Parse(evt.newData))
            {
                gobtn.SetEnabled(true);
            }
            else
            {
                gobtn.SetEnabled(false);
            }
        });
        goElement.Add(tf);
        goElement.Add(gobtn);
        buttonContainer.Add(goElement);
    }

    private Button SetupButton(string buttonName)
    {
        Button btn = new Button();
        btn.text = buttonName;
        btn.name = buttonName.ToLower().Replace(" ", "-") + "-button";
        btn.AddToClassList("menu-button");
        btn.AddToClassList("menu-button-list");
        btn.AddToClassList("main-menu-button1");
        btn.RegisterCallback<PointerUpEvent, string>(ButtonMethod, buttonName);
        btn.tooltip = buttonName;
        return btn;
    }

    void ButtonMethod(PointerUpEvent _evt, string name)
    {
        // Instantiates our primitive object on a left click.
        var primitiveTypeName = string.Concat("On", name.Replace(" ", ""));
        Type thisType = this.GetType();
        MethodInfo theMethod = thisType.GetMethod(primitiveTypeName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (theMethod != null)
        {
            theMethod.Invoke(this, new object[] { });
            // theMethod.Invoke(this, new object[1] { button });
        }
    }

    void OnResumeGame()
    {
        SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
    }

    void OnLeaderboards()
    {
        Application.OpenURL($"{WHConstants.WEB_URL}/leaderboard");
    }

    void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    async void OnCreate()
    {
        TextField tf = root.Q<TextField>("create-textfield");
        WelcomeUIController.buildingID = tf.value;
        SceneObject.Get().ActiveMode = SceneObject.Mode.Creator;
        if (CreatorUIController.getRoot() != null)
        {
            OsmBuildingData buildingData = await OsmBuildings.GetBuildingDetail(WelcomeUIController.buildingID);
            if (buildingData != null && buildingData.id != null)
            {
                CreatorUIController.CreateBuildingCanvas(buildingData);
            }
        }
    }
    void OnGo()
    {
        if (latLonInput.IsValid)
        {
            SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
            TerrainEngine.TerrainPresenter.TeleportToLatLong(latLonInput.Latitude, latLonInput.Longitude);
        }

    }

    void createButtonPressed()
    {
        SceneObject.Get().ActiveMode = SceneObject.Mode.Creator;
    }

    void OnElevator()
    {
        ElevatorController.EnterElevator(
            null,   //  TODO: Elevator wall gameObject
            40,     //  number of floors not counting roof
            18,     //  zero-based starting floor
            true,   //  has a lobby as the first floor.
            true);  //  allow roof access
    }

    // Update is called once per frame
    void Update()
    {

    }
}