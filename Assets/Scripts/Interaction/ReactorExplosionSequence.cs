using System.Collections;
using UnityEngine;

namespace ReactorTechnician
{
    public sealed class ReactorExplosionSequence : MonoBehaviour
    {
        [SerializeField] private ReactorOverheatManager overheatManager;
        [SerializeField] private Transform reactorCore;
        [SerializeField] private ParticleSystem[] explosionParticles;
        [SerializeField] private Light flashLight;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private PlayerMovementController playerMovement;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private StationFeedbackUI feedbackUI;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float flashIntensity = 9f;
        [SerializeField] private float flashDuration = 0.55f;
        [SerializeField] private float shakeDuration = 1.2f;
        [SerializeField] private float shakeMagnitude = 0.35f;

        private bool played;
        private float shakeTimer;
        private float currentShakeMagnitude;
        private AudioClip explosionClip;

        private void Awake()
        {
            if (overheatManager == null)
            {
                overheatManager = FindAnyObjectByType<ReactorOverheatManager>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (feedbackUI == null)
            {
                feedbackUI = FindAnyObjectByType<StationFeedbackUI>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (flashLight != null)
            {
                flashLight.enabled = false;
            }

            explosionClip = CreateExplosionTone();
        }

        private void Update()
        {
            if (!played && overheatManager != null && overheatManager.CriticalFailure)
            {
                played = true;
                StartCoroutine(PlayExplosion());
            }
        }

        private void LateUpdate()
        {
            if (shakeTimer <= 0f || targetCamera == null)
            {
                return;
            }

            shakeTimer -= Time.deltaTime;
            float falloff = Mathf.Clamp01(shakeTimer / shakeDuration);
            targetCamera.transform.position += Random.insideUnitSphere * currentShakeMagnitude * falloff;
        }

        public void PlayNow()
        {
            if (played)
            {
                return;
            }

            played = true;
            StartCoroutine(PlayExplosion());
        }

        private IEnumerator PlayExplosion()
        {
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            if (playerInteractor != null)
            {
                playerInteractor.enabled = false;
            }

            if (reactorCore != null && flashLight != null)
            {
                flashLight.transform.position = reactorCore.position + Vector3.up * 2f;
            }

            if (explosionParticles != null)
            {
                for (int i = 0; i < explosionParticles.Length; i++)
                {
                    if (explosionParticles[i] != null)
                    {
                        explosionParticles[i].Play(true);
                    }
                }
            }

            if (audioSource != null && explosionClip != null)
            {
                audioSource.PlayOneShot(explosionClip);
            }

            feedbackUI?.ShowMessage("Reactor core detonation.");
            feedbackUI?.ShowFailure("The reactor overheated and exploded.\nPress R to restart the level.");

            shakeTimer = shakeDuration;
            currentShakeMagnitude = shakeMagnitude;

            if (flashLight != null)
            {
                flashLight.enabled = true;
                float timer = flashDuration;
                while (timer > 0f)
                {
                    timer -= Time.deltaTime;
                    flashLight.intensity = Mathf.Lerp(0f, flashIntensity, timer / flashDuration);
                    yield return null;
                }

                flashLight.enabled = false;
            }
        }

        private static AudioClip CreateExplosionTone()
        {
            const int sampleRate = 44100;
            const float duration = 0.7f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float fade = 1f - t / duration;
                float rumble = Mathf.Sin(2f * Mathf.PI * 75f * t);
                float crack = Mathf.Sin(2f * Mathf.PI * 31f * t + Mathf.Sin(t * 70f));
                samples[i] = (rumble * 0.22f + crack * 0.16f) * fade;
            }

            AudioClip clip = AudioClip.Create("Generated Reactor Explosion", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
