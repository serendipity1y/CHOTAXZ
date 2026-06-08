using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Player;

namespace MiniPuzzles
{
    /// <summary>
    /// Persistent singleton entry point for the mini-puzzle system.
    /// Reads player state, resolves the puzzle type, spawns the matching puzzle prefab,
    /// and routes the result back to the caller and the event bus. Depends only on the
    /// <see cref="IPlayerStateProvider"/> and <see cref="IGameEventBus"/> interfaces and
    /// on prefabs keyed by <see cref="PuzzleType"/> — never on concrete puzzle classes.
    /// </summary>
    public class MiniPuzzleManager : MonoBehaviour
    {
        /// <summary>Maps a puzzle type to a prefab whose root implements <see cref="IMiniPuzzle"/>.</summary>
        [Serializable]
        public struct PuzzlePrefabEntry
        {
            public PuzzleType type;
            public GameObject prefab;
        }

        [Header("Dependencies (interfaces via MonoBehaviour adapters)")]
        [SerializeField] private MonoBehaviour playerStateProvider; // IPlayerStateProvider
        [SerializeField] private MonoBehaviour gameEventBus;        // IGameEventBus

        [Header("UI")]
        [SerializeField] private PuzzleOverlay overlay;

        [Header("Puzzle prefabs (one per type)")]
        [SerializeField] private List<PuzzlePrefabEntry> puzzlePrefabs = new();

        /// <summary>Global access point.</summary>
        public static MiniPuzzleManager Instance { get; private set; }

        private IPlayerStateProvider _stateProvider;
        private IGameEventBus _eventBus;
        private readonly Dictionary<PuzzleType, GameObject> _prefabLookup = new();

        private bool _busy;
        private GameObject _activeInstance;
        private float _startTime;
        private PuzzleType _activeType;
        private bool _resolved;
        private UnityAction _callerSuccess;
        private UnityAction _callerFailure;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _stateProvider = playerStateProvider as IPlayerStateProvider;
            _eventBus = gameEventBus as IGameEventBus;

            if (_stateProvider == null)
                Debug.LogError("[MiniPuzzleManager] playerStateProvider does not implement IPlayerStateProvider.");
            if (_eventBus == null)
                Debug.LogWarning("[MiniPuzzleManager] gameEventBus does not implement IGameEventBus; events will be dropped.");

            foreach (var entry in puzzlePrefabs)
            {
                if (entry.prefab != null) _prefabLookup[entry.type] = entry.prefab;
            }
        }

        /// <summary>
        /// Single public entry point. Opens the puzzle that matches the player's current
        /// state. Ignored if a puzzle is already open.
        /// </summary>
        /// <param name="config">Caller config (difficulty, seed, label).</param>
        /// <param name="onSuccess">Invoked when the player solves the puzzle.</param>
        /// <param name="onFailure">Invoked when the player gives up or fails.</param>
        public void OpenPuzzle(PuzzleCallConfig config, UnityAction onSuccess, UnityAction onFailure)
        {
            if (_busy)
            {
                Debug.LogWarning("[MiniPuzzleManager] A puzzle is already open; ignoring OpenPuzzle.");
                return;
            }
            if (_stateProvider == null)
            {
                Debug.LogError("[MiniPuzzleManager] No player state provider; cannot open puzzle.");
                onFailure?.Invoke();
                return;
            }

            int seed = config.seed != 0 ? config.seed : Environment.TickCount & 0x7fffffff;
            if (seed == 0) seed = 1;

            PlayerState state = _stateProvider.CurrentState;
            PuzzleType type = PuzzleTypeResolver.Resolve(state, seed);

            if (!_prefabLookup.TryGetValue(type, out GameObject prefab) || prefab == null)
            {
                Debug.LogError($"[MiniPuzzleManager] No prefab assigned for puzzle type {type}.");
                onFailure?.Invoke();
                return;
            }

            var context = new PuzzleContext
            {
                difficulty = config.difficulty,
                seed = seed,
                contextLabel = config.contextLabel,
                playerState = state
            };

            _busy = true;
            _resolved = false;
            _activeType = type;
            _callerSuccess = onSuccess;
            _callerFailure = onFailure;
            _startTime = Time.unscaledTime;

            overlay.ClearContent();
            // Instantiate first, then re-parent. Instantiating directly into a persistent
            // (DontDestroyOnLoad) parent warns and drops the parent; SetParent does not.
            _activeInstance = Instantiate(prefab);
            _activeInstance.transform.SetParent(overlay.ContentRoot, worldPositionStays: false);
            var puzzle = _activeInstance.GetComponentInChildren<IMiniPuzzle>(true);
            if (puzzle == null)
            {
                Debug.LogError($"[MiniPuzzleManager] Prefab for {type} has no IMiniPuzzle component.");
                Cleanup();
                _busy = false;
                onFailure?.Invoke();
                return;
            }

            overlay.Show(type, context, onGiveUp: () => Finish(false));
            _eventBus?.Publish(new PuzzleOpenedEvent { Type = type, State = state, Seed = seed });

            puzzle.Begin(context, overlay, onSolved: () => Finish(true), onFailed: () => Finish(false));
        }

        private void Finish(bool success)
        {
            if (_resolved) return; // guard against double callbacks
            _resolved = true;

            float elapsed = Time.unscaledTime - _startTime;
            _eventBus?.Publish(new PuzzleCompletedEvent
            {
                Type = _activeType,
                Success = success,
                TimeSeconds = elapsed
            });

            UnityAction callerCallback = success ? _callerSuccess : _callerFailure;

            overlay.Hide();
            Cleanup();
            _busy = false;

            callerCallback?.Invoke();
        }

        private void Cleanup()
        {
            if (_activeInstance != null)
            {
                Destroy(_activeInstance);
                _activeInstance = null;
            }
            _callerSuccess = null;
            _callerFailure = null;
        }
    }
}
