using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniPuzzles
{
    /// <summary>
    /// Stateless runtime UI helper for building a square grid and its cells from code,
    /// so no per-size cell prefab is needed. This is plain UI infrastructure — it is not
    /// a puzzle type and holds no game-system or inter-puzzle dependencies.
    /// </summary>
    public static class PuzzleGrid
    {
        /// <summary>
        /// Creates a stretched grid container with a <see cref="GridLayoutGroup"/> sized
        /// to fit <paramref name="size"/> x <paramref name="size"/> cells.
        /// </summary>
        public static RectTransform Create(RectTransform parent, int size, float maxExtent = 0f)
        {
            StretchFill(parent);

            if (maxExtent <= 0f)
            {
                float w = parent.rect.width > 0f ? parent.rect.width : 480f;
                float h = parent.rect.height > 0f ? parent.rect.height : 480f;
                maxExtent = Mathf.Min(w, h) * 0.95f;
            }

            var go = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float side = maxExtent;
            rt.sizeDelta = new Vector2(side, side);
            rt.anchoredPosition = Vector2.zero;

            float spacing = 6f;
            float cell = (side - spacing * (size - 1)) / size;

            var layout = go.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cell, cell);
            layout.spacing = new Vector2(spacing, spacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = size;
            layout.childAlignment = TextAnchor.MiddleCenter;

            return rt;
        }

        /// <summary>Anchors a RectTransform to fully fill its parent.</summary>
        public static void StretchFill(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        /// <summary>
        /// Creates one interactive cell with an optional centered icon Image (for sprites).
        /// </summary>
        public static Image CreateIconCell(RectTransform grid, out Button button, out Image icon)
        {
            Image img = CreateCell(grid, out button, out _);
            icon = CreateChildIcon(img.transform);
            return img;
        }

        /// <summary>
        /// Creates one interactive cell (Image + Button + centered Text) under the grid.
        /// </summary>
        public static Image CreateCell(RectTransform grid, out Button button, out TextMeshProUGUI label)
        {
            var go = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(grid, false);

            var img = go.GetComponent<Image>();
            img.color = Color.gray;

            button = go.GetComponent<Button>();
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var trt = (RectTransform)textGo.transform;
            trt.SetParent(go.transform, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            label = textGo.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = 48;
            label.color = Color.black;
            label.text = string.Empty;
            label.raycastTarget = false;

            return img;
        }

        private static Image CreateChildIcon(Transform parent)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)iconGo.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);

            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            return icon;
        }
    }
}
