using UnityEngine;
using UnityEngine.Events;

namespace MiniPuzzles
{
    /// <summary>
    /// Example caller. Holds no puzzle logic and never picks a puzzle type — it only
    /// fires <see cref="MiniPuzzleManager.OpenPuzzle"/> with Inspector-set config and
    /// forwards the result to UnityEvents wired in the Inspector.
    /// </summary>
    public class PuzzleDoor : MonoBehaviour
    {
        [Header("Puzzle request")]
        [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Medium;
        [SerializeField] private string contextLabel = "Hack the lock";
        [Tooltip("0 = random each time; set a fixed value for a repeatable puzzle.")]
        [SerializeField] private int seed = 0;

        [Header("Result events")]
        public UnityEvent onPuzzleSolved;
        public UnityEvent onPuzzleFailed;

        /// <summary>Call this from your interaction system (trigger, button, etc.).</summary>
        public void OnPlayerInteract()
        {
            var config = new PuzzleCallConfig
            {
                difficulty = difficulty,
                seed = seed,
                contextLabel = contextLabel
            };

            MiniPuzzleManager.Instance.OpenPuzzle(config,
                onSuccess: () => onPuzzleSolved.Invoke(),
                onFailure: () => onPuzzleFailed.Invoke());
        }
    }
}
