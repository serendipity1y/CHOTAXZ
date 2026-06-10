MINI-PUZZLE SYSTEM — PREFAB & SCENE SETUP
=========================================

Cells and grids are built entirely in code (PuzzleGrid.cs), so you do NOT need a
per-cell or per-size prefab. You only build a few container prefabs/objects.

----------------------------------------------------------------------
1. PUZZLE OVERLAY (one Canvas overlay)
----------------------------------------------------------------------
Create in a scene, then save as a prefab "PuzzleOverlay":

  Canvas  (Render Mode: Screen Space - Overlay)
    - Canvas Scaler: Scale With Screen Size (1920x1080 reference)
    - Graphic Raycaster (required for input)
    + CanvasGroup            <- on the Canvas (for fade/block-input)
    └─ Panel (RectTransform, Image = dim background)   <- this is "panel"
         ├─ Header (horizontal layout)
         │    ├─ Title        (Text)   -> PuzzleOverlay.titleText
         │    ├─ Context      (Text)   -> PuzzleOverlay.contextText
         │    ├─ Difficulty   (Text)   -> PuzzleOverlay.difficultyBadge
         │    └─ State        (Text)   -> PuzzleOverlay.stateBadge   (Yin/Yang)
         ├─ Status            (Text)   -> PuzzleOverlay.statusText   (timer/moves)
         ├─ ContentRoot       (RectTransform, empty)  -> PuzzleOverlay.contentRoot
         └─ GiveUpButton      (Button) -> PuzzleOverlay.giveUpButton

  Add component PuzzleOverlay to the Canvas root and drag the references above
  into its fields:
    canvasGroup, panel, contentRoot, titleText, contextText, difficultyBadge,
    stateBadge, statusText, giveUpButton.

  Make sure ONE EventSystem exists in the scene (GameObject > UI > Event System).
  Required for button clicks and ZIP drag tracing.

----------------------------------------------------------------------
2. PUZZLE PREFABS (one per type — just an empty RectTransform + script)
----------------------------------------------------------------------
For each of the four types create a prefab whose ROOT has:
  - RectTransform (stretch to parent: anchors 0,0 - 1,1, offsets 0)
  - the matching puzzle component:
        ZipPuzzle        -> prefab "Puzzle_ZIP"
        TangoPuzzle      -> prefab "Puzzle_Tango"
        QueensPuzzle     -> prefab "Puzzle_Queens"
        LightsOutPuzzle  -> prefab "Puzzle_LightsOut"
  No children needed — the grid is generated at runtime under the root.

----------------------------------------------------------------------
3. MINI PUZZLE MANAGER (persistent singleton)
----------------------------------------------------------------------
Create an empty GameObject "MiniPuzzleManager" (place in your bootstrap scene):
  + MiniPuzzleManager
  + PlayerStateAdapter      (implements IPlayerStateProvider)
  + GameEventBusAdapter     (implements IGameEventBus)

  Wire MiniPuzzleManager fields:
    playerStateProvider = the PlayerStateAdapter component
    gameEventBus        = the GameEventBusAdapter component
    overlay             = the PuzzleOverlay (instance in scene or instantiated prefab)
    puzzlePrefabs       = 4 entries:
        ZIP        -> Puzzle_ZIP
        Tango      -> Puzzle_Tango
        Queens     -> Puzzle_Queens
        LightsOut  -> Puzzle_LightsOut

  The manager survives scene loads (DontDestroyOnLoad). Keep the overlay either as
  a child of the manager or also persistent so it is always available.

----------------------------------------------------------------------
4. DOOR / CALLER
----------------------------------------------------------------------
On any door/interactable add PuzzleDoor, set difficulty/contextLabel/seed,
and call OnPlayerInteract() from your interaction system. Wire onPuzzleSolved /
onPuzzleFailed UnityEvents in the Inspector.

----------------------------------------------------------------------
5. PLAYER STATE + TEST MATRIX
----------------------------------------------------------------------
Yin state  -> ZIP (seed even) or Tango (seed odd)
Yang state -> Queens (seed even) or Lights Out (seed odd)

Per level scene:
  - MiniPuzzleManager + PlayerStateAdapter + GameEventBusAdapter
  - PlayerStateAdapter.playerStateSystem -> scene PlayerStateSystem
  - PuzzleOverlay with help/feedback (auto-created if unwired)
  - Puzzle_Tango prefab: Yin.png + Yang.png on TangoPuzzle component

Console log on open: [MiniPuzzleManager] Opening <type> for <state> ...
