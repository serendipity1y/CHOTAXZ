using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniPuzzles
{
    /// <summary>
    /// Tango puzzle module. Self-contained. Player taps empty cells to cycle
    /// sun -> moon -> empty. Fixed clue cells cannot be changed. Auto-wins when the grid
    /// is full and satisfies all constraints (no 3 consecutive same symbol in any
    /// row/column, equal sun/moon count per row and column).
    /// </summary>
    public class TangoPuzzle : MonoBehaviour, IMiniPuzzle
    {
        private const int Empty = -1;
        private const int Sun = 0;
        private const int Moon = 1;

        private static readonly Color CellColor = new Color(0.93f, 0.90f, 0.82f);
        private static readonly Color ClueColor = new Color(0.78f, 0.74f, 0.62f);

        private int _size;
        private int[] _grid;
        private bool[] _isClue;
        private TextMeshProUGUI[] _labels;
        private bool _finished;
        private Action _onSolved;

        /// <inheritdoc />
        public PuzzleType Type => PuzzleType.Tango;

        /// <inheritdoc />
        public void Begin(PuzzleContext context, PuzzleOverlay overlay, Action onSolved, Action onFailed)
        {
            _onSolved = onSolved;
            _finished = false;

            TangoLevelData data = TangoGenerator.Generate(context.difficulty, context.seed);
            _size = data.size;
            _grid = (int[])data.givens.Clone();
            _isClue = new bool[_size * _size];
            for (int i = 0; i < _isClue.Length; i++) _isClue[i] = data.givens[i] != Empty;

            BuildGrid();
            RefreshAll();
            overlay.SetStatus("Fill the grid");
        }

        private void BuildGrid()
        {
            var grid = PuzzleGrid.Create(transform as RectTransform, _size);
            _labels = new TextMeshProUGUI[_size * _size];

            for (int i = 0; i < _size * _size; i++)
            {
                int index = i;
                Image img = PuzzleGrid.CreateCell(grid, out Button button, out TextMeshProUGUI label);
                img.color = _isClue[i] ? ClueColor : CellColor;
                _labels[i] = label;
                if (!_isClue[i])
                    button.onClick.AddListener(() => OnCellClicked(index));
                else
                    button.interactable = false;
            }
        }

        private void OnCellClicked(int cell)
        {
            if (_finished || _isClue[cell]) return;
            _grid[cell] = _grid[cell] switch
            {
                Empty => Sun,
                Sun => Moon,
                _ => Empty
            };
            RefreshCell(cell);

            if (IsComplete() && IsValid())
            {
                _finished = true;
                _onSolved?.Invoke();
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < _grid.Length; i++) RefreshCell(i);
        }

        private void RefreshCell(int i)
        {
            _labels[i].text = _grid[i] switch { Sun => "S", Moon => "M", _ => string.Empty };
        }

        private bool IsComplete()
        {
            for (int i = 0; i < _grid.Length; i++) if (_grid[i] == Empty) return false;
            return true;
        }

        // Validates the full grid against both Tango constraints.
        private bool IsValid()
        {
            int half = _size / 2;

            for (int r = 0; r < _size; r++)
            {
                int sun = 0;
                for (int c = 0; c < _size; c++)
                {
                    int v = _grid[r * _size + c];
                    if (v == Sun) sun++;
                    if (c >= 2 && v == _grid[r * _size + c - 1] && v == _grid[r * _size + c - 2]) return false;
                }
                if (sun != half) return false;
            }

            for (int c = 0; c < _size; c++)
            {
                int sun = 0;
                for (int r = 0; r < _size; r++)
                {
                    int v = _grid[r * _size + c];
                    if (v == Sun) sun++;
                    if (r >= 2 && v == _grid[(r - 1) * _size + c] && v == _grid[(r - 2) * _size + c]) return false;
                }
                if (sun != half) return false;
            }

            return true;
        }
    }
}
