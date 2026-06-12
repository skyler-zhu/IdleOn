# IdleOn-Like Unity 2D Demo

This is the starting architecture for a small, client-only Unity 2D demo inspired by early-game IdleOn.

The current scaffold focuses on clean data ownership and art replacement workflow:

- Gameplay data is represented by ScriptableObjects.
- Art is assigned through Inspector fields, Prefabs, or scene references.
- No runtime system should load art by hard-coded string path.
- The character roster is list-based and already includes room for a second playable character.
- Zones are data-driven and include a third-zone expansion slot.

## Unity Setup

Open the project in Unity `2022.3.62f1c1`.

Then run:

```text
IdleOn Like > Generate Demo Scaffold
```

This creates:

- Placeholder sprites in `Assets/Art/Placeholder`
- Starter ScriptableObjects in `Assets/ScriptableObjects`
- Scenes for `Boot`, `CharacterSelect`, `Village`, `Forest`, and `ExpansionPreview`
- A top-level `GameCatalog` asset

The generator is safe to rerun; it reuses existing assets where possible.

## Play Test

Open:

```text
Assets/Scenes/Boot.unity
```

Press Play.

Expected first-run flow:

```text
Boot
→ CharacterSelect
→ choose a character
→ save file is created
→ Village
→ top HUD shows character, level, coins, and current zone
→ accept First Steps
→ Go Forest
→ automatic combat starts
→ quest tracker updates after Mushroom kills
→ gain XP and coins
→ open Inventory to see drops
→ equip any equipment item in inventory
→ Return Village
→ complete First Steps
→ Learn to Chop becomes active
→ Go Forest
→ switch to Chopping
→ gather Wood
→ use Sim 1h to preview offline gains
→ Return Village
→ craft Training Sword
```

The save file is stored as JSON in `Application.persistentDataPath` under `idleon_like_save.json`.
Use the `New Save` button in the Village HUD to delete the current save and return to character selection.

## Project Layout

```text
Assets/
  Art/
  Animations/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Data/
    Combat/
    Inventory/
    Equipment/
    Progression/
    Quests/
    Skills/
    Crafting/
    UI/
    Save/
  ScriptableObjects/
  Docs/
```

## Next Implementation Steps

1. Implement save data and runtime `GameState`.
2. Build character select from `GameCatalog.PlayableCharacters`.
3. Add automatic combat driven by `ZoneDefinition.Enemies`.
4. Add inventory, equipment, and stat calculation.
5. Add quest progression with kill, collect, gather, craft, and level objectives.
6. Add chopping, crafting, and offline gains.
7. Polish the HUD and replace placeholder art with approved free 2D assets.

## Art Replacement Workflow

Import art into the matching `Assets/Art` subfolder, then assign it in:

- `CharacterDefinition`
- `EnemyDefinition`
- `ItemDefinition`
- `SkillDefinition`
- `ZoneDefinition`
- Prefabs and scene objects

Record every non-placeholder asset in `Assets/Docs/ART_CREDITS.md`.
