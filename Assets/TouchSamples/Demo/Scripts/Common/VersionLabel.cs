using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace InputSamples.Demo
{
    [RequireComponent(typeof(Text))]
    public class VersionLabel : MonoBehaviour
    {
        private void Awake()
        {
            var textToDisplay = new StringBuilder();
            var label = GetComponent<Text>();

            textToDisplay.Append(Application.version);
            textToDisplay.Append(" - ");
            textToDisplay.Append(Application.unityVersion);

            label.text = textToDisplay.ToString();
        }
    }
}
