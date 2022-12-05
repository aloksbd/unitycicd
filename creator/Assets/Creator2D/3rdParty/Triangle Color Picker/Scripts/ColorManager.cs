using UnityEngine;
using UnityEngine.UI;

namespace TriangleColorPicker
{
    [HelpURL("https://assetstore.unity.com/packages/slug/226108")]
    public class ColorManager : MonoBehaviour
    {
        [SerializeField]
        private Slider[] sliders;

        [SerializeField]
        private Text[] values;

        [SerializeField]
        private Renderer colorCircle;

        [SerializeField]
        private Transform handle, colors;

        [SerializeField]
        private Image colorBox;

        [SerializeField]
        private Text codeHTML;

        [SerializeField]
        private Rigidbody2D rb;

        [SerializeField]
        private float maxColliderSize;

        private Camera cam;
        private BoxCollider boxCollider;
        private Color selectedColor = Color.red, finalColor;
        private Vector2 innerDelta, localPosition;
        private float startColliderSize, v, w, u;
        private int clickPlace;

        /// <summary>
        /// Saving the selected color using PlayerPrefs
        /// </summary>
        public void Save()
        {
            PlayerPrefs.SetFloat("R", sliders[0].value);
            PlayerPrefs.SetFloat("G", sliders[1].value);
            PlayerPrefs.SetFloat("B", sliders[2].value);
        }

        /// <summary>
        /// Color selection using an already prepared set of colors
        /// </summary>
        /// <param name="index"></param>
        public void SetFinalColor(int index)
        {
            finalColor = colors.GetChild(index).GetComponent<Image>().color;
            colorBox.color = finalColor;
            SetValues();
        }

        /// <summary>
        /// Displaying the parameters of the selected color
        /// </summary>
        private void SetValues()
        {
            codeHTML.text = ColorUtility.ToHtmlStringRGB(finalColor);
            sliders[0].value = finalColor.r;
            values[0].text = ((int)(sliders[0].value * 255)).ToString();
            sliders[1].value = finalColor.g;
            values[1].text = ((int)(sliders[1].value * 255)).ToString();
            sliders[2].value = finalColor.b;
            values[2].text = ((int)(sliders[2].value * 255)).ToString();
        }

        /// <summary>
        /// Preparing for script work
        /// </summary>
        private void Start()
        {
            cam = Camera.main;
            boxCollider = GetComponent<BoxCollider>();
            startColliderSize = boxCollider.size.x;
        }

        /// <summary>
        /// Tracking clicks on ColorSelector
        /// </summary>
        private void Update()
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                localPosition = transform.InverseTransformPoint(hit.point);
        }

        /// <summary>
        /// Determining which place the user clicked
        /// </summary>
        private void OnMouseDown()
        {
            float distance = (Vector2.zero - localPosition).magnitude;
            clickPlace = distance > 0.2f ? (distance < 0.3f ? 1 : 0) : 2;
            boxCollider.size = new Vector3(maxColliderSize, maxColliderSize, boxCollider.size.z);
        }

        /// <summary>
        /// Updating the selected color
        /// </summary>
        private void OnMouseDrag()
        {
            switch (clickPlace)
            {
                case 1:
                    float angle = Mathf.Atan2(localPosition.x, localPosition.y);
                    float angleGrad = angle * (180f / Mathf.PI);
                    if (angleGrad < 0)
                        angleGrad = 360 + angleGrad;
                    angleGrad /= 60;
                    selectedColor = Color.black;
                    float v2 = 1f - (angleGrad - (int)angleGrad);
                    float v3 = 1f - v2;
                    switch ((int)angleGrad)
                    {
                        case 0:
                            selectedColor.r = 1;
                            selectedColor.g = v3;
                            break;
                        case 1:
                            selectedColor.r = v2;
                            selectedColor.g = 1;
                            break;
                        case 2:
                            selectedColor.g = 1;
                            selectedColor.b = v3;
                            break;
                        case 3:
                            selectedColor.g = v2;
                            selectedColor.b = 1;
                            break;
                        case 4:
                            selectedColor.r = v3;
                            selectedColor.b = 1;
                            break;
                        case 5:
                            selectedColor.r = 1;
                            selectedColor.b = v2;
                            break;
                    }
                    colorCircle.material.SetColor("_Color", selectedColor);
                    handle.localPosition = new Vector2(0.225f * Mathf.Sin(angle), 0.225f * Mathf.Cos(angle));
                    CalculateColorParameters(innerDelta - new Vector2(0, 0.125f), ref v, ref w, ref u);
                    finalColor = new Color(v * selectedColor.r + w, v * selectedColor.g + w, v * selectedColor.b + w);
                    colorBox.color = finalColor;
                    break;
                case 2:
                    CalculateColorParameters(localPosition - new Vector2(0, 0.125f), ref v, ref w, ref u);
                    rb.MovePosition(localPosition * 3 + new Vector2(0, 0.8f));
                    innerDelta = localPosition;
                    break;
            }
            finalColor = new Color(v * selectedColor.r + w, v * selectedColor.g + w, v * selectedColor.b + w);
            colorBox.color = finalColor;
            SetValues();
        }

        /// <summary>
        /// Restriction of the clickable zone when releasing the finger
        /// </summary>
        private void OnMouseUp()
        {
            boxCollider.size = new Vector3(startColliderSize, startColliderSize, boxCollider.size.z);
        }

        /// <summary>
        /// Calculate color parameters
        /// </summary>
        /// <param name="point"></param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="w"></param>
        private void CalculateColorParameters(Vector2 point, ref float u, ref float v, ref float w)
        {
            float d20 = Vector2.Dot(point, new Vector2(-0.145f, -0.27f));
            float d21 = Vector2.Dot(point, new Vector2(0.145f, -0.27f));
            v = 15.3322f * d20 - 8.4816f * d21;
            w = 15.3322f * d21 - 8.4816f * d20;
            u = 1f - v - w;
        }
    }
}