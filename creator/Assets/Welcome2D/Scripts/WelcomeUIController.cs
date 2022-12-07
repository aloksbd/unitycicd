using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
//using static System.Net.Mime.MediaTypeNames;

public class WelcomeUIController : MonoBehaviour
{
    private UIDocument m_UIDocument;
    private Button exploreButton;
    private Button createButton;
    private Button elevatorButton;
    private Button settingsButton;
    private Button exitButton;
    private Label versionLabel;
    private VisualElement buttonsWrapper;
    // Start is called before the first frame update

    private void OnEnable()
    {
        m_UIDocument = GetComponent<UIDocument>();

        VisualElement root = m_UIDocument.rootVisualElement;

        exploreButton = root.Q<Button>("explore-button");
        createButton = root.Q<Button>("create-button");
        elevatorButton = root.Q<Button>("elevator-button");
        settingsButton = root.Q<Button>("settings-button");
        exitButton = root.Q<Button>("exit-button");

        exploreButton.clickable.clicked += exploreButtonPressed;
        createButton.clickable.clicked += createButtonPressed;
        elevatorButton.clickable.clicked += elevatorButtonPressed;
        settingsButton.clicked += SettingsButtonPressed;
        exitButton.clicked += ExitButtonPressed;
    }

    void exploreButtonPressed()
    {
        SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
    }

    void createButtonPressed()
    {
        SceneObject.Get().ActiveMode = SceneObject.Mode.Creator;
    }

    void elevatorButtonPressed()
    {
        SceneObject.Get().ActiveMode = SceneObject.Mode.Elevator;
    }

    void SettingsButtonPressed()
    {
        Debug.Log("SettingsButtonPressed::: I am clicked");
        // buttonsWrapper.Clear();
        // SceneManager.LoadScene("Main");
    }

    void ExitButtonPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else   
        Application.Quit();
#endif
    }

    // Update is called once per frame
    void Update()
    {

    }
}