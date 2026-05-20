using UnityEngine;

namespace Player
{
    public class AudioManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStateSystem stateSystem;
        [SerializeField] private PeakSystem peakSystem;
        [SerializeField] private AudioSource audioSource;

        [Header("Clips")]
        [SerializeField] private AudioClip switchSound;
        [SerializeField] private AudioClip peakReachedSound;
        [SerializeField] private AudioClip instabilitySound;
        [SerializeField] private AudioClip interactionSound;

        private void Awake()
        {
            if (stateSystem == null) stateSystem = GetComponent<PlayerStateSystem>();
            if (peakSystem == null) peakSystem = GetComponent<PeakSystem>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (stateSystem != null)
            {
                stateSystem.OnStateChanged += HandleStateChanged;
                stateSystem.OnInstabilityStarted += HandleInstabilityStarted;
            }

            if (peakSystem != null)
            {
                peakSystem.OnPeakReached += HandlePeakReached;
            }
        }

        private void OnDisable()
        {
            if (stateSystem != null)
            {
                stateSystem.OnStateChanged -= HandleStateChanged;
                stateSystem.OnInstabilityStarted -= HandleInstabilityStarted;
            }

            if (peakSystem != null)
            {
                peakSystem.OnPeakReached -= HandlePeakReached;
            }
        }

        private void HandleStateChanged(PlayerState state)
        {
            PlaySound(switchSound);
        }

        private void HandleInstabilityStarted()
        {
            PlaySound(instabilitySound);
        }

        private void HandlePeakReached()
        {
            PlaySound(peakReachedSound);
        }

        public void PlayInteractionSound()
        {
            PlaySound(interactionSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
