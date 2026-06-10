using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPuzzles
{
    /// <summary>
    /// Tango puzzle. Player taps empty cells to cycle empty -> Yin -> Yang.
    /// Fixed clue cells cannot be changed. Auto-wins when the grid is full and valid.
    /// </summary>
    public class TangoPuzzle : MonoBehaviour, IMiniPuzzle
    {
        private const int Empty = -1;
        private const int Yin = 0;
        private const int Yang = 1;

        private static readonly Color CellColor = new Color(0.93f, 0.90f, 0.82f);
        private static readonly Color ClueColor = new Color(0.78f, 0.74f, 0.62f);
        private static readonly Color ViolationColor = new Color(0.95f, 0.45f, 0.45f);

        [SerializeField] private Sprite yinSprite;
        [SerializeField] private Sprite yangSprite;

        private int _size;
        private int[] _grid;
        private bool[] _isClue;
        private Image[] _cellImages;
        private Image[] _icons;
        private bool _finished;
        private PuzzleOverlay _overlay;
        private Action _onSolved;
        private Coroutine _violationRoutine;
        private readonly HashSet<int> _violationCells = new();
        private readonly HashSet<int> _flashedCells = new();

        public PuzzleType Type => PuzzleType.Tango;

        public void Begin(PuzzleContext context, PuzzleOverlay overlay, Action onSolved, Action onFailed)
        {
            _overlay = overlay;
            _onSolved = onSolved;
            _finished = false;

            TangoLevelData data = TangoGenerator.Generate(context.difficulty, context.seed);
            _size = data.size;
            _grid = (int[])data.givens.Clone();
            _isClue = new bool[_size * _size];
            for (int i = 0; i < _isClue.Length; i++) _isClue[i] = data.givens[i] != Empty;

            BuildGrid();
            RefreshAll();
        }

        private void OnDestroy()
        {
            ClearViolationFlash();
        }

        private void BuildGrid()
        {
            var grid = PuzzleGrid.Create(transform as RectTransform, _size);
            int cells = _size * _size;
            _cellImages = new Image[cells];
            _icons = new Image[cells];

            for (int i = 0; i < cells; i++)
            {
                int index = i;
                Image img = PuzzleGrid.CreateIconCell(grid, out Button button, out Image icon);
                img.color = _isClue[i] ? ClueColor : CellColor;
                _cellImages[i] = img;
                _icons[i] = icon;
                if (!_isClue[i])
                    button.onClick.AddListener(() => OnCellClicked(index));
                else
                    button.interactable = false;
            }
        }

        private void OnCellClicked(int cell)
        {
            if (_finished || _isClue[cell]) return;

            ClearViolationFlash();

            _grid[cell] = _grid[cell] switch
            {
                Empty => Yin,
                Yin => Yang,
                _ => Empty
            };
            RefreshCell(cell);
            CheckViolations(cell);

            if (IsComplete())
            {
                if (IsValid())
                {
                    _finished = true;
                    _overlay.SetFeedback("Solved!", false);
                    _onSolved?.Invoke();
                }
                else
                {
                    _overlay.SetFeedback("Almost — check row/column balance", true);
                }
            }
        }

        private void CheckViolations(int cell)
        {
            int r = cell / _size, c = cell % _size;
            _violationCells.Clear();

            CheckLineViolations(r, true);
            CheckLineViolations(c, false);
            CheckCountViolations(r, true);
            CheckCountViolations(c, false);

            if (_violationCells.Count > 0)
                _violationRoutine = StartCoroutine(FlashViolations());
        }

        private void CheckLineViolations(int line, bool isRow)
        {
            for (int i = 0; i < _size - 2; i++)
            {
                int a = isRow ? line * _size + i : i * _size + line;
                int b = isRow ? line * _size + i + 1 : (i + 1) * _size + line;
                int d = isRow ? line * _size + i + 2 : (i + 2) * _size + line;
                if (_grid[a] != Empty && _grid[a] == _grid[b] && _grid[b] == _grid[d])
                {
                    _violationCells.Add(a);
                    _violationCells.Add(b);
                    _violationCells.Add(d);
                    _overlay.SetFeedback("No three Yin or Yang in a row", true);
                }
            }
        }

        private void CheckCountViolations(int line, bool isRow)
        {
            int half = _size / 2;
            int yin = 0, yang = 0;
            for (int i = 0; i < _size; i++)
            {
                int idx = isRow ? line * _size + i : i * _size + line;
                if (_grid[idx] == Yin) yin++;
                else if (_grid[idx] == Yang) yang++;
            }

            if (yin > half)
            {
                for (int i = 0; i < _size; i++)
                {
                    int idx = isRow ? line * _size + i : i * _size + line;
                    if (_grid[idx] == Yin) _violationCells.Add(idx);
                }
                _overlay.SetFeedback("Too many Yin in this row/column", true);
            }
            else if (yang > half)
            {
                for (int i = 0; i < _size; i++)
                {
                    int idx = isRow ? line * _size + i : i * _size + line;
                    if (_grid[idx] == Yang) _violationCells.Add(idx);
                }
                _overlay.SetFeedback("Too many Yang in this row/column", true);
            }
        }

        private IEnumerator FlashViolations()
        {
            _flashedCells.Clear();
            foreach (int i in _violationCells)
            {
                _flashedCells.Add(i);
                _cellImages[i].color = ViolationColor;
            }

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            RestoreFlashedCells();
            _violationRoutine = null;
        }

        private void ClearViolationFlash()
        {
            if (_violationRoutine != null)
            {
                StopCoroutine(_violationRoutine);
                _violationRoutine = null;
            }
            RestoreFlashedCells();
        }

        private void RestoreFlashedCells()
        {
            if (_cellImages == null)
            {
                _flashedCells.Clear();
                return;
            }
            foreach (int i in _flashedCells)
                _cellImages[i].color = _isClue[i] ? ClueColor : CellColor;
            _flashedCells.Clear();
        }

        private void RefreshAll()
        {
            for (int i = 0; i < _grid.Length; i++) RefreshCell(i);
        }

        private void RefreshCell(int i)
        {
            Image icon = _icons[i];
            switch (_grid[i])
            {
                case Yin:
                    icon.sprite = yinSprite;
                    icon.enabled = yinSprite != null;
                    break;
                case Yang:
                    icon.sprite = yangSprite;
                    icon.enabled = yangSprite != null;
                    break;
                default:
                    icon.enabled = false;
                    break;
            }
        }

        private bool IsComplete()
        {
            for (int i = 0; i < _grid.Length; i++) if (_grid[i] == Empty) return false;
            return true;
        }

        private bool IsValid()
        {
            int half = _size / 2;

            for (int r = 0; r < _size; r++)
            {
                int yin = 0;
                for (int c = 0; c < _size; c++)
                {
                    int v = _grid[r * _size + c];
                    if (v == Yin) yin++;
                    if (c >= 2 && v == _grid[r * _size + c - 1] && v == _grid[r * _size + c - 2]) return false;
                }
                if (yin != half) return false;
            }

            for (int c = 0; c < _size; c++)
            {
                int yin = 0;
                for (int r = 0; r < _size; r++)
                {
                    int v = _grid[r * _size + c];
                    if (v == Yin) yin++;
                    if (r >= 2 && v == _grid[(r - 1) * _size + c] && v == _grid[(r - 2) * _size + c]) return false;
                }
                if (yin != half) return false;
            }

            return true;
        }
    }
}
