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
    private Button startButton;
    private Button settingsButton;
    private Button exitButton;
    private Label versionLabel;
    private VisualElement buttonsWrapper;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello");
        m_UIDocument = GetComponent<UIDocument>();

        VisualElement root = m_UIDocument.rootVisualElement;

        // buttonsWrapper = root.Q<VisualElement>("Buttons");

        startButton = root.Q<Button>("start-button");
        settingsButton = root.Q<Button>("settings-button");
        exitButton = root.Q<Button>("exit-button");
       // versionLabel = root.Q<Label>("TextVersion");

        startButton.clickable.clicked += StartButtonPressed;
        settingsButton.clicked += SettingsButtonPressed;
        exitButton.clicked += ExitButtonPressed;

    }

    void StartButtonPressed () {
        Debug.Log("I am clicked");
        SceneManager.LoadScene("Main");
    }

    void SettingsButtonPressed () {
        Debug.Log("SettingsButtonPressed::: I am clicked");
        // buttonsWrapper.Clear();
        // SceneManager.LoadScene("Main");
    }

    void ExitButtonPressed () {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
