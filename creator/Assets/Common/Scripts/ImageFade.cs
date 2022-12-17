using UnityEngine;
using UnityEngine.UI;

//
//  class ImageFade
//
//  Replacement for UnityEngine.UI.Image.CrossFade
//
//  Add this component to an Image component-bearing game object representing
//  the starting image, then invoke FadeIn to fade the image in to full opacity,
//  or FadeOut to fade the image to full transparency.

public class ImageFade : MonoBehaviour
{
    //
    //  Public interface

    public void FadeIn(float duration)
    {
        Begin(0f, 1f, duration);
    }

    public void FadeOut(float duration)
    {
        Begin(1f, 0f, duration);
    }

    public void Cancel(float alpha)
    {
        duration = 0;
        startTime = 0;
        fadeSource.color = new UnityEngine.Color(fadeSource.color.r, fadeSource.color.g, fadeSource.color.b, alpha);
    }

    //
    //  Internal implementation

    private Image fadeSource;
    private float alphaStart;
    private float alphaEnd;
    private float duration;
    private float startTime;
    
    private void Begin(float alphaStart, float alphaEnd, float duration)
    {
        fadeSource = GetComponent<Image>();
        Trace.Assert(fadeSource != null, "This component should be attached to a game object with an Image component");

        this.alphaStart = alphaStart;
        this.alphaEnd = alphaEnd;
        this.duration = duration;

        fadeSource.color = new Color(fadeSource.color.r, fadeSource.color.g, fadeSource.color.b, alphaStart);
        gameObject.SetActive(true);
        startTime = Time.timeSinceLevelLoad;
    }

    private void FixedUpdate()
    {
        if (startTime != 0 && duration != 0)
        {
            float t = (Time.timeSinceLevelLoad - startTime)/duration;

            float alpha = Mathf.Lerp(alphaStart, alphaEnd, t);
            fadeSource.color = new UnityEngine.Color(fadeSource.color.r, fadeSource.color.g, fadeSource.color.b, alpha);

            if (alpha == alphaEnd)
            {
                //  Deactivate (hide) the image if fully transparent
                if (alphaEnd == 0f) 
                {
                    gameObject.SetActive(false);
                }
                duration = 0;
                startTime = 0;
            }
        }
    }
}
