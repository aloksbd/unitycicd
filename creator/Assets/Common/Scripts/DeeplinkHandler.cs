using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class DeeplinkHandler
{
    public static DeeplinkHandler Instance { get { return Nested.instance; } }
    public string Uri { get; private set; }
    public string RawQueryString { get; private set; }
    public Dictionary<string, string> QueryString { get; private set; }
    public bool isDeeplinkCalled { get; private set; }
    public class PlayData
    {
        public static string latitude;
        public static string longitude;
    }
    public class BuildData
    {
        public static string building_id;
    }
    public void Init()
    {
        Trace.Log("DeeplinkHandler.Initialize");
        isDeeplinkCalled = false;
        ImaginationOverflow.UniversalDeepLinking.DeepLinkManager.Instance.LinkActivated += Instance_LinkActivated;
    }

    private void Instance_LinkActivated(ImaginationOverflow.UniversalDeepLinking.LinkActivation linkActivation)
    {
        isDeeplinkCalled = true;
        var url = linkActivation.Uri;
        var querystring = linkActivation.RawQueryString;
        var mode = linkActivation.QueryString["mode"];

        MonoBehaviour mono = GameObject.FindObjectOfType<MonoBehaviour>();

        if (mode == "launch")
        {
            Trace.Log("launch");
            NavigateToMode(SceneObject.Mode.Welcome);
        }
        if (mode == "play")
        {
            Trace.Log("play");

            PlayData.latitude = linkActivation.QueryString["latitude"];
            PlayData.longitude = linkActivation.QueryString["longitude"];

            NavigateToMode(SceneObject.Mode.Player);
        }

        if (mode == "build")
        {
            Trace.Log("build");

            BuildData.building_id = linkActivation.QueryString["building_id"];
            Trace.Log("building_id: " + BuildData.building_id);

            NavigateToMode(SceneObject.Mode.Creator);
        }

        if (mode == "admin")
        {
            //Admin
        }

        //UnLinkDeeplink();
    }

    static void NavigateToMode(SceneObject.Mode mode)
    {
        SceneObject.Get().ActiveMode = mode;
    }

    public void UnLinkDeeplink()
    {
        ImaginationOverflow.UniversalDeepLinking.DeepLinkManager.Instance.LinkActivated -= Instance_LinkActivated;
    }

    private class Nested
    {
        static Nested() { }

        internal static readonly DeeplinkHandler instance = new DeeplinkHandler();
    }

}