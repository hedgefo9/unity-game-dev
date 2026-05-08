using UnityEngine;
using UnityEngine.UI;

namespace ReactorTechnician
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private Text promptText;

        public void SetPrompt(string message)
        {
            if (promptText == null)
            {
                return;
            }

            bool hasMessage = !string.IsNullOrWhiteSpace(message);
            promptText.enabled = hasMessage;
            promptText.text = hasMessage ? message : string.Empty;
        }
    }
}
