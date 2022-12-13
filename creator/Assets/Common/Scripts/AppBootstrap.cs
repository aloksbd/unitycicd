using UnityEngine;
using System.Collections;

public class AppBootstrap : MonoBehaviour
{
    //  Configured in inspector:
    public SceneObject.Mode DefaultStartupMode;

    public void Awake()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            if (args[1] == "-install")
            {
                DeeplinkHandler.Instance.Init();
                Application.Quit();
                return;
            }
            if (args[1] == "-uninstall")
            {
                DeeplinkHandler.Instance.UnLinkDeeplink();
                PlayerPrefs.DeleteKey("access_token");
                Application.Quit();
                return;
            }
        }
        Init();
    }

    void Init()
    {
        AuthenticationHandler.Init();
        #if ADMIN
            SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
        #else
            SceneObject.Get().ActiveMode = DefaultStartupMode;
        #endif 
        StartCoroutine(AfterAuth());
    }

    IEnumerator AfterAuth()
    {
        yield return new WaitUntil(() => AuthenticationHandler.IsAuthenticated);
        DeeplinkHandler.Instance.Init();
    }

    void OnApplicationQuit()
    {
        ServerSocket.CloseSocket();
        DeeplinkHandler.Instance.UnLinkDeeplink();
    }
}
