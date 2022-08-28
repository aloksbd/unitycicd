using UnityEngine;
using UnityEngine.UI;


public class VersionShower : MonoBehaviour
{
    private void Awake(){
        if(TryGetComponent(out Text output))
            output.text = Application.version;
    }
}
 