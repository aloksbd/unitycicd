using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Splash : MonoBehaviour
{
    void Awake()
    {
        DeeplinkHandler.Instance.Init();
    }
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SplashScreen());
    }

    // Update is called once per frame
    void Update()
    {
    }

    IEnumerator SplashScreen()
    {
        /*
            -> Check whether the user is authenticated or not and proceed only if the
                user is authenticated
                
        AuthenticationHandler.Init();
        yield return new WaitUntil(() => AuthenticationHandler.IsAuthenticated);
        AuthenticationHandler.SecurelySaveToken();
        */
        yield return new WaitForSeconds(7);
        //SceneManager.LoadScene(DeeplinkHandler.Instance.SceneToLoad);
    }
}
