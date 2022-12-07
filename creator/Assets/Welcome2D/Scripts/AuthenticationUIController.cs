using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class AuthenticationUIController : MonoBehaviour
{
    private UIDocument m_UIDocument;
    private Button authButton;

    private void OnEnable()
    {
        m_UIDocument = GetComponent<UIDocument>();

        VisualElement root = m_UIDocument.rootVisualElement;

        authButton = root.Q<Button>("authentication-button");
        authButton.clickable.clicked += AuthenticateButtonPressed;

        AuthenticationHandler.OnAuthenticationFailure += AuthenticationHandler_OnAuthenticationFailed;
        StartCoroutine(OnAuth());
    }

    public void AuthenticateButtonPressed()
    {
        authButton.SetEnabled(false);
        AuthenticationHandler.Authenticate();
    }

    IEnumerator OnAuth()
    {
        yield return new WaitUntil(() => AuthenticationHandler.IsAuthenticated);

        var obj = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.AUTHENTICATION_UI);
        obj.SetActive(false);

        var obj1 = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.WELCOME_UI);
        obj1.SetActive(true);
    }

    private void AuthenticationHandler_OnAuthenticationFailed()
    {
        authButton.text = "Retry";
        authButton.SetEnabled(true);
    }
}
