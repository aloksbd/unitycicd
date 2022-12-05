using UnityEngine;
using UnityEngine.UIElements;

// Add KeyboardEventTest to a GameObject with a valid UIDocument.
// When the user presses a key, it will print the keyboard event properties to the console.
[RequireComponent(typeof(UIDocument))]
public class CreatorKeyEventManager : MonoBehaviour
{
    VisualElement root;
    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        // root.RegisterCallback<KeyUpEvent>(OnKeyUp, TrickleDown.TrickleDown);
        root.focusable = true;
        root.pickingMode = PickingMode.Position;
        root.Focus();

    }
    void OnKeyDown(KeyDownEvent ev)
    {
        var focusedElement = root.focusController.focusedElement as VisualElement;
        if (focusedElement != null && focusedElement is TextField)
        {
            return;
        }
        switch (ev.keyCode)
        {
            case KeyCode.Escape:
                SceneObject.Get().ActiveMode = SceneObject.Mode.Welcome;
                ev.StopPropagation();
                break;
            case KeyCode.E:
                if (SceneObject.Get().PrevActiveMode == SceneObject.Mode.Player)
                {
                    SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
                }
                ev.StopPropagation();
                break;
            default:
                break;
        }
    }
}