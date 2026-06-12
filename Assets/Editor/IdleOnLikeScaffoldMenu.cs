using System.IO;
using System;
using IdleOnLike.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IdleOnLike.EditorTools
{
    public static class IdleOnLikeScaffoldMenu
    {
        private const string PlaceholderPath = "Assets/Art/Placeholder";
        private const string CharacterPath = "Assets/ScriptableObjects/Characters";
        private const string ItemPath = "Assets/ScriptableObjects/Items";
        private const string EnemyPath = "Assets/ScriptableObjects/Enemies";
        private const string QuestPath = "Assets/ScriptableObjects/Quests";
        private const string SkillPath = "Assets/ScriptableObjects/Skills";
        private const string ZonePath = "Assets/ScriptableObjects/Zones";
        private const string RecipePath = "Assets/ScriptableObjects/Recipes";
        private const string ScenesPath = "Assets/Scenes";

        [MenuItem("IdleOn Like/Generate Demo Scaffold")]
        public static void GenerateDemoScaffold()
        {
            EnsureFolder(PlaceholderPath);
            EnsureFolder(CharacterPath);
            EnsureFolder(ItemPath);
            EnsureFolder(EnemyPath);
            EnsureFolder(QuestPath);
            EnsureFolder(SkillPath);
            EnsureFolder(ZonePath);
            EnsureFolder(RecipePath);
            EnsureFolder(ScenesPath);

            var heroSprite = CreatePlaceholderSprite("placeholder_hero", new Color32(82, 168, 255, 255));
            var secondHeroSprite = CreatePlaceholderSprite("placeholder_second_hero", new Color32(255, 176, 74, 255));
            var enemySprite = CreatePlaceholderSprite("placeholder_enemy", new Color32(123, 212, 97, 255));
            var slimeSprite = CreatePlaceholderSprite("placeholder_slime", new Color32(77, 205, 221, 255));
            var itemSprite = CreatePlaceholderSprite("placeholder_item", new Color32(235, 215, 87, 255));
            var villageSprite = CreatePlaceholderSprite("placeholder_village", new Color32(98, 173, 132, 255));
            var forestSprite = CreatePlaceholderSprite("placeholder_forest", new Color32(43, 117, 77, 255));
            var expansionSprite = CreatePlaceholderSprite("placeholder_expansion_zone", new Color32(126, 96, 178, 255));

            var wood = CreateAsset<ItemDefinition>($"{ItemPath}/Item_Wood.asset");
            Set(wood, "id", "wood");
            Set(wood, "displayName", "Wood");
            Set(wood, "description", "A basic crafting material gathered from trees.");
            Set(wood, "icon", itemSprite);
            Set(wood, "itemType", ItemType.Material);
            Set(wood, "maxStack", 99);

            var cap = CreateAsset<ItemDefinition>($"{ItemPath}/Item_MushroomCap.asset");
            Set(cap, "id", "mushroom_cap");
            Set(cap, "displayName", "Mushroom Cap");
            Set(cap, "description", "A squishy early-game drop.");
            Set(cap, "icon", itemSprite);
            Set(cap, "itemType", ItemType.Material);
            Set(cap, "maxStack", 99);

            var trainingSword = CreateAsset<ItemDefinition>($"{ItemPath}/Item_TrainingSword.asset");
            Set(trainingSword, "id", "training_sword");
            Set(trainingSword, "displayName", "Training Sword");
            Set(trainingSword, "description", "A starter weapon for the first combat loop.");
            Set(trainingSword, "icon", itemSprite);
            Set(trainingSword, "itemType", ItemType.Equipment);
            Set(trainingSword, "equipmentSlot", EquipmentSlot.Weapon);
            Set(trainingSword, "maxStack", 1);
            Set(trainingSword, "equipStats.attack", 4);

            var chopping = CreateAsset<SkillDefinition>($"{SkillPath}/Skill_Chopping.asset");
            Set(chopping, "id", "chopping");
            Set(chopping, "displayName", "Chopping");
            Set(chopping, "description", "Gather wood while the character keeps progressing.");
            Set(chopping, "icon", itemSprite);
            Set(chopping, "skillType", SkillType.Chopping);

            var mushroom = CreateAsset<EnemyDefinition>($"{EnemyPath}/Enemy_Mushroom.asset");
            Set(mushroom, "id", "mushroom");
            Set(mushroom, "displayName", "Mushroom");
            Set(mushroom, "description", "A soft target for the first auto-combat quest.");
            Set(mushroom, "idleSprite", enemySprite);
            Set(mushroom, "maxHp", 20);
            Set(mushroom, "attackDamage", 2);
            Set(mushroom, "experienceReward", 8);
            Set(mushroom, "minCoins", 1);
            Set(mushroom, "maxCoins", 3);
            SetLootTable(mushroom, (cap, 0.6f, 1, 1));

            var slime = CreateAsset<EnemyDefinition>($"{EnemyPath}/Enemy_Slime.asset");
            Set(slime, "id", "slime");
            Set(slime, "displayName", "Slime");
            Set(slime, "description", "A bouncy early-game enemy with slightly more health than a mushroom.");
            Set(slime, "idleSprite", slimeSprite);
            Set(slime, "hitSprite", slimeSprite);
            Set(slime, "deathSprite", slimeSprite);
            Set(slime, "maxHp", 28);
            Set(slime, "attackDamage", 3);
            Set(slime, "attackInterval", 1.7f);
            Set(slime, "experienceReward", 12);
            Set(slime, "minCoins", 2);
            Set(slime, "maxCoins", 4);
            SetLootTable(slime, (wood, 0.5f, 1, 1), (cap, 0.5f, 1, 1));

            var village = CreateAsset<ZoneDefinition>($"{ZonePath}/Zone_Village.asset");
            Set(village, "id", "village");
            Set(village, "displayName", "Blunder Hills Village");
            Set(village, "description", "The demo hub for quests, crafting, and equipment.");
            Set(village, "sceneName", "Village");
            Set(village, "mapIcon", villageSprite);
            Set(village, "backgroundSprite", villageSprite);

            var forest = CreateAsset<ZoneDefinition>($"{ZonePath}/Zone_Forest.asset");
            Set(forest, "id", "forest");
            Set(forest, "displayName", "Mushroom Forest");
            Set(forest, "description", "The first combat and gathering area.");
            Set(forest, "sceneName", "Forest");
            Set(forest, "mapIcon", forestSprite);
            Set(forest, "backgroundSprite", forestSprite);
            SetZoneEnemies(forest, (mushroom, 3), (slime, 2));

            var expansion = CreateAsset<ZoneDefinition>($"{ZonePath}/Zone_ExpansionPreview.asset");
            Set(expansion, "id", "expansion_preview");
            Set(expansion, "displayName", "Expansion Preview");
            Set(expansion, "description", "Reserved for a third scene if time allows.");
            Set(expansion, "sceneName", "ExpansionPreview");
            Set(expansion, "mapIcon", expansionSprite);
            Set(expansion, "backgroundSprite", expansionSprite);
            Set(forest, "nextZonePreview", expansion);

            var hero = CreateAsset<CharacterDefinition>($"{CharacterPath}/Character_Beginner.asset");
            Set(hero, "id", "beginner_01");
            Set(hero, "displayName", "Beginner");
            Set(hero, "role", CharacterRole.Beginner);
            Set(hero, "description", "The primary demo character.");
            Set(hero, "portrait", heroSprite);
            Set(hero, "idleSprite", heroSprite);
            Set(hero, "baseStats.maxHp", 50);
            Set(hero, "baseStats.attack", 5);
            Set(hero, "baseStats.strength", 1);
            Set(hero, "baseStats.agility", 1);
            Set(hero, "baseStats.wisdom", 1);
            Set(hero, "baseStats.luck", 1);
            Set(hero, "startingZone", village);
            Set(hero, "startingWeapon", trainingSword);

            var secondHero = CreateAsset<CharacterDefinition>($"{CharacterPath}/Character_SecondSlot.asset");
            Set(secondHero, "id", "second_slot");
            Set(secondHero, "displayName", "Second Character Slot");
            Set(secondHero, "role", CharacterRole.Beginner);
            Set(secondHero, "description", "Reserved for a second playable character.");
            Set(secondHero, "portrait", secondHeroSprite);
            Set(secondHero, "idleSprite", secondHeroSprite);
            Set(secondHero, "baseStats.maxHp", 45);
            Set(secondHero, "baseStats.attack", 4);
            Set(secondHero, "startingZone", village);

            var firstQuest = CreateAsset<QuestDefinition>($"{QuestPath}/Quest_FirstSteps.asset");
            Set(firstQuest, "id", "first_steps");
            Set(firstQuest, "title", "First Steps");
            Set(firstQuest, "description", "Defeat a few mushrooms to prove the auto-combat loop.");
            Set(firstQuest, "icon", itemSprite);
            Set(firstQuest, "rewards.coins", 50);
            Set(firstQuest, "rewards.experience", 25);
            SetQuestObjectives(firstQuest, (QuestObjectiveType.KillEnemy, "mushroom", 5, "Defeat Mushrooms"));
            SetRewardItems(firstQuest, (trainingSword, 1));

            var learnToChopQuest = CreateAsset<QuestDefinition>($"{QuestPath}/Quest_LearnToChop.asset");
            Set(learnToChopQuest, "id", "learn_to_chop");
            Set(learnToChopQuest, "title", "Learn to Chop");
            Set(learnToChopQuest, "description", "Gather wood from trees to open the crafting loop.");
            Set(learnToChopQuest, "icon", itemSprite);
            Set(learnToChopQuest, "rewards.coins", 30);
            Set(learnToChopQuest, "rewards.experience", 40);
            Set(learnToChopQuest, "unlockedSkill", chopping);
            SetQuestObjectives(learnToChopQuest, (QuestObjectiveType.GatherResource, "tree", 5, "Chop Trees"));
            SetRewardItems(learnToChopQuest);
            Set(firstQuest, "nextQuest", learnToChopQuest);

            var swordRecipe = CreateAsset<RecipeDefinition>($"{RecipePath}/Recipe_TrainingSword.asset");
            Set(swordRecipe, "id", "training_sword");
            Set(swordRecipe, "displayName", "Training Sword");
            Set(swordRecipe, "description", "Starter craft that demonstrates material conversion into power.");
            Set(swordRecipe, "requiredSkill", chopping);
            Set(swordRecipe, "output.item", trainingSword);
            Set(swordRecipe, "output.quantity", 1);
            Set(swordRecipe, "icon", itemSprite);
            SetRecipeIngredients(swordRecipe, (wood, 5), (cap, 2));

            var catalog = CreateAsset<GameCatalog>("Assets/ScriptableObjects/GameCatalog.asset");
            Set(catalog, "defaultCharacter", hero);
            Set(catalog, "villageZone", village);
            Set(catalog, "forestZone", forest);
            Set(catalog, "expansionZone", expansion);
            AddToList(catalog, "playableCharacters", hero, secondHero);
            AddToList(catalog, "items", wood, cap, trainingSword);
            AddToList(catalog, "enemies", mushroom, slime);
            AddToList(catalog, "quests", firstQuest, learnToChopQuest);
            AddToList(catalog, "skills", chopping);
            AddToList(catalog, "zones", village, forest, expansion);
            AddToList(catalog, "recipes", swordRecipe);

            CreateScene("Boot", "Boot Scene", new Color(0.12f, 0.13f, 0.16f));
            CreateScene("CharacterSelect", "Character Select", new Color(0.10f, 0.12f, 0.18f));
            CreateScene("Village", "Village", new Color(0.25f, 0.43f, 0.33f));
            CreateScene("Forest", "Forest", new Color(0.10f, 0.28f, 0.18f));
            CreateScene("ExpansionPreview", "Expansion Preview", new Color(0.25f, 0.20f, 0.35f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("IdleOn-like demo scaffold generated. Replace placeholder art by assigning assets in the generated ScriptableObjects and Prefabs.");
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Sprite CreatePlaceholderSprite(string fileName, Color32 color)
        {
            var path = $"{PlaceholderPath}/{fileName}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color32[32 * 32];
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateScene(string sceneName, string label, Color cameraColor)
        {
            var scenePath = $"{ScenesPath}/{sceneName}.unity";
            if (File.Exists(scenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraColor;
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            var marker = new GameObject(label);
            marker.transform.position = Vector3.zero;

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void Set(UnityEngine.Object target, string propertyPath, object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogWarning($"Property '{propertyPath}' not found on {target.name}.");
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    property.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = (float)value;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = (string)value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value as UnityEngine.Object;
                    break;
                default:
                    Debug.LogWarning($"Property '{propertyPath}' uses unsupported scaffold type {property.propertyType}.");
                    break;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void AddToList(UnityEngine.Object target, string propertyPath, params UnityEngine.Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"List property '{propertyPath}' not found on {target.name}.");
                return;
            }

            property.ClearArray();
            for (var i = 0; i < values.Length; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetZoneEnemies(ZoneDefinition zone, params (EnemyDefinition enemy, int weight)[] spawns)
        {
            var serializedObject = new SerializedObject(zone);
            var property = serializedObject.FindProperty("enemies");
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"List property 'enemies' not found on {zone.name}.");
                return;
            }

            property.ClearArray();
            for (var i = 0; i < spawns.Length; i++)
            {
                property.InsertArrayElementAtIndex(i);
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("enemy").objectReferenceValue = spawns[i].enemy;
                element.FindPropertyRelative("weight").intValue = Mathf.Max(1, spawns[i].weight);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
        }

        private static void SetLootTable(EnemyDefinition enemy, params (ItemDefinition item, float chance, int minQuantity, int maxQuantity)[] drops)
        {
            var serializedObject = new SerializedObject(enemy);
            var property = serializedObject.FindProperty("lootTable");
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"List property 'lootTable' not found on {enemy.name}.");
                return;
            }

            property.ClearArray();
            for (var i = 0; i < drops.Length; i++)
            {
                property.InsertArrayElementAtIndex(i);
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = drops[i].item;
                element.FindPropertyRelative("dropChance").floatValue = Mathf.Clamp01(drops[i].chance);
                element.FindPropertyRelative("minQuantity").intValue = Mathf.Max(1, drops[i].minQuantity);
                element.FindPropertyRelative("maxQuantity").intValue = Mathf.Max(drops[i].minQuantity, drops[i].maxQuantity);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        private static void SetQuestObjectives(QuestDefinition quest, params (QuestObjectiveType type, string targetId, int requiredAmount, string displayText)[] objectives)
        {
            var serializedObject = new SerializedObject(quest);
            var property = serializedObject.FindProperty("objectives");
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"List property 'objectives' not found on {quest.name}.");
                return;
            }

            property.ClearArray();
            for (var i = 0; i < objectives.Length; i++)
            {
                property.InsertArrayElementAtIndex(i);
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("objectiveType").enumValueIndex = (int)objectives[i].type;
                element.FindPropertyRelative("targetId").stringValue = objectives[i].targetId;
                element.FindPropertyRelative("requiredAmount").intValue = Mathf.Max(1, objectives[i].requiredAmount);
                element.FindPropertyRelative("displayText").stringValue = objectives[i].displayText;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(quest);
        }

        private static void SetRewardItems(QuestDefinition quest, params (ItemDefinition item, int quantity)[] items)
        {
            var serializedObject = new SerializedObject(quest);
            var property = serializedObject.FindProperty("rewards.items");
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"List property 'rewards.items' not found on {quest.name}.");
                return;
            }

            property.ClearArray();
            for (var i = 0; i < items.Length; i++)
            {
                property.InsertArrayElementAtIndex(i);
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = items[i].item;
                element.FindPropertyRelative("quantity").intValue = Mathf.Max(1, items[i].quantity);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(quest);
        }

        private static void SetRecipeIngredients(RecipeDefinition recipe, params (ItemDefinition item, int quantity)[] items)
        {
            var serializedObject = new SerializedObject(recipe);
            var property = serializedObject.FindProperty("ingredients");
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"List property 'ingredients' not found on {recipe.name}.");
                return;
            }

            property.ClearArray();
            for (var i = 0; i < items.Length; i++)
            {
                property.InsertArrayElementAtIndex(i);
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = items[i].item;
                element.FindPropertyRelative("quantity").intValue = Mathf.Max(1, items[i].quantity);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
        }
    }
}
