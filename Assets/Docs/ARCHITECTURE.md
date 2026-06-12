# IdleOn-Like Demo Architecture

This project is structured so gameplay data and art references live in Unity assets, not hard-coded paths.

## Data Assets

- `CharacterDefinition`: playable character slots. The catalog is intentionally list-based so a second character can be added without code changes.
- `ItemDefinition`: materials, consumables, equipment, icons, world sprites, drops, and equipment stats.
- `EnemyDefinition`: enemy combat values, reward tables, sprites, animator controller, and prefab slot.
- `QuestDefinition`: objectives, rewards, next quest, and unlock references.
- `SkillDefinition`: combat and gathering skills with configurable XP curves.
- `ZoneDefinition`: scene name, background, enemies, resource nodes, unlock requirements, and optional next-zone preview.
- `RecipeDefinition`: crafting inputs and outputs.
- `GameCatalog`: one top-level index for demo flow, playable characters, zones, and all data tables.

## Art Workflow

All visual assets should be assigned through Inspector fields on ScriptableObjects, Prefabs, or scene objects.

Do not load art by string path from code. Use placeholder sprites during implementation, then replace the assigned fields with sourced art.

## Planned Scenes

- `Boot`: load save data and catalog, then route to character select or the current zone.
- `CharacterSelect`: pick the first character now, with UI space reserved for a second playable character.
- `Village`: quest turn-ins, crafting, equipment, and demo onboarding.
- `Forest`: early combat, chopping, drops, leveling, and first crafting materials.
- Optional third zone: referenced through `GameCatalog.ExpansionZone` and `ZoneDefinition.NextZonePreview`.

## Suggested Next Modules

1. Save data and runtime `GameState`.
2. Character select reading `GameCatalog.PlayableCharacters`.
3. Automatic combat using `ZoneDefinition.Enemies`.
4. Inventory, equipment, and stat calculation.
5. Quest progression and unlocks.
6. Gathering, crafting, and offline gains.
