using UnityEngine;
using System.Collections;
using ImaginationOverflow.UniversalDeepLinking.Storage;

#if UNITY_STANDALONE_WIN 
using Microsoft.Win32;
#endif

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

                //Clearing the Registry
#if UNITY_STANDALONE_WIN
                var key = Registry.CurrentUser.OpenSubKey("Software", true);
                var classes = key.OpenSubKey("Classes", true);

                var config = ConfigurationStorage.Load();

                if (classes != null)
                {
                    var appkey = classes.OpenSubKey(config.DeepLinkingProtocols[0].Scheme);

                    if (appkey != null)
                    {
                        classes.DeleteSubKeyTree(config.DeepLinkingProtocols[0].Scheme);
                        appkey = null;
                    }
                }
#endif
                PlayerPrefs.DeleteKey("access_token");
                Application.Quit();
            }
        }
        Init();
    }

    public void Init()
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
