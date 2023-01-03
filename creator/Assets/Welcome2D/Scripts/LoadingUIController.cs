using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using TerrainEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class LoadingUIController : MonoBehaviour
{
    private UIDocument m_UIDocument;
    private Button submitBuildButton;
    private Button continueBuildButton;
    private Button discardBuildButton;
    List<Button> buttons = new List<Button>();
    public static Mode ActiveMode = Mode.PreviousBuildDetected;
    public enum Mode
    {
        PreviousBuildDetected = 0,
        UnsavedBuild,
        Saving,
        Submitting,
        NoBuildingDetected,
        UnsavedLogout,
    };

    public static OsmBuildingData osmBuildingData;
    public static string existingbuildingid;
    public static string newBuildingId;
    private VisualElement root;

    void Start()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        m_UIDocument = GetComponent<UIDocument>();

        root = m_UIDocument.rootVisualElement;
        VisualElement container = root.Q<VisualElement>("container");

        switch (ActiveMode)
        {
            case Mode.UnsavedLogout:
                LogoutWithoutSaving(container);
                break;
            case Mode.PreviousBuildDetected:
                BuildPreviousBuildDetected(container);
                break;
            case Mode.UnsavedBuild:
                BuildUnSaved(container);
                break;
            case Mode.Saving:
                BuildSaving(container);
                break;
            case Mode.Submitting:
                BuildSubmitting(container);
                break;
            case Mode.NoBuildingDetected:
                BuildNoBuildingDetected(container);
                break;
            default:
                break;
        }

    }

    private string labelClassName = "loading-label";
    private string subLabelClassName = "loading-sub-label";
    private string buttonClassName = "loading-button";

    private void BuildPreviousBuildDetected(VisualElement container)
    {
        Label title = new Label();
        title.text = "Previous Build detected!";
        title.AddToClassList(labelClassName);

        Label subTitle = new Label();
        subTitle.text = "You have unsubmitted build,. Continue build or submit build or discard previous build to start new build.";
        subTitle.AddToClassList(subLabelClassName);

        Button submitPreviousBuild = new Button();
        submitPreviousBuild.text = "Submit Previous Build";
        submitPreviousBuild.RegisterCallback<PointerUpEvent>((evt) =>
        {
            CreatorSubmission.SubmitCreatorChanges(true);
            gameObject.SetActive(false);
            SceneObject.Get().ActiveMode = SceneObject.Mode.Creator;
            CreatorUIController.CreateBuildingCanvas(osmBuildingData);
        });
        submitPreviousBuild.AddToClassList(buttonClassName);

        Button continuePreviousBuild = new Button();
        continuePreviousBuild.text = "Continue Previous Build";
        continuePreviousBuild.RegisterCallback<PointerUpEvent>((evt) =>
        {
            gameObject.SetActive(false); // gameObject will refer to current LoadingUI GameObject
            SceneObject.Get().ActiveMode = SceneObject.Mode.Creator;
        });
        continuePreviousBuild.AddToClassList(buttonClassName);

        Button discard = new Button();
        discard.text = "Discard and Build New";
        discard.RegisterCallback<PointerUpEvent>((evt) =>
        {
            CreatorSubmission.DeleteAllLocalCreation();
            gameObject.SetActive(false);
            SceneObject.Get().ActiveMode = SceneObject.Mode.Creator;
            CreatorUIController.CreateBuildingCanvas(osmBuildingData);
        });
        discard.AddToClassList(buttonClassName);

        container.Add(title);
        container.Add(subTitle);
        container.Add(submitPreviousBuild);
        container.Add(continuePreviousBuild);
        container.Add(discard);
    }

    private void BuildUnSaved(VisualElement container)
    {

        Label title = new Label();
        title.text = "UnSaved Build!";
        title.AddToClassList(labelClassName);

        Label subTitle = new Label();
        subTitle.text = "This build is not submitted yet. If build is completed, Submit the building.";
        subTitle.AddToClassList(subLabelClassName);

        Button resume = new Button();
        resume.text = "Resume";
        resume.RegisterCallback<PointerUpEvent>((evt) =>
        {
        });
        resume.AddToClassList(buttonClassName);

        Button submit = new Button();
        submit.text = "Submit";
        submit.RegisterCallback<PointerUpEvent>((evt) =>
        {
        });
        submit.AddToClassList(buttonClassName);

        Button draft = new Button();
        draft.RegisterCallback<PointerUpEvent>((evt) =>
        {
        });
        draft.AddToClassList(buttonClassName);

        Button discard = new Button();
        discard.text = "Discard Build";
        discard.RegisterCallback<PointerUpEvent>((evt) =>
        {
        });
        discard.AddToClassList(buttonClassName);

        container.Add(title);
        container.Add(subTitle);
        container.Add(resume);
        container.Add(submit);
        container.Add(draft);
        container.Add(discard);
    }

    private void LogoutWithoutSaving(VisualElement container)
    {
        Label title = new Label();
        title.text = "UnSaved Build!";
        title.AddToClassList(labelClassName);

        Label subTitle = new Label();
        subTitle.text = "Save the build. You may lose your progress!";
        subTitle.AddToClassList(subLabelClassName);

        Button resume = new Button();
        resume.text = "Resume";
        resume.RegisterCallback<PointerUpEvent>((evt) =>
        {
            var loadingUI = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.LOADING_UI);
            loadingUI.SetActive(false);
        });
        resume.AddToClassList(buttonClassName);

        Button discard = new Button();
        discard.text = "Discard & Logout";
        discard.RegisterCallback<PointerUpEvent>((evt) =>
        {
            AuthenticationHandler.Logout();
            var bootstrap = GameObject.Find(ObjectName.BOOTSTRAP_OBJECT).GetComponent<AppBootstrap>();
            bootstrap.Init();
            var loadingUI = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.LOADING_UI);
            loadingUI.SetActive(false);
        });
        discard.AddToClassList(buttonClassName);

        container.Add(title);
        container.Add(subTitle);
        container.Add(resume);
        container.Add(discard);
    }

    private void BuildSaving(VisualElement container)
    {
        Label title = new Label();
        title.text = "Saving";
        title.AddToClassList(labelClassName);

        //ADD Loader
        // SpriteRenderer spRenderer = gameObject.AddComponent<SpriteRenderer>();
        // spRenderer.sprite = Resources.Load<Sprite>("Welcome2D/Resources/Images/loader.png");

        container.Add(title);
    }

    private void BuildSubmitting(VisualElement container)
    {

        Label title = new Label();
        title.text = "Submitting";
        title.AddToClassList(labelClassName);

        //ADD Loader        

        container.Add(title);
    }

    private void BuildNoBuildingDetected(VisualElement container)
    {

        Label title = new Label();
        title.text = "No Building To Build!";
        title.AddToClassList(labelClassName);

        Button creatorCenter = new Button();
        creatorCenter.text = "Creator Center";
        creatorCenter.RegisterCallback<PointerUpEvent>((evt) =>
        {
        });
        creatorCenter.AddToClassList(buttonClassName);

        Button cancel = new Button();
        cancel.text = "Cancel";
        cancel.RegisterCallback<PointerUpEvent>((evt) =>
        {
        });
        cancel.AddToClassList(buttonClassName);

        container.Add(title);
        container.Add(creatorCenter);
        container.Add(cancel);
    }
}
