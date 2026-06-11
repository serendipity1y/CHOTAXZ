using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace MiniPuzzles
{
    /// <summary>
    /// ZIP path-tracing puzzle (LinkedIn-style). Drag from 1, visit numbered dots in order
    /// while filling every cell. Walls block crossing. Path shows direction arrows.
    /// </summary>
    public class ZipPuzzle : MonoBehaviour, IMiniPuzzle
    {
        private static readonly Color CellColor = new Color(0.20f, 0.22f, 0.30f);
        private static readonly Color PathColor = new Color(0.28f, 0.52f, 0.92f);
        private static readonly Color PathHeadColor = new Color(0.40f, 0.68f, 1.00f);
        private static readonly Color WaypointPendingColor = new Color(0.92f, 0.82f, 0.22f);
        private static readonly Color WaypointStartColor = new Color(0.22f, 0.78f, 0.42f);
        private static readonly Color WaypointEndColor = new Color(0.88f, 0.32f, 0.32f);
        private static readonly Color WaypointNextGlow = new Color(1.00f, 0.95f, 0.55f);
        private static readonly Color WallColor = new Color(0.95f, 0.95f, 0.98f);

        private int _size;
        private int _waypointCount;
        private int[] _waypointAtCell;
        private byte[] _wallMask;
        private Image[] _images;
        private TextMeshProUGUI[] _labels;
        private TextMeshProUGUI[] _arrows;
        private Outline[] _labelOutlines;
        private readonly List<int> _path = new();
        private int _nextWaypoint = 1;
        private bool _finished;
        private float _startTime;
        private int _lastStatusTenth = -1;
        private PuzzleOverlay _overlay;
        private Action _onSolved;

        public PuzzleType Type => PuzzleType.ZIP;

        public void Begin(PuzzleContext context, PuzzleOverlay overlay, Action onSolved, Action onFailed)
        {
            _overlay = overlay;
            _onSolved = onSolved;
            _finished = false;
            _startTime = Time.unscaledTime;

            ZipLevelData data = ZipGenerator.Generate(context.difficulty, context.seed);
            _size = data.size;
            _waypointCount = data.waypointCount;
            _waypointAtCell = data.waypointAtCell;
            _wallMask = data.wallMask;

            BuildGrid();
            ResetPath();
            UpdateProgressStatus();
        }

        private void Update()
        {
            if (_finished) return;
            float elapsed = Time.unscaledTime - _startTime;
            int tenth = (int)(elapsed * 10f);
            if (tenth == _lastStatusTenth) return;
            _lastStatusTenth = tenth;
            _overlay.SetStatus(_nextWaypoint <= _waypointCount
                ? $"Next dot: {_nextWaypoint}  |  {elapsed:0.0}s"
                : $"Fill remaining cells  |  {elapsed:0.0}s");
        }

        private void BuildGrid()
        {
            var grid = PuzzleGrid.Create(transform as RectTransform, _size);
            int cells = _size * _size;
            _images = new Image[cells];
            _labels = new TextMeshProUGUI[cells];
            _arrows = new TextMeshProUGUI[cells];
            _labelOutlines = new Outline[cells];

            for (int i = 0; i < cells; i++)
            {
                int index = i;
                Image img = PuzzleGrid.CreateCell(grid, out Button button, out TextMeshProUGUI label);
                button.transition = Selectable.Transition.None;
                _images[i] = img;
                _labels[i] = label;
                label.raycastTarget = false;

                int waypoint = _waypointAtCell[i];
                if (waypoint > 0)
                {
                    label.text = waypoint.ToString();
                    label.fontStyle = FontStyles.Bold;
                    label.fontSize = 26;
                }

                _arrows[i] = CreateArrowLabel(img.transform);
                _labelOutlines[i] = label.gameObject.AddComponent<Outline>();
                _labelOutlines[i].effectColor = new Color(1f, 0.9f, 0.2f, 0.9f);
                _labelOutlines[i].effectDistance = new Vector2(2f, -2f);
                _labelOutlines[i].enabled = false;
                CreateWallBars(img.transform, i);

                var pointer = img.gameObject.AddComponent<ZipCellPointer>();
                pointer.Init(index, this);
            }

            Repaint();
        }

        private static TextMeshProUGUI CreateArrowLabel(Transform parent)
        {
            var go = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Bottom;
            tmp.fontSize = 18;
            tmp.color = new Color(1f, 1f, 1f, 0.75f);
            tmp.raycastTarget = false;
            tmp.margin = new Vector4(0f, 0f, 0f, 4f);
            tmp.text = string.Empty;
            return tmp;
        }

        private void CreateWallBars(Transform cell, int index)
        {
            CreateWallBar(cell, "WallUp", new Vector2(0.05f, 0.92f), new Vector2(0.95f, 1f), (_wallMask[index] & ZipGenerator.WallUp) != 0);
            CreateWallBar(cell, "WallRight", new Vector2(0.92f, 0.05f), new Vector2(1f, 0.95f), (_wallMask[index] & ZipGenerator.WallRight) != 0);
            CreateWallBar(cell, "WallDown", new Vector2(0.05f, 0f), new Vector2(0.95f, 0.08f), (_wallMask[index] & ZipGenerator.WallDown) != 0);
            CreateWallBar(cell, "WallLeft", new Vector2(0f, 0.05f), new Vector2(0.08f, 0.95f), (_wallMask[index] & ZipGenerator.WallLeft) != 0);
        }

        private static void CreateWallBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, bool visible)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = WallColor;
            img.raycastTarget = false;
            go.SetActive(visible);
        }

        internal void OnCellDown(int cell)
        {
            if (_finished) return;

            if (_waypointAtCell[cell] == 1 && _path.Count > 0)
            {
                ResetPath();
                return;
            }

            if (_path.Count == 0)
            {
                if (_waypointAtCell[cell] == 1)
                    AddToPath(cell);
                else
                    _overlay.SetFeedback("Start on dot 1", true);
                return;
            }

            Extend(cell);
        }

        internal void OnCellEnter(int cell, bool pressed)
        {
            if (_finished || !pressed || _path.Count == 0) return;
            Extend(cell);
        }

        private void Extend(int cell)
        {
            int last = _path[_path.Count - 1];

            if (_path.Count >= 2 && cell == _path[_path.Count - 2])
            {
                _path.RemoveAt(_path.Count - 1);
                RecomputeNextWaypoint();
                Repaint();
                UpdateProgressStatus();
                return;
            }

            if (_path.Contains(cell)) return;
            if (!IsAdjacent(cell, last)) return;

            if (ZipGenerator.HasWall(_wallMask, last, cell, _size))
            {
                _overlay.SetFeedback("Wall blocks that move", true);
                return;
            }

            int waypoint = _waypointAtCell[cell];
            if (waypoint > 0 && waypoint != _nextWaypoint)
            {
                _overlay.SetFeedback($"Hit dot {_nextWaypoint} next (not {waypoint})", true);
                return;
            }

            AddToPath(cell);
        }

        private void AddToPath(int cell)
        {
            int waypoint = _waypointAtCell[cell];
            _path.Add(cell);

            if (waypoint > 0)
            {
                _nextWaypoint = waypoint + 1;
                if (waypoint < _waypointCount)
                    _overlay.SetFeedback($"Dot {waypoint} reached — go to {waypoint + 1}", false);
            }

            Repaint();
            UpdateProgressStatus();

            if (_path.Count == _size * _size)
            {
                if (_nextWaypoint > _waypointCount)
                {
                    _finished = true;
                    _overlay.SetFeedback("Solved!", false);
                    _overlay.SetStatus("Solved");
                    _onSolved?.Invoke();
                }
                else
                {
                    _overlay.SetFeedback($"Fill every cell — still need dot {_nextWaypoint}", true);
                }
            }
        }

        private void RecomputeNextWaypoint()
        {
            _nextWaypoint = 1;
            foreach (int c in _path)
            {
                int w = _waypointAtCell[c];
                if (w > 0 && w == _nextWaypoint) _nextWaypoint = w + 1;
            }
        }

        private void UpdateProgressStatus()
        {
            if (_finished) return;
            float elapsed = Time.unscaledTime - _startTime;
            if (_path.Count == 0)
                _overlay.SetStatus($"Start on dot 1  |  {elapsed:0.0}s");
            else if (_nextWaypoint <= _waypointCount)
                _overlay.SetStatus($"Next dot: {_nextWaypoint}  |  {elapsed:0.0}s");
            else
                _overlay.SetStatus($"Fill remaining cells  |  {elapsed:0.0}s");
        }

        private void ResetPath()
        {
            _path.Clear();
            _nextWaypoint = 1;
            Repaint();
            UpdateProgressStatus();
            _overlay.SetFeedback(string.Empty, false);
        }

        private void Repaint()
        {
            for (int i = 0; i < _images.Length; i++)
                PaintCell(i);

            for (int p = 0; p < _path.Count - 1; p++)
            {
                int from = _path[p];
                int to = _path[p + 1];
                _arrows[from].text = DirectionGlyph(from, to);
            }
            if (_path.Count > 0)
                _arrows[_path[_path.Count - 1]].text = string.Empty;
        }

        private void PaintCell(int i)
        {
            int w = _waypointAtCell[i];
            int pathIndex = _path.IndexOf(i);
            bool onPath = pathIndex >= 0;
            bool isHead = onPath && pathIndex == _path.Count - 1;
            bool completedWaypoint = w > 0 && w < _nextWaypoint;
            bool isNextTarget = w == _nextWaypoint && !onPath;

            _arrows[i].text = string.Empty;

            _labelOutlines[i].enabled = isNextTarget;

            if (onPath)
            {
                _images[i].color = isHead ? PathHeadColor : PathColor;
                if (w > 0)
                {
                    _labels[i].color = Color.white;
                    _labels[i].text = completedWaypoint ? $"{w} ✓" : w.ToString();
                }
                else
                {
                    _labels[i].color = new Color(1f, 1f, 1f, 0.5f);
                    _labels[i].text = string.Empty;
                }
                return;
            }

            _labels[i].text = w > 0 ? w.ToString() : string.Empty;
            _labels[i].color = Color.black;

            if (w > 0)
            {
                _images[i].color = isNextTarget
                    ? WaypointNextGlow
                    : w == 1 ? WaypointStartColor
                    : w == _waypointCount ? WaypointEndColor
                    : WaypointPendingColor;
            }
            else
            {
                _images[i].color = CellColor;
            }
        }

        private string DirectionGlyph(int from, int to)
        {
            int dr = to / _size - from / _size;
            int dc = to % _size - from % _size;
            if (dr < 0) return "▲";
            if (dr > 0) return "▼";
            if (dc > 0) return "▶";
            if (dc < 0) return "◀";
            return string.Empty;
        }

        private bool IsAdjacent(int a, int b)
        {
            int ar = a / _size, ac = a % _size;
            int br = b / _size, bc = b % _size;
            return (ar == br && Math.Abs(ac - bc) == 1) || (ac == bc && Math.Abs(ar - br) == 1);
        }
    }

    public class ZipCellPointer : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
    {
        private int _cell;
        private ZipPuzzle _owner;

        public void Init(int cell, ZipPuzzle owner)
        {
            _cell = cell;
            _owner = owner;
        }

        public void OnPointerDown(PointerEventData eventData) => _owner.OnCellDown(_cell);

        public void OnPointerEnter(PointerEventData eventData)
            => _owner.OnCellEnter(_cell, eventData.pointerPress != null);
    }
}
