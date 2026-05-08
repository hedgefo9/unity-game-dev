using UnityEngine;
using UnityEngine.UI;

namespace ReactorTechnician
{
    public sealed class StationFeedbackUI : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private Text moduleCountText;
        [SerializeField] private Text flowStatusText;
        [SerializeField] private Text temperatureStatusText;
        [SerializeField] private Text reactorRiskText;
        [SerializeField] private Text exposureStatusText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultBodyText;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip messageClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private float messageDuration = 2.5f;

        private static readonly Color VictoryColor = new Color(0.45f, 1f, 0.72f, 1f);
        private static readonly Color FailureColor = new Color(1f, 0.32f, 0.24f, 1f);

        private float messageTimer;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            CreateFallbackAudioClips();
            HideResult();
        }

        private void Update()
        {
            if (messageText == null || !messageText.enabled)
            {
                return;
            }

            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f)
            {
                messageText.enabled = false;
                messageText.text = string.Empty;
            }
        }

        public void ShowMessage(string message)
        {
            if (messageText == null)
            {
                Debug.Log(message);
                return;
            }

            messageText.text = message;
            messageText.enabled = true;
            messageTimer = messageDuration;
            PlayClip(messageClip);
        }

        public void SetModuleCount(int count, int capacity)
        {
            if (moduleCountText == null)
            {
                return;
            }

            moduleCountText.enabled = true;
            moduleCountText.text = $"Modules: {count}/{capacity}";
        }

        public void SetFlowStatus(string status)
        {
            if (flowStatusText == null)
            {
                return;
            }

            bool hasStatus = !string.IsNullOrWhiteSpace(status);
            flowStatusText.enabled = hasStatus;
            flowStatusText.text = hasStatus ? status : string.Empty;
        }

        public void SetTemperatureStatus(string status)
        {
            SetOptionalText(temperatureStatusText, status);
        }

        public void SetReactorRisk(float risk01)
        {
            if (reactorRiskText == null)
            {
                return;
            }

            reactorRiskText.enabled = true;
            reactorRiskText.text = $"Reactor risk: {Mathf.RoundToInt(Mathf.Clamp01(risk01) * 100f)}%";
        }

        public void SetExposureStatus(string status)
        {
            SetOptionalText(exposureStatusText, status);
        }

        public void SetObjective(string objective)
        {
            SetOptionalText(objectiveText, objective);
        }

        public void ShowVictory(string body)
        {
            ShowResult("REACTOR STABILIZED", body, VictoryColor);
            PlayClip(victoryClip);
        }

        public void ShowFailure(string body)
        {
            ShowResult("CRITICAL FAILURE", body, FailureColor);
            PlayClip(failureClip);
        }

        public void HideResult()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void ShowResult(string title, string body, Color titleColor)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            if (resultTitleText != null)
            {
                resultTitleText.enabled = true;
                resultTitleText.text = title;
                resultTitleText.color = titleColor;
            }

            if (resultBodyText != null)
            {
                resultBodyText.enabled = true;
                resultBodyText.text = body;
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void CreateFallbackAudioClips()
        {
            if (messageClip == null)
            {
                messageClip = CreateTone("HUD Message Tone", 660f, 0.08f, 0.12f);
            }

            if (victoryClip == null)
            {
                victoryClip = CreateTone("HUD Victory Tone", 880f, 0.18f, 0.16f);
            }

            if (failureClip == null)
            {
                failureClip = CreateTone("HUD Failure Tone", 220f, 0.28f, 0.18f);
            }
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float fade = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * fade;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void SetOptionalText(Text target, string status)
        {
            if (target == null)
            {
                return;
            }

            bool hasStatus = !string.IsNullOrWhiteSpace(status);
            target.enabled = hasStatus;
            target.text = hasStatus ? status : string.Empty;
        }
    }
}
