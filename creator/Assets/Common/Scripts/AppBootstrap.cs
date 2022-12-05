using UnityEngine;
using System.Collections;

public class AppBootstrap : MonoBehaviour
{
    //  Configured in inspector:
    public SceneObject.Mode DefaultStartupMode;
    bool SettingUp;

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
        StartCoroutine(AfterAuth());
    }

    IEnumerator AfterAuth()
    {
        SceneObject.Get().ActiveMode = DefaultStartupMode;

        yield return new WaitUntil(() => AuthenticationHandler.IsAuthenticated);

        DeeplinkHandler.Instance.Init();
        Trace.Assert(AuthenticationHandler.SecurelySaveToken(), "Failed to save token securely");
    }

    void OnApplicationQuit()
    {
        ServerSocket.CloseSocket();
        DeeplinkHandler.Instance.UnLinkDeeplink();
    }
}
