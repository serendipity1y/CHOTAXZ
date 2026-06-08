using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniPuzzles
{
    /// <summary>
    /// ZIP path-tracing puzzle module. Self-contained. Shows only the start and end cells;
    /// the player drags from start, across orthogonally adjacent cells, to build a path.
    /// On pointer release the path is validated: it must start at start, end at end, and
    /// visit every cell exactly once.
    /// </summary>
    public class ZipPuzzle : MonoBehaviour, IMiniPuzzle
    {
        private static readonly Color CellColor = new Color(0.20f, 0.22f, 0.30f);
        private static readonly Color PathColor = new Color(0.30f, 0.55f, 0.95f);
        private static readonly Color StartColor = new Color(0.25f, 0.80f, 0.40f);
        private static readonly Color EndColor = new Color(0.90f, 0.35f, 0.35f);

        private int _size;
        private int _start;
        private int _end;
        private Image[] _images;
        private readonly List<int> _path = new();
        private bool _finished;
        private float _startTime;
        private PuzzleOverlay _overlay;
        private Action _onSolved;

        /// <inheritdoc />
        public PuzzleType Type => PuzzleType.ZIP;

        /// <inheritdoc />
        public void Begin(PuzzleContext context, PuzzleOverlay overlay, Action onSolved, Action onFailed)
        {
            _overlay = overlay;
            _onSolved = onSolved;
            _finished = false;
            _startTime = Time.unscaledTime;

            ZipLevelData data = ZipGenerator.Generate(context.difficulty, context.seed);
            _size = data.size;
            _start = data.startIndex;
            _end = data.endIndex;

            BuildGrid();
            ResetPath();
            overlay.SetStatus("Tap or drag green -> red");
        }

        private void Update()
        {
            if (_finished) return;
            _overlay.SetStatus($"Time: {Time.unscaledTime - _startTime:0.0}s");
        }

        private void BuildGrid()
        {
            var grid = PuzzleGrid.Create(transform as RectTransform, _size);
            _images = new Image[_size * _size];

            for (int i = 0; i < _size * _size; i++)
            {
                int index = i;
                Image img = PuzzleGrid.CreateCell(grid, out Button button, out _);
                button.transition = Selectable.Transition.None;
                _images[i] = img;

                var pointer = img.gameObject.AddComponent<ZipCellPointer>();
                pointer.Init(index, this);
            }
        }

        // --- Pointer flow (called by ZipCellPointer) -------------------------------
        // Hybrid input: tapping cells one-by-one and dragging both route through Extend,
        // so the puzzle is playable even when drag pointer-enter events are not delivered.

        internal void OnCellDown(int cell)
        {
            if (_finished) return;

            if (_path.Count == 0)
            {
                if (cell == _start) // path must begin at the start cell
                {
                    _path.Add(cell);
                    Repaint();
                }
                return;
            }

            if (cell == _start) // tapping start again restarts the trace
            {
                ResetPath();
                return;
            }

            Extend(cell);
        }

        // Called on drag-over. 'pressed' is true only while the pointer button is held.
        internal void OnCellEnter(int cell, bool pressed)
        {
            if (_finished || !pressed || _path.Count == 0) return;
            Extend(cell);
        }

        private void Extend(int cell)
        {
            int last = _path[_path.Count - 1];

            // Step back when targeting the previous cell (correct the path).
            if (_path.Count >= 2 && cell == _path[_path.Count - 2])
            {
                _path.RemoveAt(_path.Count - 1);
                Repaint();
                return;
            }

            if (_path.Contains(cell)) return;       // no revisits
            if (!IsAdjacent(cell, last)) return;     // must be orthogonally adjacent

            _path.Add(cell);
            Repaint();

            // Auto-validate: solved when every cell is visited and the path ends at end.
            if (_path.Count == _size * _size && _path[_path.Count - 1] == _end)
            {
                _finished = true;
                _overlay.SetStatus("Solved");
                _onSolved?.Invoke();
            }
        }

        // --- Helpers ---------------------------------------------------------------

        private void ResetPath()
        {
            _path.Clear();
            Repaint();
        }

        private void Repaint()
        {
            for (int i = 0; i < _images.Length; i++) _images[i].color = CellColorFor(i);
            foreach (int cell in _path)
                if (cell != _start && cell != _end) _images[cell].color = PathColor;
        }

        private Color CellColorFor(int cell)
        {
            if (cell == _start) return StartColor;
            if (cell == _end) return EndColor;
            return CellColor;
        }

        private bool IsAdjacent(int a, int b)
        {
            int ar = a / _size, ac = a % _size;
            int br = b / _size, bc = b % _size;
            return (ar == br && Math.Abs(ac - bc) == 1) || (ac == bc && Math.Abs(ar - br) == 1);
        }
    }

    /// <summary>
    /// Per-cell pointer forwarder for <see cref="ZipPuzzle"/> drag tracing. Added at runtime.
    /// </summary>
    public class ZipCellPointer : MonoBehaviour,
        IPointerDownHandler, IPointerEnterHandler
    {
        private int _cell;
        private ZipPuzzle _owner;

        /// <summary>Binds this forwarder to its cell index and owning puzzle.</summary>
        public void Init(int cell, ZipPuzzle owner)
        {
            _cell = cell;
            _owner = owner;
        }

        public void OnPointerDown(PointerEventData eventData) => _owner.OnCellDown(_cell);

        // pointerPress != null means a button is currently held (a drag is in progress).
        public void OnPointerEnter(PointerEventData eventData)
            => _owner.OnCellEnter(_cell, eventData.pointerPress != null);
    }
}
