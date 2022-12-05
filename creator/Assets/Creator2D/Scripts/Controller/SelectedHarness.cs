using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SelectedHarness : VisualElement
{
    [UnityEngine.Scripting.Preserve]
    public new class UxmlFactory : UxmlFactory<SelectedHarness> { }

    // Selected harness component style classes
    private const string flip_horizontal = "Icons/flip_horizontal";
    private const string flip_vertical = "Icons/flip_vertical";
    private const string delete = "Icons/delete";
    private const string copy = "Icons/content_copy";
    private const string buttonClass = "harness_button";
    public SelectedHarness()
    {
        VisualElement mainWindow = new VisualElement();
        mainWindow.style.flexDirection = FlexDirection.Row;
        hierarchy.Add(mainWindow);

        VisualElement window2 = new VisualElement();
        mainWindow.Add(window2);

        Button CopyButton = new Button() { };
        var iconAsset2 = Resources.Load<Texture2D>(copy);
        CopyButton.style.backgroundImage = iconAsset2;

        CopyButton.AddToClassList(buttonClass);
        window2.Add(CopyButton);

        VisualElement window = new VisualElement();
        mainWindow.Add(window);

        Button LeftFlipButton = new Button() { };
        var iconAsset = Resources.Load<Texture2D>(flip_horizontal);
        LeftFlipButton.style.backgroundImage = iconAsset;

        LeftFlipButton.AddToClassList(buttonClass);
        window.Add(LeftFlipButton);


        VisualElement window1 = new VisualElement();
        mainWindow.Add(window1);

        Button TopFlipButton = new Button() { };
        var iconAsset1 = Resources.Load<Texture2D>(flip_vertical);
        TopFlipButton.style.backgroundImage = iconAsset1;

        TopFlipButton.AddToClassList(buttonClass);
        window1.Add(TopFlipButton);

        VisualElement window3 = new VisualElement();
        mainWindow.Add(window3);

        Button DeleteButton = new Button() { };
        var iconAsset3 = Resources.Load<Texture2D>(delete);
        DeleteButton.style.backgroundImage = iconAsset3;

        DeleteButton.AddToClassList(buttonClass);
        window3.Add(DeleteButton);

        // window.Add(HarnessButton1);
        // window.Add(HarnessButton2);

        // confirmButton.clicked += OnConfirm;
        // cancelButton.clicked += OnCancel;
        LeftFlipButton.clicked += OnLeftFlip;
        TopFlipButton.clicked += OnTopFlip;
        CopyButton.clicked += OnCopy;
        DeleteButton.clicked += OnDelete;

    }

    public event Action leftFlipped;
    public event Action topFlipped;
    public event Action Copied;
    public event Action Deleted;

    private void OnLeftFlip()
    {
        Debug.Log("left clicked");
        leftFlipped?.Invoke();
    }

    private void OnTopFlip()
    {
        Debug.Log("top clicked");
        topFlipped?.Invoke();
    }

    private void OnCopy()
    {
        Debug.Log("Rotate clicked");
        Copied?.Invoke();
    }

    private void OnDelete()
    {
        Debug.Log("Setting clicked");
        Deleted?.Invoke();
    }
}
