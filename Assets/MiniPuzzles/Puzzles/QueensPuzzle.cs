using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniPuzzles
{
    /// <summary>
    /// Queens puzzle module. Self-contained. Cells are tinted by color region. Player taps
    /// to cycle empty -> queen -> empty. Conflicts (shared row, column, region, or any
    /// adjacency including diagonal) are highlighted in real time. Solved when exactly one
    /// queen sits in each row, column, and region with no conflicts.
    /// </summary>
    public class QueensPuzzle : MonoBehaviour, IMiniPuzzle
    {
        private int _size;
        private int[] _regionOf;
        private bool[] _queen;
        private Image[] _images;
        private TextMeshProUGUI[] _labels;
        private Color[] _regionColors;
        private bool _finished;
        private float _startTime;
        private PuzzleOverlay _overlay;
        private Action _onSolved;

        /// <inheritdoc />
        public PuzzleType Type => PuzzleType.Queens;

        /// <inheritdoc />
        public void Begin(PuzzleContext context, PuzzleOverlay overlay, Action onSolved, Action onFailed)
        {
            _overlay = overlay;
            _onSolved = onSolved;
            _finished = false;
            _startTime = Time.unscaledTime;

            QueensLevelData data = QueensGenerator.Generate(context.difficulty, context.seed);
            _size = data.size;
            _regionOf = (int[])data.regionOf.Clone();
            _queen = new bool[_size * _size];
            _regionColors = BuildRegionColors(_size);

            BuildGrid();
            Refresh();
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
            _labels = new TextMeshProUGUI[_size * _size];

            for (int i = 0; i < _size * _size; i++)
            {
                int index = i;
                Image img = PuzzleGrid.CreateCell(grid, out Button button, out TextMeshProUGUI label);
                _images[i] = img;
                _labels[i] = label;
                label.text = string.Empty;
                button.onClick.AddListener(() => OnCellClicked(index));
            }
        }

        private void OnCellClicked(int cell)
        {
            if (_finished) return;
            _queen[cell] = !_queen[cell];
            Refresh();

            if (IsSolved())
            {
                _finished = true;
                _onSolved?.Invoke();
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < _queen.Length; i++)
            {
                Color baseColor = _regionColors[_regionOf[i]];
                bool conflict = _queen[i] && HasConflict(i);
                _images[i].color = conflict ? Color.Lerp(baseColor, Color.red, 0.6f) : baseColor;
                _labels[i].text = _queen[i] ? "Q" : string.Empty;
            }
        }

        private bool HasConflict(int cell)
        {
            int r = cell / _size, c = cell % _size;
            for (int o = 0; o < _queen.Length; o++)
            {
                if (o == cell || !_queen[o]) continue;
                int or = o / _size, oc = o % _size;
                if (or == r) return true;                       // same row
                if (oc == c) return true;                       // same column
                if (_regionOf[o] == _regionOf[cell]) return true; // same region
                if (Math.Abs(or - r) <= 1 && Math.Abs(oc - c) <= 1) return true; // adjacency
            }
            return false;
        }

        private bool IsSolved()
        {
            int total = 0;
            var rowUsed = new bool[_size];
            var colUsed = new bool[_size];
            var regionUsed = new bool[_size];

            for (int i = 0; i < _queen.Length; i++)
            {
                if (!_queen[i]) continue;
                total++;
                if (HasConflict(i)) return false;
                int r = i / _size, c = i % _size;
                if (rowUsed[r] || colUsed[c] || regionUsed[_regionOf[i]]) return false;
                rowUsed[r] = colUsed[c] = regionUsed[_regionOf[i]] = true;
            }
            return total == _size;
        }

        // Distinct hues per region, derived deterministically (no Unity randomness).
        private static Color[] BuildRegionColors(int count)
        {
            var colors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                float h = (float)i / count;
                colors[i] = Color.HSVToRGB(h, 0.45f, 0.92f);
            }
            return colors;
        }
    }
}
