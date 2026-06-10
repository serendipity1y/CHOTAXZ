using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Player;

namespace MiniPuzzles
{
    /// <summary>
    /// Full-screen overlay that hosts the active puzzle.
    /// </summary>
    public class PuzzleOverlay : MonoBehaviour
    {
        private static readonly Color HelpColor = new Color(0.55f, 0.58f, 0.65f);
        private static readonly Color FeedbackOkColor = new Color(0.55f, 0.90f, 0.60f);
        private static readonly Color FeedbackErrorColor = new Color(0.95f, 0.40f, 0.40f);

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI contextText;
        [SerializeField] private TextMeshProUGUI difficultyBadge;
        [SerializeField] private TextMeshProUGUI stateBadge;
        [SerializeField] private Image stateBadgeIcon;
        [SerializeField] private TextMeshProUGUI helpText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button giveUpButton;

        [Header("State badges (optional)")]
        [SerializeField] private Sprite yinBadge;
        [SerializeField] private Sprite yangBadge;

        [Header("Animation")]
        [SerializeField] private float animDuration = 0.2f;
        [SerializeField] private float feedbackClearDelay = 2.5f;

        private Action _onGiveUp;
        private Coroutine _animRoutine;
        private Coroutine _feedbackRoutine;
        private CursorLockMode _prevLock;
        private bool _prevCursorVisible;
        private float _prevTimeScale = 1f;

        public RectTransform ContentRoot => contentRoot;

        private void Awake()
        {
            if (giveUpButton != null)
                giveUpButton.onClick.AddListener(HandleGiveUp);
            EnsureHelpUI();
            ConfigureContentArea();
            HideImmediate();
        }

        public void Show(PuzzleType type, PuzzleContext context, Action onGiveUp)
        {
            _onGiveUp = onGiveUp;

            if (titleText != null) titleText.text = FormatName(type);
            if (contextText != null) contextText.text = context.contextLabel;
            if (difficultyBadge != null) difficultyBadge.text = context.difficulty.ToString();
            ApplyStateBadge(context.playerState);
            if (statusText != null) statusText.text = string.Empty;
            SetHelp(PuzzleHelpText.Short(type));
            ClearFeedbackImmediate();
            ConfigureContentArea();
            ConfigureStatusArea();

            _prevLock = Cursor.lockState;
            _prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            gameObject.SetActive(true);
            SetInteractable(true);
            Animate(0f, 1f);
        }

        public void Hide()
        {
            SetInteractable(false);
            Cursor.lockState = _prevLock;
            Cursor.visible = _prevCursorVisible;
            Time.timeScale = _prevTimeScale;
            Animate(1f, 0f, onComplete: () => gameObject.SetActive(false));
        }

        public void SetHelp(string help)
        {
            if (helpText == null) return;
            helpText.text = help ?? string.Empty;
            helpText.color = HelpColor;
        }

        public void SetFeedback(string message, bool isError)
        {
            if (feedbackText == null) return;

            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = null;
            }

            if (string.IsNullOrEmpty(message))
            {
                feedbackText.text = string.Empty;
                return;
            }

            feedbackText.text = message;
            feedbackText.color = isError ? FeedbackErrorColor : FeedbackOkColor;
            _feedbackRoutine = StartCoroutine(ClearFeedbackAfterDelay());
        }

        public void SetStatus(string status)
        {
            if (statusText != null) statusText.text = status;
        }

        public void ClearContent()
        {
            if (contentRoot == null) return;
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
        }

        private void ConfigureContentArea()
        {
            if (contentRoot == null) return;
            contentRoot.anchorMin = new Vector2(0.08f, 0.16f);
            contentRoot.anchorMax = new Vector2(0.92f, 0.82f);
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
        }

        private void ConfigureStatusArea()
        {
            if (statusText == null) return;
            var rt = statusText.rectTransform;
            rt.anchorMin = new Vector2(0.50f, 0.88f);
            rt.anchorMax = new Vector2(0.94f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            statusText.alignment = TextAlignmentOptions.MidlineRight;
            statusText.fontSize = 18f;
        }

        private void ApplyStateBadge(PlayerState state)
        {
            if (stateBadge != null) stateBadge.text = state.ToString();
            if (stateBadgeIcon == null) return;
            Sprite sprite = state == PlayerState.Yin ? yinBadge : yangBadge;
            stateBadgeIcon.sprite = sprite;
            stateBadgeIcon.enabled = sprite != null;
        }

        private void EnsureHelpUI()
        {
            if (panel == null) return;

            if (helpText == null)
            {
                helpText = CreateAnchoredLabel("HelpText",
                    new Vector2(0.06f, 0.085f), new Vector2(0.94f, 0.125f), 16f);
                helpText.fontStyle = FontStyles.Italic;
            }

            if (feedbackText == null)
            {
                feedbackText = CreateAnchoredLabel("FeedbackText",
                    new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.88f), 18f);
                feedbackText.fontWeight = FontWeight.SemiBold;
            }
        }

        private TextMeshProUGUI CreateAnchoredLabel(string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(panel, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            tmp.text = string.Empty;
            return tmp;
        }

        private void ClearFeedbackImmediate()
        {
            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = null;
            }
            if (feedbackText != null) feedbackText.text = string.Empty;
        }

        private IEnumerator ClearFeedbackAfterDelay()
        {
            float t = 0f;
            while (t < feedbackClearDelay)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (feedbackText != null) feedbackText.text = string.Empty;
            _feedbackRoutine = null;
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
                float eased = 1f - (1f - k) * (1f - k);
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
            return type switch
            {
                PuzzleType.ZIP => "ZIP",
                PuzzleType.Tango => "Tango",
                PuzzleType.Queens => "Queens",
                PuzzleType.LightsOut => "Lights Out",
                _ => type.ToString()
            };
        }
    }
}
