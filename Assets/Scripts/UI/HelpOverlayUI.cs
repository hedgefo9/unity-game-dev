using UnityEngine;
using UnityEngine.UI;

namespace ReactorTechnician
{
    public sealed class HelpOverlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text bodyText;

        private const string HelpText =
            "REACTOR TECHNICIAN HELP\n\n" +
            "Controls\n" +
            "WASD - move\n" +
            "Mouse - look\n" +
            "Space - jump\n" +
            "Shift - sprint\n" +
            "E - terminals and doors\n" +
            "F - valves\n" +
            "Q - pick up / install cooling module\n" +
            "H - show / hide this help\n" +
            "R - restart after failure\n\n" +
            "Map legend\n" +
            "Blue square - player\n" +
            "Cyan room - reactor hall / target node\n" +
            "Green room - module storage\n" +
            "Yellow room - valve room\n" +
            "Red room - overheated section\n" +
            "Purple room - terminal / access control\n\n" +
            "Coolant route\n" +
            "Open Supply, Service and Injector valves. Keep Waste Bypass closed. Active pipes glow blue and reduce temperature.";

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (bodyText != null)
            {
                bodyText.text = HelpText;
            }
        }

        private void Update()
        {
            if (ReactorInput.HelpPressed && panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }
    }
}
