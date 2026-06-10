using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPuzzles
{
    /// <summary>
    /// Lights Out puzzle module. Self-contained: generates its own board, builds its grid
    /// at runtime, and reports the result through callbacks. Tapping a cell toggles it and
    /// its orthogonal neighbors; the board is solved when every light is off.
    /// </summary>
    public class LightsOutPuzzle : MonoBehaviour, IMiniPuzzle
    {
        private static readonly Color OnColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color OffColor = new Color(0.18f, 0.18f, 0.22f);

        private int _size;
        private bool[] _state;
        private Image[] _cellImages;
        private int _moves;
        private bool _finished;
        private PuzzleOverlay _overlay;
        private Action _onSolved;

        /// <inheritdoc />
        public PuzzleType Type => PuzzleType.LightsOut;

        /// <inheritdoc />
        public void Begin(PuzzleContext context, PuzzleOverlay overlay, Action onSolved, Action onFailed)
        {
            _overlay = overlay;
            _onSolved = onSolved;
            _moves = 0;
            _finished = false;

            LightsOutLevelData data = LightsOutGenerator.Generate(context.difficulty, context.seed);
            _size = data.size;
            _state = (bool[])data.initial.Clone();

            BuildGrid();
            Refresh();
            UpdateStatus();
        }

        private void BuildGrid()
        {
            var grid = PuzzleGrid.Create(transform as RectTransform, _size);
            _cellImages = new Image[_size * _size];

            for (int i = 0; i < _size * _size; i++)
            {
                int index = i;
                Image img = PuzzleGrid.CreateCell(grid, out Button button, out _);
                _cellImages[i] = img;
                button.onClick.AddListener(() => OnCellClicked(index));
            }
        }

        private void OnCellClicked(int cell)
        {
            if (_finished) return;
            LightsOutGenerator.Toggle(_state, _size, cell);
            _moves++;
            Refresh();
            UpdateStatus();

            if (IsAllOff())
            {
                _finished = true;
                _overlay.SetFeedback("Solved!", false);
                _onSolved?.Invoke();
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < _state.Length; i++)
                _cellImages[i].color = _state[i] ? OnColor : OffColor;
        }

        private void UpdateStatus() => _overlay.SetStatus($"Moves: {_moves}");

        private bool IsAllOff()
        {
            for (int i = 0; i < _state.Length; i++) if (_state[i]) return false;
            return true;
        }
    }
}
