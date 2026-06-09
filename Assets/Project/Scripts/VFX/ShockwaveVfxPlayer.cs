using Player;
using UnityEngine;
using UnityEngine.VFX;

namespace VFX
{
    /// <summary>
    /// Plays Shockwave VFX in response to PeakEffectsHandler events.
    /// </summary>
    public class ShockwaveVfxPlayer : MonoBehaviour
    {
        [SerializeField] private PeakEffectsHandler peakEffectsHandler;
        [SerializeField] private VisualEffect shockwaveVfx;

        private void Awake()
        {
            if (peakEffectsHandler == null)
                peakEffectsHandler = GetComponent<PeakEffectsHandler>();

            if (shockwaveVfx == null)
                shockwaveVfx = GetComponentInChildren<VisualEffect>();
        }

        private void OnEnable()
        {
            if (peakEffectsHandler != null)
                peakEffectsHandler.OnShockwaveTriggered += HandleShockwaveTriggered;
        }

        private void OnDisable()
        {
            if (peakEffectsHandler != null)
                peakEffectsHandler.OnShockwaveTriggered -= HandleShockwaveTriggered;
        }

        private void HandleShockwaveTriggered(Vector3 position, float radius)
        {
            if (shockwaveVfx == null) return;

            shockwaveVfx.transform.position = position;
            shockwaveVfx.Play();
        }
    }
}
