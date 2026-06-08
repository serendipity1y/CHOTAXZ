using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniPuzzles
{
    /// <summary>
    /// Full-screen overlay that hosts the active puzzle. Owns the header (puzzle name,
    /// context label, difficulty badge, player-state badge), a status line (timer /
    /// move counter), the "Give Up" button, and the content area puzzles parent into.
    /// Animates open/close with a 0.2s ease-out scale. Blocks input while open.
    /// </summary>
    public class PuzzleOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI contextText;
        [SerializeField] private TextMeshProUGUI difficultyBadge;
        [SerializeField] private TextMeshProUGUI stateBadge;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button giveUpButton;

        [Header("Animation")]
        [SerializeField] private float animDuration = 0.2f;

        private Action _onGiveUp;
        private Coroutine _animRoutine;
        private CursorLockMode _prevLock;
        private bool _prevCursorVisible;
        private float _prevTimeScale = 1f;

        /// <summary>Container that puzzles parent their generated grid into.</summary>
        public RectTransform ContentRoot => contentRoot;

        private void Awake()
        {
            if (giveUpButton != null)
            {
                giveUpButton.onClick.AddListener(HandleGiveUp);
            }
            HideImmediate();
        }

        /// <summary>
        /// Configures the header and shows the overlay with the open animation.
        /// </summary>
        public void Show(PuzzleType type, PuzzleContext context, Action onGiveUp)
        {
            _onGiveUp = onGiveUp;

            if (titleText != null) titleText.text = FormatName(type);
            if (contextText != null) contextText.text = context.contextLabel;
            if (difficultyBadge != null) difficultyBadge.text = context.difficulty.ToString();
            if (stateBadge != null) stateBadge.text = context.playerState.ToString();
            if (statusText != null) statusText.text = string.Empty;

            // Free the cursor for puzzle interaction; remember gameplay state to restore.
            _prevLock = Cursor.lockState;
            _prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Freeze the game; UI uses unscaled time so it keeps animating/updating.
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            gameObject.SetActive(true);
            SetInteractable(true);
            Animate(0f, 1f);
        }

        /// <summary>Hides the overlay with the close animation, then deactivates it.</summary>
        public void Hide()
        {
            SetInteractable(false);
            // Restore the cursor and time state gameplay had before the puzzle opened.
            Cursor.lockState = _prevLock;
            Cursor.visible = _prevCursorVisible;
            Time.timeScale = _prevTimeScale;
            Animate(1f, 0f, onComplete: () => gameObject.SetActive(false));
        }

        /// <summary>Sets the status line (e.g. timer or move counter). Empty to clear.</summary>
        public void SetStatus(string status)
        {
            if (statusText != null) statusText.text = status;
        }

        /// <summary>Removes every child currently parented under the content area.</summary>
        public void ClearContent()
        {
            if (contentRoot == null) return;
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }

        private void HandleGiveUp() => _onGiveUp?.Invoke();

        private void SetInteractable(bool on)
        {
            if (canvasGroup == null) return;
            canvasGroup.blocksRaycasts = on;
            canvasGroup.interactable = on;
        }

        private void Animate(float from, float to, Action onComplete = null)
        {
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(ScaleRoutine(from, to, onComplete));
        }

        private IEnumerator ScaleRoutine(float from, float to, Action onComplete)
        {
            RectTransform target = panel != null ? panel : (RectTransform)transform;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(from);

            float t = 0f;
            while (t < animDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / animDuration);
                float eased = 1f - (1f - k) * (1f - k); // ease-out quad
                float s = Mathf.Lerp(from, to, eased);
                target.localScale = new Vector3(s, s, 1f);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(from, to, eased);
                yield return null;
            }

            target.localScale = new Vector3(to, to, 1f);
            if (canvasGroup != null) canvasGroup.alpha = to;
            _animRoutine = null;
            onComplete?.Invoke();
        }

        private void HideImmediate()
        {
            SetInteractable(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            RectTransform target = panel != null ? panel : (RectTransform)transform;
            target.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }

        private static string FormatName(PuzzleType type)
        {
            switch (type)
            {
                case PuzzleType.ZIP: return "ZIP";
                case PuzzleType.Tango: return "Tango";
                case PuzzleType.Queens: return "Queens";
                case PuzzleType.LightsOut: return "Lights Out";
                default: return type.ToString();
            }
        }
    }
}
