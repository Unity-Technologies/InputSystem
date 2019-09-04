using UnityEngine;
using UnityEngine.UI;

namespace InputSamples.Demo.Drawing.UI
{
    /// <summary>
    /// Component that acts as a color selector for the UI
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ColorSwatch : MonoBehaviour
    {
        /// <summary>
        /// The color that this swatch represents
        /// </summary>
        [SerializeField]
        private Color color;

        /// <summary>
        /// The controller onto which we apply the color
        /// </summary>
        [SerializeField]
        private DrawingController controller;

        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (controller == null)
            {
                return;
            }
            controller.SetColor(color);
        }
    }
}
