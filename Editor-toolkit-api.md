# HS Stride Editor Toolkit - Complete API Reference

A library for creating custom editor tools for Stride. Batch task automation for scenes. Create UI and prefabs via code. Edit assets programmatically. Build CLI or GUI tools for repetitive editor work.

**Version:** 1.8.0
**Target Framework:** .NET 8.0
**License:** Apache 2.0

---

## Project Validation Modes

The toolkit can be run in two modes when working with custom components:

- **Strict mode (default)**: Throws an exception if you try to set a property that doesn't exist in the corresponding C# script. This helps catch typos and ensures you're only setting properties that Stride will recognize.
- **Loose mode**: Allows setting any property on a component, even if it's not defined in the C# script. This offers more flexibility for dynamic scenarios but is less safe, as invalid properties will cause an error when launching stride. A good scenario for this is if you're updating the scripts and the toolkit doesn't know about the changes yet. 

You can set the mode when creating a `StrideProject` instance.

```csharp
// Strict mode (default) - helps catch typos
var project = new StrideProject(@"C:\MyGame"); 
// Throws an exception on: component.Set("UnkownProperty", 123);

// Loose mode - permissive, for advanced use cases
var project = new StrideProject(@"C:\MyGame", ProjectMode.Loose);
// Allows: component.Set("UnkownProperty", 123);
```

---

## 🚨 BACKUP YOUR PROJECT FIRST!

**Before using this toolkit:**

- ✅ Commit to version control (Git)
- ✅ Create a backup copy of your project folder
- ✅ Test on a copy first
- ✅ Changes are permanent when you call `.Save()`

**This software is provided "AS IS" without warranty. We are NOT responsible for data loss or project corruption.**

---

## 🔄 How This Works (Batch Workflow)

**This is NOT live editor scripting.** You're not connected to a running Stride editor or working with in-memory scenes.

**This is file-based batch automation.** The workflow:

1. **Plan** - Decide what repetitive task to automate (add colliders, swap materials, etc.)
2. **Close Stride** - Avoid file conflicts (or work on a project copy)
3. **Run Your Tool** - Execute your C# script/app - it edits .sdscene/.sdmat files directly
4. **Open Stride** - Review the changes, continue normal work

**Key differences from Unity:**

- Close editor before running scripts (no file conflicts)
- Work on project copies for safe testing
- Perfect for CI/CD pipelines
- Build cross-platform tools

---

## ⚠️ CRITICAL: Component Property Limitations

**IMPORTANT:** Only properties that Stride serializes (visible in Stride's Property Grid) can be accessed with `Get<T>()` and `Set()`.

Internal script variables that aren't serialized by Stride won't be available. If you try to access a property and get `null` or default values, check if it's visible in the Stride Editor's Property Grid first.

---

## 🔄 GameStudio Workflow Note

****Close and reopen GameStudio after running scripts** - Changes won't show until you restart. Yes, it's annoying, but saving hours of manual work is worth it.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Core Concepts](#core-concepts)
3. [StrideProject API (Recommended)](#strideproject-api-recommended)
4. [Scene Manipulation API](#scene-manipulation-api)
5. [Prefab Creation](#prefab-creation-programmatically)
6. [UI Page Creation](#ui-page-creation-programmatically)
7. [Particle System Creation (Programmatically)](#particle-system-creation-programmatically)
8. [Entity & Component System](#entity--component-system)
9. [Working with Custom Components](#working-with-custom-components-critical)
10. [Typed Component Wrappers](#typed-component-wrappers)
11. [Asset Scanning & References](#asset-scanning--references)
12. [Direct Asset Editing](#direct-asset-editing)
13. [Data Types](#data-types)
14. [Error Handling](#error-handling)
15. [Advanced Workflows & Best Practices](#advanced-workflows--best-practices)

---

## Getting Started

### Installation

```bash
dotnet add package HS.Stride.Editor.Toolkit
```

### Basic Usage (Recommended - StrideProject)

```csharp
using HS.Stride.Editor.Toolkit.Core;

// Create project instance (auto-scans all assets)
var project = new StrideProject(@"C:\MyGame");

// Load scene by name (no full path needed!)
var scene = project.LoadScene("Level1");

// Find an entity
var player = scene.FindEntityByName("Player");

// Modify transform
var transform = player.GetTransform();
transform.SetPosition(10, 0, 5);

// Save changes
scene.Save();

// Find and use assets easily
var enemyPrefab = project.FindAsset("Enemy", AssetType.Prefab);
scene.InstantiatePrefab(enemyPrefab, new Vector3Data(10, 0, 5));
```

### Quick Example: Batch Add Colliders

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");

// Find all entities with models but no colliders
var targets = scene.FindEntities(e =>
    e.HasComponent("ModelComponent") &&
    !e.HasComponent("StaticColliderComponent"));

// Add box colliders to all
foreach (var entity in targets)
{
    var collider = entity.AddStaticCollider();
    collider.AddBoxShape(1.0f, 1.0f, 1.0f);
}

scene.Save();
```

---

## Core Concepts

The toolkit is built on a **3-Pillar Architecture**:

### Pillar 1: Scene Manipulation

Load `.sdscene` files, find/create/modify entities and components, save changes.

**Primary Classes:**

- `Scene` - Scene operations
- `Entity` - Represents a game object in the scene
- `Component` - Represents a component attached to an entity

### Pillar 2: Asset Referencing

Scan Stride projects to discover and reference assets (prefabs, models, materials, etc.).

**Primary Classes:**

`StrideProject` -  auto-scans and allows you to load project assets by name only.

### Pillar 3: Direct Asset Editing

Load and modify asset files directly (materials, textures, animations, prefabs, etc.).

**Primary Classes:**

- `MaterialAsset`, `TextureAsset`, `AnimationAsset`, `PrefabAsset`, etc.
- All loadable via `StrideProject.LoadXxx()` methods or direct `XxxAsset.Load()` calls

---

## StrideProject API (Recommended)

### Overview

The `StrideProject` class provides a **unified, simplified API** for working with Stride projects. It's the recommended way to use this toolkit because:

- **Single entry point** - Give project path once, load assets/scenes by name
- **Auto-scanning** - Automatically discovers all assets on construction
- **Cleaner code** - `LoadScene("Level1")` vs `Scene.Load(fullPath)`

**Namespace:** `HS.Stride.Editor.Toolkit.Core`

### Constructor

```csharp
public StrideProject(string projectPath, ProjectMode projectMode = ProjectMode.Strict)
```

Creates a project instance and automatically scans all assets.

**Parameters:**

- `projectPath` (string) - Path to the Stride project root folder
- `projectMode` (ProjectMode, optional) - The validation mode to use for components. Defaults to `ProjectMode.Strict`.

**Throws:**

- `ArgumentNullException` - If projectPath is null/whitespace
- `ArgumentException` - If path is not a valid Stride project

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");
// Assets are now scanned and ready to use
```

### Properties

#### `string ProjectPath`

The project root directory path.

#### `string AssetsPath`

The full path to the Assets folder.

#### `ProjectMode Mode`

The current validation mode for the project. Can be changed after construction to switch between Strict and Loose modes.

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");
Console.WriteLine($"Current mode: {project.Mode}"); // ProjectMode.Strict

// Switch to loose mode for dynamic scenarios
project.Mode = ProjectMode.Loose;

// Now setting invalid properties won't throw
component.Set("NonExistentProperty", 123); // Works in Loose mode
```

### Scene Loading Methods

#### `Scene LoadScene(string sceneNameOrPath)`

Loads a scene by name or relative path.

**Parameters:**

- `sceneNameOrPath` (string) - Scene name (e.g., "Level1") or relative path (e.g., "Scenes/Level1")

**Returns:** `Scene` instance

**Throws:**

- `ArgumentNullException` - If sceneNameOrPath is null/whitespace
- `FileNotFoundException` - If scene not found

**Examples:**

```csharp
// By name
var scene1 = project.LoadScene("Level1");

// By relative path
var scene2 = project.LoadScene("Scenes/MainMenu");

// Nested folders
var scene3 = project.LoadScene("Levels/Chapter1/Intro");
```

#### `Scene CreateScene(string name, string? relativePath = null)`

Creates a new blank scene and saves it to the Assets folder. The scene is immediately saved to disk and returned ready for editing.

**Parameters:**

- `name` (string) - Name of the scene
- `relativePath` (string?, optional) - Relative path from Assets folder (e.g., "Scenes/Level1" or just "Level1")

**Returns:** `Scene` instance ready for editing

**Throws:**

- `ArgumentNullException` - If name is null/whitespace

**Examples:**

```csharp
// Create scene in Assets root
var scene1 = project.CreateScene("Level1");

// Create scene in subfolder
var scene2 = project.CreateScene("BossLevel", "Scenes/Chapter1");

// Start editing immediately
var player = scene1.CreateEntity("Player");
player.AddTransform();
scene1.Save();

// Rescan to register in project
project.Rescan();
```

### Asset Loading Methods

All asset loading methods follow the same pattern: `Load{AssetType}(string nameOrPath)`. Each of these methods returns a specific asset object (e.g., `MaterialAsset`, `TextureAsset`, `Prefab`) which represents the **editable programmatic instance** of that asset.

#### `MaterialAsset LoadMaterial(string nameOrPath)`

#### `TextureAsset LoadTexture(string nameOrPath)`

#### `AnimationAsset LoadAnimation(string nameOrPath)`

#### `ColliderShapeAsset LoadColliderShape(string nameOrPath)`

#### `PrefabAsset LoadPrefab(string nameOrPath)`

#### `UIPage LoadUIPage(string nameOrPath)`

#### `SoundAsset LoadSound(string nameOrPath)`

#### `SkeletonAsset LoadSkeleton(string nameOrPath)`

#### `SpriteSheetAsset LoadSpriteSheet(string nameOrPath)`

#### `EffectAsset LoadEffect(string nameOrPath)`

**Examples:**

```csharp
// Load by name - returns an editable MaterialAsset object
var material = project.LoadMaterial("PlayerMat");
var texture = project.LoadTexture("Skybox"); // Returns an editable TextureAsset object

// Load by path
var mat2 = project.LoadMaterial("Materials/Environment/Ground");
var tex2 = project.LoadTexture("Textures/UI/Icons/Health");

// All throw FileNotFoundException if not found
```

##### Generic Asset Loading (via AssetReference)

Sometimes you may want to load an asset's editable representation after obtaining a generic `AssetReference`. You can use the static `Load()` method available on each specific asset type.

```csharp
// 1. Find an AssetReference first
var anyAssetRef = project.FindAsset("MyDynamicAsset");

if (anyAssetRef != null)
{
    // 2. Load the specific asset type using its static Load method and the FilePath from the AssetReference
    switch (anyAssetRef.Type)
    {
        case AssetType.Material:
            var loadedMaterial = MaterialAsset.Load(anyAssetRef.FilePath);
            // Now you can work with 'loadedMaterial'
            break;
        case AssetType.Texture:
            var loadedTexture = TextureAsset.Load(anyAssetRef.FilePath);
            // Now you can work with 'loadedTexture'
            break;
        case AssetType.Prefab:
            var loadedPrefab = Prefab.Load(anyAssetRef.FilePath);
            // Now you can work with 'loadedPrefab'
            break;
        // ... handle other AssetType cases
        default:
            Console.WriteLine($"Asset {anyAssetRef.Name} is of type {anyAssetRef.Type} and cannot be directly loaded this way.");
            break;
    }
}
```

### Asset Finding Methods

All finding methods delegate to the internal ProjectScanner:

#### `AssetReference? FindAsset(string name, AssetType? type = null)`

Finds an asset by exact name.

```csharp
var prefab = project.FindAsset("Enemy", AssetType.Prefab);
var anyAsset = project.FindAsset("PlayerModel");
```

#### `List<AssetReference> FindAssets(string pattern, AssetType? type = null)`

Finds assets matching a wildcard pattern.

```csharp
var enemies = project.FindAssets("Enemy_*", AssetType.Prefab);
var allWeapons = project.FindAssets("Weapon*");
```

#### `AssetReference? FindAssetByPath(string path)`

Finds an asset by relative path (from Assets folder).

```csharp
var asset = project.FindAssetByPath("Characters/Player/PlayerModel");
```

#### `AssetReference? FindAssetByGuid(string guid)`

Finds an asset by its unique ID.

```csharp
var asset = project.FindAssetByGuid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
```

### Get All Assets Methods

#### `List<AssetReference> GetScenes()`

#### `List<AssetReference> GetPrefabs()`

#### `List<AssetReference> GetModels()`

#### `List<AssetReference> GetMaterials()`

#### `List<AssetReference> GetTextures()`

#### `List<AssetReference> GetAnimations()`

#### `List<AssetReference> GetSkeletons()`

#### `List<AssetReference> GetSounds()`

#### `List<AssetReference> GetUIPages()`

#### `List<AssetReference> GetSpriteSheets()`

#### `List<AssetReference> GetEffects()`

#### `List<AssetReference> GetScripts()`

#### `List<AssetReference> GetAssets(AssetType type)`

#### `List<AssetReference> GetAllAssets()`

**Examples:**

```csharp
var allScenes = project.GetScenes();
var allPrefabs = project.GetPrefabs();
var allMaterials = project.GetMaterials();

Console.WriteLine($"Project has {allScenes.Count} scenes");
```

### Utility Methods

#### `void Rescan()`

Rescans the project for assets. Call this if assets were added/removed externally.

```csharp
project.Rescan();
```

#### `string? GetRawAssetSource(AssetReference rawAssetReference)`

Gets the source file path for a RawAsset (.sdraw file points to actual content file in Resources/, representing external data).

**Note on RawAssets ("URL-like" references):**
`RawAsset`s are designed to give you a way to reference arbitrary external data files (like JSON, XML, TXT, CSV) that are managed by your Stride project. The toolkit does not directly load assets from arbitrary web URLs over HTTP. Instead, `RawAsset` is used for files that reside within your project's file system or local resources, offering a mechanism to retrieve their actual content path.

**Parameters:**

- `rawAssetReference` (AssetReference) - The RawAsset reference

**Returns:**

Full path to the source file (JSON/TXT/XML/CSV), or `null` in the following cases:

- The .sdraw file doesn't exist
- The .sdraw file has no `Source:` property
- The source path cannot be resolved
- The resolved source file doesn't exist

**Throws:**

- `ArgumentNullException` - If rawAssetReference is null
- `ArgumentException` - If asset is not of type RawAsset

**Fallback Behavior:**

If the relative path from the .sdraw file fails, the method automatically searches common Resources folders by filename.

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");

// Find a RawAsset (e.g., from a database system)
var dialogDB = project.FindAsset("merchant_dialog", AssetType.RawAsset);

// Get the actual JSON file path
var jsonPath = project.GetRawAssetSource(dialogDB);

if (jsonPath != null)
{
    // Read and parse the actual content
    var jsonContent = File.ReadAllText(jsonPath);

    // Using Newtonsoft.Json or System.Text.Json
    var dialogData = JsonConvert.DeserializeObject<DialogTree>(jsonContent);

    Console.WriteLine($"Dialog: {dialogData.Name}");
    Console.WriteLine($"Lines: {dialogData.Lines.Count}");
}
```

**Use Case - Mass Import Systems:**

Perfect for working with external data management tools that create RawAsset databases:

```csharp
// After using a data manager tool to import hundreds of files
var project = new StrideProject(@"C:\MyGame");
var allDialogs = project.FindAssets("*", AssetType.RawAsset)
    .Where(a => a.Path.Contains("DialogSystem_db"));

foreach (var dialogAsset in allDialogs)
{
    // Get the actual content file
    var sourcePath = project.GetRawAssetSource(dialogAsset);
    if (sourcePath != null)
    {
        var json = File.ReadAllText(sourcePath);
        var dialog = JsonConvert.DeserializeObject<DialogData>(json);

        // Process or validate content
        Console.WriteLine($"Loaded: {dialog.CharacterName} - {dialog.LineCount} lines");
    }
}
```

#### `string? GetAssetSource(AssetReference assetReference)`

Gets the source file path for any asset that has a Source property. Works with Textures (.sdtex), SpriteSheets (.sdsheet), SpriteFonts (.sdfnt), Animations (.sdanim), RawAssets (.sdraw), and other assets with Source properties.

**Parameters:**

- `assetReference` (AssetReference) - The asset reference

**Returns:**

Full absolute path to the source file, or `null` if:

- The asset file doesn't exist
- The asset has no `Source:` property
- The source path cannot be resolved
- The resolved source file doesn't exist

**Throws:**

- `ArgumentNullException` - If assetReference is null

**Fallback Behavior:**

If the relative path from the asset file fails, the method automatically searches common Resources folders by filename.

**Special Handling:**

- For SpriteFonts (.sdfnt), extracts the font file path from the nested `FontSource` section
- For other assets, looks for a top-level `Source:` property

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");

// Get source for a texture
var texture = project.FindAsset("PlayerSkin", AssetType.Texture);
var texturePath = project.GetAssetSource(texture);
// Returns: "C:\MyGame\Assets\Textures\player_skin.png"

// Get source for a sprite font (extracts .ttf/.otf path)
var font = project.FindAsset("GameFont", AssetType.SpriteFont);
var fontPath = project.GetAssetSource(font);
// Returns: "C:\MyGame\Resources\Fonts\Saira-Bold.ttf"

// Get source for an animation
var anim = project.FindAsset("WalkCycle", AssetType.Animation);
var animPath = project.GetAssetSource(anim);
// Returns: "C:\MyGame\Assets\Animations\character_walk.fbx"
```

### Complete Workflow Example

```csharp
using HS.Stride.Editor.Toolkit.Core;
using HS.Stride.Editor.Toolkit.Core.AssetEditing;

// Initialize project
var project = new StrideProject(@"C:\MyGame");

// Load and modify scene
var scene = project.LoadScene("Level1");
var player = scene.FindEntityByName("Player");

var transform = player.GetTransform();
transform.SetPosition(0, 10, 0);

// Use asset references
var enemyPrefab = project.FindAsset("Enemy", AssetType.Prefab);
if (enemyPrefab != null)
{
    scene.InstantiatePrefab(enemyPrefab, new Vector3Data(5, 0, 5));
}

// Load and modify material
var material = project.LoadMaterial("PlayerMat");
var newTexture = project.FindAsset("NewSkin", AssetType.Texture);
material.SetDiffuseTexture(newTexture.Reference);
material.Save();

// Save scene
scene.Save();

// Work with multiple scenes
foreach (var sceneRef in project.GetScenes())
{
    var s = project.LoadScene(sceneRef.Name);
    // Batch modify...
    s.Save();
}
```

---

## Scene Manipulation API

### Scene Class

**Namespace:** `HS.Stride.Editor.Toolkit.Core.SceneEditing`

The `Scene` class provides methods to load, query, and modify Stride scene files.

#### Static Methods

##### `Scene.Load(string filePath)`

Loads a scene from disk.

**Parameters:**

- `filePath` (string) - Absolute path to the `.sdscene` file

**Returns:** `Scene` instance

**Throws:**

- `ArgumentNullException` - If filePath is null or whitespace
- `FileNotFoundException` - If file doesn't exist

**Example:**

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");

// Alternative: Load directly by full path
var scene = Scene.Load(@"C:\MyGame\Assets\Scenes\Level1.sdscene");
```

#### Properties

##### `string Id`

The unique GUID of the scene asset.

##### `string FilePath`

The file path this scene was loaded from.

##### `List<Entity> AllEntities`

Returns all entities in the scene (including nested children).

#### Find Methods

##### `Entity? FindEntityById(string id)`

Finds an entity by its GUID.

**Parameters:**

- `id` (string) - The entity's GUID

**Returns:** Entity or null if not found

**Throws:** `ArgumentNullException` if id is null/whitespace

**Example:**

```csharp
var entity = scene.FindEntityById("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
```

##### `Entity? FindEntityByName(string name)`

Finds the first entity with an exact name match.

**Parameters:**

- `name` (string) - Exact entity name

**Returns:** Entity or null if not found

**Throws:** `ArgumentNullException` if name is null/whitespace

**Example:**

```csharp
var player = scene.FindEntityByName("Player");
```

##### `List<Entity> FindEntitiesByName(string pattern)`

Finds all entities matching a wildcard pattern.

**Parameters:**

- `pattern` (string) - Pattern with `*` (any characters) or `?` (single character)

**Returns:** List of matching entities (empty if none found)

**Throws:** `ArgumentNullException` if pattern is null/whitespace

**Example:**

```csharp
// Find all enemies
var enemies = scene.FindEntitiesByName("Enemy_*");

// Find numbered entities
var items = scene.FindEntitiesByName("Item_??");
```

##### `List<Entity> FindEntitiesWithComponent(string componentType)`

Finds all entities that have a specific component type.

**Parameters:**

- `componentType` (string) - Component type name (can be partial, e.g., "ModelComponent" or just "Model")

**Returns:** List of entities with that component

**Throws:** `ArgumentNullException` if componentType is null/whitespace

**Example:**

```csharp
// Find all entities with models
var modeled = scene.FindEntitiesWithComponent("ModelComponent");

// Find all entities with custom character controller
var characters = scene.FindEntitiesWithComponent("CharacterController");
```

##### `List<Entity> FindEntities(Func<Entity, bool> predicate)`

Finds all entities matching a custom predicate (LINQ query).

**Parameters:**

- `predicate` (Func<Entity, bool>) - Custom filter function

**Returns:** List of matching entities

**Throws:** `ArgumentNullException` if predicate is null

**Example:**

```csharp
// Find all physics-enabled entities
var physicsObjects = scene.FindEntities(e =>
    e.HasComponent("RigidbodyComponent") ||
    e.HasComponent("StaticColliderComponent"));

// Complex query
var targets = scene.FindEntities(e =>
    e.Name.StartsWith("Target_") &&
    e.HasComponent("ModelComponent") &&
    !e.HasComponent("DestroyedTag"));
```

#### Entity Manipulation

##### `Entity CreateEntity(string name, string? folder = null)`

Creates a new entity with a `TransformComponent`. This method is suitable for creating top-level entities or organizing them into editor folders (metadata only, no transform hierarchy). For entity-based parenting, see the overload below.

**Parameters:**

- `name` (string) - Entity name
- `folder` (string?, optional) - Optional folder path for organization in the editor (e.g., "Enemies/Bosses"). This defines an editor folder for organizational purposes and does not create a parent-child transform hierarchy.

**Returns:** The newly created Entity

**Throws:** `ArgumentNullException` if name is null/whitespace

**Example:**

```csharp
// Creates a new entity named "Powerup" and organizes it under an "Items" folder in the editor.
// The entity itself will be a root entity in terms of transform hierarchy.
var newEntity = scene.CreateEntity("Powerup", "Items");
```

##### `Entity CreateEntity(string name, string parent, ParentType parentType)`

Creates a new entity with specified parent organization.

**Parameters:**

- `name` (string) - Entity name
- `parent` (string) - Parent name (folder name or entity name). Supports nested paths like "House/Floor/Room"
- `parentType` (ParentType) - Type of organization:
  - `ParentType.Folder` - Folder-based organization (metadata only, no transform hierarchy)
  - `ParentType.Entity` - Entity-based parenting (transform hierarchy, children follow parent)

**Returns:** The newly created Entity

**Throws:**

- `ArgumentNullException` if name or parent is null/whitespace
- `InvalidOperationException` if parent entity not found (when using ParentType.Entity)

**Behavior:**

- With `ParentType.Folder`: Entity appears in folder in editor, but has independent transform
- With `ParentType.Entity`: Entity becomes child of parent entity, transform is relative to parent
- Nested paths auto-create missing parent entities (e.g., "Level1/Level2/Level3")

**Example:**

```csharp
// Folder organization (Stride/Unreal style - can't move folder)
var enemy = scene.CreateEntity("Enemy_01", "Enemies", ParentType.Folder);

// Entity hierarchy (Unity style - moving parent moves children)
var door = scene.CreateEntity("Door", "House", ParentType.Entity);

// Nested hierarchy (auto-creates House -> Floor -> Room)
var collider = scene.CreateEntity("Collider", "House/Floor/Room", ParentType.Entity);
```

##### `Entity InstantiatePrefab(AssetReference prefab, Vector3Data? position = null, string? folder = null)`

Instantiates a prefab in the scene.

**Parameters:**

- `prefab` (AssetReference) - Reference to the prefab asset
- `position` (Vector3Data?, optional) - Spawn position (defaults to 0,0,0)
- `folder` (string?, optional) - Folder path

**Returns:** The instantiated root entity

**Throws:** `ArgumentNullException` if prefab is null

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");
var enemyPrefab = project.FindAsset("Enemy", AssetType.Prefab);

var enemy = scene.InstantiatePrefab(enemyPrefab,
    new Vector3Data(10, 0, 5),
    "Enemies");
```

##### `void RemoveEntity(Entity entity)`

Removes an entity from the scene.

**Parameters:**

- `entity` (Entity) - The entity to remove

**Throws:** `ArgumentNullException` if entity is null

**Example:**

```csharp
var oldEntity = scene.FindEntityByName("Deprecated");
scene.RemoveEntity(oldEntity);
```

##### `void RemoveEntity(string entityId)`

Removes an entity by GUID.

**Parameters:**

- `entityId` (string) - The entity's GUID

**Throws:** `ArgumentNullException` if entityId is null/whitespace

#### Save Methods

##### `void Save()`

Saves the scene back to its original file path and reloads it.

**Example:**

```csharp
scene.Save();
```

##### `void SaveAs(string filePath)`

Saves the scene to a new file path.

**Parameters:**

- `filePath` (string) - Target file path

**Throws:** `ArgumentNullException` if filePath is null/whitespace

**Example:**

```csharp
scene.SaveAs(@"C:\MyGame\Assets\Scenes\Level1_Modified.sdscene");
```

##### `void Reload()`

Reloads the scene from disk, discarding any unsaved changes.

---

## Prefab Creation (Programmatically)

**Namespace:** `HS.Stride.Editor.Toolkit.Core.PrefabEditing`

Create prefabs programmatically without opening Stride Editor. Perfect for mass-generating enemy types, props, characters, or any reusable game objects.

### Creating a New Prefab

```csharp
var project = new StrideProject(@"C:\MyGame");

// Create prefab (automatically creates root entity with TransformComponent)
var prefab = project.CreatePrefab("Crate", "Prefabs/Props");

// Get root entity and add components
var root = prefab.GetRootEntity();
root.AddModel();
root.AddStaticCollider().AddBoxShape(1.0f, 1.0f, 1.0f);

// Save to disk
prefab.Save();

// Rescan to register in project
project.Rescan();
```

### Mass Enemy Generation Example

```csharp
var project = new StrideProject(@"C:\MyGame");

var enemyTypes = new Dictionary<string, (float health, float speed, string model)>
{
    ["Goblin"] = (50f, 3.0f, "GoblinModel"),
    ["Orc"] = (100f, 2.0f, "OrcModel"),
    ["Troll"] = (200f, 1.5f, "TrollModel"),
    ["Dragon"] = (500f, 5.0f, "DragonModel")
};

foreach (var enemy in enemyTypes)
{
    var prefab = project.CreatePrefab(enemy.Key, $"Prefabs/Enemies/{enemy.Key}");
    var root = prefab.GetRootEntity();

    // Add model
    var modelAsset = project.FindAsset(enemy.Value.model, AssetType.Model);
    if (modelAsset != null)
    {
        var model = root.AddModel();
        model.SetModel(modelAsset);
    }

    // Add collider
    root.AddStaticCollider().AddCapsuleShape(0.5f, 2.0f);

    // Add AI script with properties
    var aiScript = root.AddComponent("EnemyAI");
    aiScript.Set("MaxHealth", enemy.Value.health);
    aiScript.Set("MoveSpeed", enemy.Value.speed);

    prefab.Save();
}

project.Rescan();
Console.WriteLine($"Generated {enemyTypes.Count} enemy prefabs!");
```

### Character Variants Example

```csharp
var project = new StrideProject(@"C:\MyGame");

var classes = new[] { "Warrior", "Archer", "Mage" };
var equipment = new Dictionary<string, string[]>
{
    ["Warrior"] = new[] { "Sword", "Shield", "Helmet" },
    ["Archer"] = new[] { "Bow", "Quiver", "Hood" },
    ["Mage"] = new[] { "Staff", "Robe", "Hat" }
};

foreach (var className in classes)
{
    var prefab = project.CreatePrefab($"Player_{className}", "Prefabs/Characters");
    var root = prefab.GetRootEntity();

    // Add character model
    root.AddModel();
    root.AddRigidbody(mass: 70f);

    // Create equipment children
    foreach (var item in equipment[className])
    {
        var equipSlot = prefab.CreateEntity(item, root.Name, ParentType.Entity);
        equipSlot.AddModel();
    }

    prefab.Save();
}

project.Rescan();
```

### Key Methods

- `project.CreatePrefab(name, relativePath)` - Creates new prefab
- `Prefab.Load(filePath)` - Loads existing prefab
- `prefab.GetRootEntity()` - Gets root entity
- `prefab.CreateEntity(name, parent, ParentType)` - Adds child entity
- `prefab.Save()` - Saves changes
- `prefab.SaveAs(filePath)` - Saves to new location

### Prefab Class

**Namespace:** `HS.Stride.Editor.Toolkit.Core.PrefabEditing`

Represents an editable Stride Prefab asset (`.sdprefab`). This class allows for programmatic management of prefabs, including creating new ones, loading existing ones, and manipulating the entities within them.

#### Loading and Creation

##### `static Prefab Load(string filePath)`

Loads a prefab asset from the specified `.sdprefab` file path.

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var prefab = project.LoadPrefab("Enemy");

// Alternative: Load directly by full path
var prefab = Prefab.Load(@"C:\MyGame\Assets\Prefabs\Enemy.sdprefab");
```

#### Properties

##### `string Id`

The unique GUID of this prefab. (Inherited from `IStrideAsset`).

##### `string FilePath`

The absolute file path where this prefab is stored. (Inherited from `IStrideAsset`).

##### `List<Entity> AllEntities`

A list of all entities contained within this prefab, including the root and its children.

#### Entity Management Methods

##### `Entity? GetRootEntity()`

Gets the root entity of this prefab. This is the top-level entity in the prefab's hierarchy.

**Returns:** The root `Entity` or `null` if no root entity exists (which shouldn't happen for a properly created prefab).

**Example:**

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var prefab = project.LoadPrefab("Enemy");
var root = prefab.GetRootEntity();

if (root != null)
{
    // Modify root entity's transform
    root.GetTransform().SetScale(1.5f, 1.5f, 1.5f);
    prefab.Save();
}
```

##### `Entity CreateEntity(string name, string? folder = null)`

Creates a new entity within this prefab with a `TransformComponent`.

**Parameters:**

- `name` (string): The name of the new entity.
- `folder` (string?, optional): Optional folder path for organization within the prefab (e.g., "WeaponMounts").

**Returns:** The newly created `Entity`.

**Example:**

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var prefab = project.LoadPrefab("House");

// Add a new entity to the prefab
var window = prefab.CreateEntity("Window", "Exterior");
window.AddModel(); // Add a ModelComponent to the window entity
prefab.Save();
```

##### `Entity CreateEntity(string name, string parent, ParentType parentType)`

Creates a new entity within this prefab with specified parent organization (either a folder or another entity).

**Parameters:**

- `name` (string): The name of the new entity.
- `parent` (string): The name of the parent folder or entity. Supports nested paths like "House/Floor/Room".
- `parentType` (ParentType): Specifies whether `parent` refers to a folder (`ParentType.Folder`) or an entity (`ParentType.Entity`).

**Returns:** The newly created `Entity`.

**Example:**

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var prefab = project.LoadPrefab("House");

// Create a door handle as a child of an existing "Door" entity within the prefab
var doorHandle = prefab.CreateEntity("Handle", "Door", ParentType.Entity);
doorHandle.AddModel(); // Add components to the handle
prefab.Save();
```

##### `void RemoveEntity(Entity entity)`

Removes an entity from this prefab.

**Parameters:**

- `entity` (Entity): The entity to remove.

#### Entity Finding Methods

These methods are similar to those found in the `Scene` class, but they operate specifically on the entities within this prefab.

##### `Entity? FindEntityById(string id)`

Finds an entity by its unique GUID within this prefab.

##### `Entity? FindEntityByName(string name)`

Finds the first entity with an exact name match within this prefab.

##### `List<Entity> FindEntitiesByName(string pattern)`

Finds all entities matching a wildcard pattern (`*` or `?`) within this prefab.

##### `List<Entity> FindEntitiesWithComponent(string componentType)`

Finds all entities that have a specific component type within this prefab.

##### `List<Entity> FindEntities(Func<Entity, bool> predicate)`

Finds entities using a custom predicate function (LINQ query) within this prefab.

#### Persistence Methods

##### `void Save()`

Saves the prefab's current state back to its original file. (Inherited from `IStrideAsset`).

##### `void SaveAs(string filePath)`

Saves the prefab's current state to a new file path. (Inherited from `IStrideAsset`).

---

## UI Page Creation (Programmatically)

**Namespace:** `HS.Stride.Editor.Toolkit.Core.UIPageEditing`

The toolkit provides a powerful API for programmatically creating, modifying, and managing Stride UI pages without needing to use the Game Studio editor. This is ideal for generating entire UI systems, creating dynamic UI elements, or integrating with generative AI tools.

### UIPage Class

Represents a Stride UI page (`.sduipage` file) that can be loaded, modified, and saved.

#### Loading and Creation

##### `static UIPage Load(string filePath)`

Loads a UI page from a `.sduipage` file on disk.

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var page = project.LoadUIPage("MainMenu");

// Alternative: Load directly by full path
var page = UIPage.Load(@"C:\MyGame\Assets\UI\MainMenu.sduipage");
```

#### Properties

##### `string Id`

The unique GUID of the UI page.

##### `string FilePath`

The absolute file path where this UI page is stored.

##### `Dictionary<string, float> Resolution`

The current design resolution of the UI page (e.g., {"X":1920, "Y":1080, "Z":1000}).

##### `List<UIElement> AllElements`

A list of all `UIElement` instances within this page, including nested children.

##### `List<UIElement> RootElements`

A list of the top-level `UIElement` instances in the page's hierarchy (usually just one root `Grid`).

#### Element Manipulation Methods

##### `UIElement CreateElement(string type, string name, UIElement? parent = null)`

Creates a new UI element of a specified type and adds it to the page.

**Parameters:**

- `type` (string): The type of UI element to create (e.g., "Grid", "TextBlock", "Button", "ImageElement", "Canvas", "StackPanel", "ScrollViewer", "EditText").
- `name` (string): The unique name of the new element.
- `parent` (UIElement?, optional): An optional parent `UIElement` to nest this new element under. If `null`, it attempts to attach to the page's root `Grid`.

**Returns:** The newly created `UIElement`.

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");
var page = project.CreateUIPage("MainMenu", "UI/MainMenu");
var rootGrid = page.RootElements.First(); // Get the auto-created root Grid

// Create a TextBlock as a child of the root grid
var titleText = page.CreateElement("TextBlock", "GameTitle", rootGrid);
titleText.Set("Text", "My Stride Game");

// Create a Button and set its parent
var startButton = page.CreateElement("Button", "StartButton", rootGrid);
```

##### `bool RemoveElement(UIElement element)`

Removes a UI element from the page and its parent.

**Parameters:**

- `element` (UIElement): The element to remove.

**Returns:** `true` if the element was successfully removed, `false` otherwise.

#### Element Finding Methods

##### `UIElement? FindElementById(string id)`

Finds a UI element by its unique GUID.

##### `UIElement? FindElementByName(string name)`

Finds the first UI element with an exact name match.

##### `IEnumerable<UIElement> FindElementsByName(string pattern)`

Finds all UI elements matching a wildcard pattern (`*` for any characters, `?` for a single character).

##### `IEnumerable<UIElement> FindElementsByType(string type)`

Finds all UI elements of a specific type (e.g., "TextBlock", "Button").

##### `IEnumerable<UIElement> FindElements(Func<UIElement, bool> predicate)`

Finds UI elements using a custom predicate (LINQ query).

#### Page Management

##### `void Save()`

Saves the UI page back to its original file path. Throws `InvalidOperationException` if `FilePath` is not set.

##### `void SaveAs(string filePath)`

Saves the UI page to a new file path.

#### Generic Property Access

##### `object? Get(string propertyName)`

Gets a page-level property value by name. Supports nested paths with dot notation.

##### `void Set(string propertyName, object value)`

Sets a page-level property value by name. Supports nested paths with dot notation.

##### `Dictionary<string, object> GetAllProperties()`

Gets all page-level properties as a dictionary.

---

### UIElement Class

Represents an individual UI element within a `UIPage`. This class provides methods to manipulate an element's properties and its relationship within the UI hierarchy.

#### Properties

##### `string Id`

The unique GUID of the UI element.

##### `string Name`

The name of the UI element (e.g., "PlayerHealthBar", "SettingsButton").

##### `string Type`

The type of the UI element (e.g., "TextBlock", "Button", "Grid", "Canvas").

##### `Dictionary<string, object> Properties`

A dictionary containing all the UI element's properties (e.g., "Margin", "Color", "Text").

##### `Dictionary<string, UIElement> Children`

A dictionary of child UI elements (for container types like `Grid`, `Canvas`, `StackPanel`). The key is an internal hash, and the value is the child `UIElement`.

##### `UIElement? Parent`

A reference to the parent `UIElement` (null if it's a root element).

##### `UIPage? ParentPage`

A reference to the `UIPage` this element belongs to.

#### Methods

##### `T? Get<T>(string key)`

Gets a property value of type `T` by its key. Performs automatic type conversion.

##### `void Set(string key, object value)`

Sets a property value by its key.

##### `bool HasProperty(string key)`

Checks if a specific property exists on this UI element.

##### `List<UIElement> GetChildren()`

Gets a list of all direct child UI elements.

##### `void AddChild(UIElement child)`

Adds a `UIElement` as a child to this element (making this element a container).

**Example:**

```csharp
var parentCanvas = page.CreateElement("Canvas", "MyCanvas");
var button = page.CreateElement("Button", "MyButton");
parentCanvas.AddChild(button); // Nest the button under the canvas
```

##### `bool RemoveChild(UIElement child)`

Removes a specified child `UIElement` from this element's children.

##### `UIElement? FindChildByName(string name)`

Finds a direct child UI element by exact name.

##### `List<UIElement> GetDescendants()`

Gets a list of all descendant UI elements (children, grandchildren, etc.) recursively.

##### `int GetZIndex()`

Gets the ZIndex (Panel.ZIndex) value from DependencyProperties. ZIndex controls sibling draw order within a panel - higher values are drawn on top. Returns 0 if not set.

```csharp
int zIndex = element.GetZIndex();
Console.WriteLine($"Element draw order: {zIndex}");
```

##### `void SetZIndex(int zIndex)`

Sets the ZIndex (Panel.ZIndex) value in DependencyProperties. ZIndex controls sibling draw order within a panel - higher values are drawn on top. Setting to 0 removes the ZIndex entry (0 is the default).

```csharp
// Set element to render on top of siblings
element.SetZIndex(10);

// Reset to default (removes ZIndex entry)
element.SetZIndex(0);
```

**Note:** ZIndex is stored in Stride's DependencyProperties with the format `<guid>~Panel.ZIndexPropertyKey`. The toolkit handles this format automatically.

---

### Complete Pause Menu Example

```csharp
var project = new StrideProject(@"C:\MyGame");
var page = project.CreateUIPage("PauseMenu", "UI/Menus");

var rootGrid = page.RootElements.First();

// Background overlay
var bgImage = page.CreateImage("background", parent: rootGrid);
bgImage.SetSize(1280f, 720f);
bgImage.SetBackgroundColor(0, 0, 0, 180);

// Menu container
var menuCanvas = page.CreateCanvas("menu_container", parent: rootGrid);
menuCanvas.SetSize(600f, 500f);
menuCanvas.SetAlignment("Center", "Center");

// Title
var title = page.CreateTextBlock("title", "GAME PAUSED", parent: menuCanvas, fontSize: 40f);
title.SetMargin(top: 50f);
title.SetHorizontalAlignment("Center");
title.SetTextColor(255, 255, 255);

// Buttons
var buttons = new[] {
    ("resume_btn", "Resume Game", 150f),
    ("settings_btn", "Settings", 230f),
    ("mainmenu_btn", "Main Menu", 310f),
    ("quit_btn", "Quit Game", 390f)
};

foreach (var (name, text, yPos) in buttons)
{
    var btn = page.CreateButton(name, text, parent: menuCanvas, width: 400f, height: 60f);
    btn.SetMargin(left: 100f, top: yPos);
}

page.Save();
project.Rescan();
```

### HUD with Health/Ammo Example

```csharp
var project = new StrideProject(@"C:\MyGame");
var page = project.CreateUIPage("GameHUD", "UI");

var rootGrid = page.RootElements.First();

// Health bar (bottom-left)
var healthCanvas = page.CreateCanvas("health_container", parent: rootGrid);
healthCanvas.SetSize(300f, 50f);
healthCanvas.SetMargin(left: 20f, bottom: 20f);
healthCanvas.SetAlignment("Left", "Bottom");

var healthBg = page.CreateImage("health_bg", parent: healthCanvas);
healthBg.SetSize(300f, 50f);
healthBg.SetBackgroundColor(60, 60, 60);

var healthFg = page.CreateImage("health_bar", parent: healthCanvas);
healthFg.SetSize(300f, 50f);
healthFg.SetBackgroundColor(200, 0, 0);

var healthText = page.CreateTextBlock("health_text", "100 / 100", parent: healthCanvas);
healthText.SetAlignment("Center", "Center");
healthText.SetTextColor(255, 255, 255);

// Ammo counter (bottom-right)
var ammoCanvas = page.CreateCanvas("ammo_container", parent: rootGrid);
ammoCanvas.SetSize(200f, 100f);
ammoCanvas.SetMargin(right: 20f, bottom: 20f);
ammoCanvas.SetAlignment("Right", "Bottom");

var ammoText = page.CreateTextBlock("ammo_text", "30 / 120", parent: ammoCanvas, fontSize: 60f);
ammoText.SetAlignment("Right", "Center");
ammoText.SetTextColor(255, 255, 0);

page.Save();
project.Rescan();
```

---

### Key Methods

**UIPage Creation & Management:**

- `project.CreateUIPage(name, relativePath)`: Creates a new UI page asset.
- `UIPage.Load(filePath)`: Loads an existing UI page from a file.
- `page.Save()` / `page.SaveAs(filePath)`: Saves changes to the UI page.
- `page.GetDesignResolution()`: Gets the design resolution (X, Y, Z).
- `page.SetDesignResolution(x, y, z)`: Sets the design resolution.

**Element Creation Helpers:**

- `page.CreateElement(type, name, parent)`: Creates any UI element type.
- `page.CreateTextBlock(name, text, parent, fontSize, alignment...)`: Creates a text label.
- `page.CreateButton(name, text, parent, width, height)`: Creates a button with text.
- `page.CreateImage(name, spriteSheet, frame, parent, width, height)`: Creates an image from sprite sheet.
- `page.CreateImageFromTexture(name, texture, parent, width, height)`: Creates an image from texture.
- `page.CreateCanvas(name, parent, width, height)`: Creates a canvas container.
- `page.CreateGrid(name, parent)`: Creates a grid container.
- `page.CreateStackPanel(name, parent)`: Creates a stack panel container.
- `page.CreateScrollViewer(name, contentElement, parent)`: Creates a scroll viewer.
- `page.CreateEditText(name, placeholder, parent, width, height)`: Creates a text input field.
- `page.CreateSlider(name, min, max, value, parent, width)`: Creates a slider.
- `page.CreateToggleButton(name, text, isChecked, parent, width, height)`: Creates a toggle/checkbox.
- `page.CreateScrollBar(name, isVertical, parent, width, height)`: Creates a scrollbar.

**Layout & Sizing:**

- `element.SetSize(width, height)`: Sets width and height.
- `element.SetWidth(width)` / `element.SetHeight(height)`: Sets individual dimensions.
- `element.SetMargin(left, top, right, bottom)`: Sets margins for positioning.
- `element.SetPadding(left, top, right, bottom)`: Sets padding.
- `element.SetAlignment(horizontal, vertical)`: Sets alignment ("Left", "Center", "Right", "Top", "Bottom", "Stretch").

**Colors & Appearance:**

- `element.SetBackgroundColor(r, g, b, a)`: Sets background color.
- `element.SetTextColor(r, g, b, a)`: Sets text color (TextBlock).
- `element.SetColor(r, g, b, a)`: Sets tint color (ImageElement).
- `element.SetOpacity(value)`: Sets transparency (0.0-1.0).
- `element.SetVisibility(visible)`: Shows or hides element.

**Assets (Using AssetReference):**

- `element.SetFont(fontAsset)`: Sets font for TextBlock.
- `element.SetSprite(spriteSheetAsset, frame)`: Sets sprite from sheet.
- `element.SetTexture(textureAsset)`: Sets texture directly.
- `element.SetPressedImage/SetNotPressedImage/SetMouseOverImage`: Button images from sprites.
- `element.SetPressedTexture/SetNotPressedTexture/SetMouseOverTexture`: Button images from textures.

**Element Finding:**

- `page.FindElementById(id)`: Finds element by ID.
- `page.FindElementByName(name)`: Finds element by exact name.
- `page.FindElementsByName(pattern)`: Finds elements by wildcard pattern.
- `page.FindElementsByType(type)`: Finds elements by type.
- `page.FindElements(predicate)`: Finds elements using custom filter.

**Hierarchy:**

- `element.AddChild(child)`: Adds a child element.
- `element.RemoveChild(child)`: Removes a child element.
- `element.GetChildren()`: Gets all direct children.
- `element.GetDescendants()`: Gets all descendants recursively.

**ZIndex (Draw Order):**

- `element.GetZIndex()`: Gets the Panel.ZIndex value (controls sibling draw order).
- `element.SetZIndex(int)`: Sets the Panel.ZIndex value (higher = drawn on top).

### UI Element Types

The `CreateElement` method supports all 16 Stride UI element types:

- **TextBlock**: Displays text.
- **Button**: A clickable button.
- **ToggleButton**: A checkbox or toggle switch.
- **ImageElement**: Displays sprites or textures.
- **Canvas**: Container for absolute positioning.
- **Grid**: Container for grid-based layout.
- **StackPanel**: Container that stacks elements horizontally or vertically.
- **UniformGrid**: Grid with uniform cell sizes.
- **ScrollViewer**: Container that provides scrolling for content.
- **ScrollBar**: Standalone scrollbar control.
- **ScrollingText**: Scrolling/marquee text display.
- **EditText**: Text input field.
- **Slider**: Value slider control.
- **Border**: Container with border styling.
- **ContentDecorator**: Decorator for content styling.
- **ModalElement**: Modal overlay container.

---

### Position and Margin Helpers (v1.6.0+)

Helper methods for working with element positioning:

#### `GetPosition()` / `SetPosition()`

Gets or sets the element position using margin values.

```csharp
// Set position
element.SetPosition(100f, 200f);  // Sets Left and Top margin

// Get position (returns X and Y from margin)
var pos = element.GetPosition();
Console.WriteLine($"Position: {pos.X}, {pos.Y}");
```

#### `GetMargin()` / `SetMargin()`

Gets or sets the full margin rectangle.

```csharp
// Set full margin
element.SetMargin(left: 10f, top: 20f, right: 5f, bottom: 15f);

// Get margin values
var margin = element.GetMargin();
Console.WriteLine($"Left: {margin.Left}, Top: {margin.Top}");
```

---

### Sprite and Texture Detection (v1.6.0+)

Helper methods for detecting and working with sprite/texture sources:

#### `IsSpriteFromSheet()` / `IsSpriteFromTexture()`

Checks what type of source is set for a sprite property.

```csharp
// Check if using sprite sheet vs texture
if (imageElement.IsSpriteFromSheet("Source"))
{
    var sprite = imageElement.GetSpriteSheet("Source");
    Console.WriteLine($"Sprite sheet: {sprite?.AssetReference}, Frame: {sprite?.Frame}");
}
else if (imageElement.IsSpriteFromTexture("Source"))
{
    var texture = imageElement.GetTextureSource("Source");
    Console.WriteLine($"Texture: {texture}");
}
```

#### Button-Specific Sprite Detection

```csharp
// Check if button is using sprite sheet for images
if (button.IsButtonUsingSpriteSheet())
{
    var pressed = button.GetPressedImageSprite();
    var notPressed = button.GetNotPressedImageSprite();
    var mouseOver = button.GetMouseOverImageSprite();
}
else if (button.IsButtonUsingTexture())
{
    var pressed = button.GetPressedImageTexture();
    // etc.
}
```

#### ImageElement Sprite Helpers

```csharp
// Check if ImageElement is using sprite sheet
if (image.IsImageUsingSpriteSheet())
{
    var sprite = image.GetImageSprite();
    Console.WriteLine($"Using sprite sheet: {sprite?.AssetReference}");
}
```

---

### Content Management (v1.6.0+)

Helper methods for managing content properties on Button, ToggleButton, and ScrollViewer:

#### `SetContent()` / `GetContent()`

```csharp
// Create a button and set its content
var button = page.CreateButton("my_btn", "Click Me", root);
var textBlock = page.CreateTextBlock("btn_text", "Custom Text", autoAttach: false);
button.SetContent(textBlock);

// Get the content element
var content = button.GetContent();
```

---

### Positioning System (v1.6.0+)

The toolkit uses Stride's 4-margin positioning system for accurate element placement.

#### How Stride Positioning Works

Stride calculates element position using all 4 margin values. For absolute positioning:

```
Left   = X position
Top    = Y position
Right  = ParentWidth - (X + ElementWidth)
Bottom = ParentHeight - (Y + ElementHeight)
```

#### SetPosition (Automatic 4-Margin Calculation)

```csharp
// SetPosition automatically calculates all 4 margins
// and sets Left/Top alignment for absolute positioning
button.SetSize(200f, 50f);
button.SetPosition(100f, 200f);  // Places button at (100, 200)

// This generates:
// Margin: {Left: 100, Top: 200, Right: 1620, Bottom: 830}
// HorizontalAlignment: Left
// VerticalAlignment: Top
```

#### GetParentDimensions

```csharp
// Get the parent container size (or design resolution if root)
var (parentWidth, parentHeight) = element.GetParentDimensions();
```

#### Stretch Fill (for Backgrounds)

```csharp
// For background images that should fill the entire page
var bg = page.CreateImage("background", parent: root, stretchFill: true);
bg.SetBackgroundColor(20, 20, 40, 200);

// Or apply stretch fill to any existing element
element.SetStretchFill();

// For ImageElements specifically (adds StretchType and StretchDirection)
image.SetImageStretchFill();
```

#### CreateImage with stretchFill Parameter

```csharp
// Create a background image that fills the page
var bg = page.CreateImage("bg_overlay", parent: root, stretchFill: true);

// This sets:
// HorizontalAlignment: Stretch
// VerticalAlignment: Stretch
// StretchType: Fill
// StretchDirection: Both
// Margin: {Left: 0, Top: 0, Right: 0, Bottom: 0}
// (No Width/Height - Stretch fills parent)
```

---

### Additional UIElement Properties (v1.6.0+)

New getter/setter methods for UI element properties:

#### Behavior Properties

```csharp
// Enable/disable elements
element.SetIsEnabled(true);
var enabled = element.GetIsEnabled();

// Button click mode
button.SetClickMode("Release");  // "Press" or "Release"
var mode = button.GetClickMode();

// Text wrapping
textBlock.SetWrapText(true);
var wraps = textBlock.GetWrapText();

// Slider properties
slider.SetMinimum(0f);
slider.SetMaximum(100f);
slider.SetValue(50f);
slider.SetStep(1f);
slider.SetTickFrequency(10f);
slider.SetSnapToTicks(true);
slider.SetOrientation("Horizontal");  // or "Vertical"

// EditText properties
editText.SetMaxLength(100);
editText.SetIsReadOnly(false);

// ToggleButton properties
toggle.SetIsChecked(true);
toggle.SetIsThreeState(false);
```

---

### Type-Specific UIElement Extension Methods (Complete Reference)

The following extension methods are available for specific UI element types. All methods are in the `HS.Stride.Editor.Toolkit.Core.UIPageEditing` namespace.

#### Border Methods

```csharp
border.SetBorderColor(0.5f, 0.5f, 0.5f, 1.0f);           // Set border color (RGBA)
border.SetBorderThickness(2f, 2f, 2f, 2f);              // Set thickness per side
border.SetBorderThicknessUniform(2f);                   // Set uniform thickness
```

#### Slider Methods

```csharp
// Value and range
slider.SetMinimum(0f);                    // Set minimum value
slider.SetMaximum(100f);                  // Set maximum value
slider.SetValue(50f);                     // Set current value
slider.SetStep(1f);                       // Set step increment
slider.SetRange(0f, 100f);                // Set min and max together

// Getters
float val = slider.GetSliderValue();
float min = slider.GetSliderMinimum();
float max = slider.GetSliderMaximum();
var range = slider.GetSliderRange();      // Returns (Min, Max) tuple
float step = slider.GetSliderStep();

// Tick marks
slider.SetTickFrequency(10f);             // Interval between ticks
slider.SetSnapToTicks(true);              // Snap to tick positions
slider.SetAreTicksDisplayed(true);        // Show tick marks
float freq = slider.GetTickFrequency();
bool snaps = slider.GetSnapToTicks();
bool showTicks = slider.GetAreTicksDisplayed();

// Slider images (sprite sheet)
slider.SetTrackBackgroundImage(spriteSheet, frameIndex);
slider.SetTrackForegroundImage(spriteSheet, frameIndex);
slider.SetThumbImage(spriteSheet, frameIndex);
slider.SetMouseOverThumbImage(spriteSheet, frameIndex);

// Slider images (texture)
slider.SetTrackBackgroundTexture(textureAsset);
slider.SetTrackForegroundTexture(textureAsset);
slider.SetThumbTexture(textureAsset);
slider.SetMouseOverThumbTexture(textureAsset);
```

#### EditText Methods

```csharp
// Text properties
editText.SetMaxLength(100);               // Maximum characters
editText.SetIsReadOnly(false);            // Read-only mode
editText.SetMinLines(1);                  // Minimum visible lines
editText.SetMaxLines(5);                  // Maximum visible lines
int maxLen = editText.GetMaxLength();
bool readOnly = editText.GetIsReadOnly();
int minLines = editText.GetMinLines();
int maxLines = editText.GetMaxLines();

// Appearance
editText.SetCaretColor(1f, 1f, 1f, 1f);           // Cursor color
editText.SetSelectionColor(0.3f, 0.5f, 0.8f, 1f); // Selection highlight color
editText.SetCaretFrequency(1.0f);                 // Blink rate

// Images (sprite sheet)
editText.SetActiveImage(spriteSheet, frameIndex);     // Focused state
editText.SetInactiveImage(spriteSheet, frameIndex);   // Unfocused state

// Images (texture)
editText.SetActiveTexture(textureAsset);
editText.SetInactiveTexture(textureAsset);
```

#### ToggleButton Methods

```csharp
// State
toggle.SetIsChecked(true);
toggle.SetIsThreeState(false);            // Allow indeterminate state
bool isChecked = toggle.IsChecked();
bool threeState = toggle.GetIsThreeState();

// Images (sprite sheet)
toggle.SetCheckedImage(spriteSheet, frameIndex);
toggle.SetUncheckedImage(spriteSheet, frameIndex);
toggle.SetIndeterminateImage(spriteSheet, frameIndex);
toggle.SetToggleButtonImages(spriteSheet, checkedFrame, uncheckedFrame, indeterminateFrame);

// Images (texture)
toggle.SetCheckedTexture(textureAsset);
toggle.SetUncheckedTexture(textureAsset);
toggle.SetIndeterminateTexture(textureAsset);
toggle.SetToggleButtonTextures(checkedTex, uncheckedTex, indeterminateTex);
```

#### ScrollViewer Methods

```csharp
scrollViewer.SetScrollContent(contentElement);        // Set scrollable content
scrollViewer.SetScrollMode("Vertical");               // "None", "Horizontal", "Vertical", "Both"
scrollViewer.SetScrollBarColor(0.5f, 0.5f, 0.5f, 1f); // Scrollbar color
scrollViewer.SetScrollBarThickness(10f);              // Scrollbar width
scrollViewer.SetDeceleration(0.9f);                   // Scroll deceleration
scrollViewer.SetTouchScrollingEnabled(true);          // Enable touch scrolling
scrollViewer.SetSnapToAnchors(false);                 // Snap to anchor points
```

#### ScrollingText Methods

```csharp
scrollingText.SetScrollingSpeed(50f);              // Pixels per second
scrollingText.SetDesiredCharacterNumber(20);       // Visible character count
scrollingText.SetRepeatText(true);                 // Loop the text
```

#### ContentDecorator Methods

```csharp
// Background image (sprite sheet)
decorator.SetBackgroundImage(spriteSheet, frameIndex);

// Background image (texture)
decorator.SetBackgroundTexture(textureAsset);
```

#### ModalElement Methods

```csharp
modal.SetOverlayColor(0f, 0f, 0f, 0.7f);  // Semi-transparent overlay
modal.SetIsModal(true);                    // Enable modal behavior
```

#### ImageElement Methods

```csharp
image.SetStretchType("Uniform");           // "None", "Fill", "Uniform", "UniformToFill"
image.SetStretchDirection("Both");         // "UpOnly", "DownOnly", "Both"
```

#### TextBlock Methods

```csharp
textBlock.SetWrapText(true);              // Enable text wrapping
textBlock.SetTextAlignment("Center");      // "Left", "Center", "Right"
textBlock.SetOutlineColor(0f, 0f, 0f, 1f); // Text outline color
textBlock.SetOutlineThickness(2);          // Outline thickness in pixels
bool wraps = textBlock.GetWrapText();
```

#### Common Layout Methods (All Elements)

```csharp
// Visibility and interaction
element.SetVisibility(true);
element.SetCanBeHitByUser(true);          // Receive input events
element.SetClipToBounds(true);            // Clip children to bounds
bool visible = element.IsVisible();
bool hittable = element.GetCanBeHitByUser();
bool clips = element.GetClipToBounds();

// Draw order
element.SetDrawLayerNumber(1);            // Layer number for rendering
int layer = element.GetDrawLayerNumber();

// Size getters
float w = element.GetWidth();
float h = element.GetHeight();
var size = element.GetSize();             // Returns (Width, Height) tuple
var padding = element.GetPadding();       // Returns (Left, Top, Right, Bottom) tuple

// Alignment getters
string hAlign = element.GetHorizontalAlignment();
string vAlign = element.GetVerticalAlignment();
var align = element.GetAlignment();       // Returns (Horizontal, Vertical) tuple

// Color getters
var bgColor = element.GetBackgroundColor();   // Returns (R, G, B, A)? tuple
var textColor = element.GetTextColor();       // Returns (R, G, B, A)? tuple
var color = element.GetColor();               // Returns (R, G, B, A)? tuple
float opacity = element.GetOpacity();
```

---

## Particle System Creation (Programmatically)

**Namespace:** `HS.Stride.Editor.Toolkit.Core.Wrappers`

The toolkit provides a powerful API for programmatically creating and configuring particle effects (VFX) without needing the Game Studio editor. Create smoke, fire, explosions, sparks, magic effects, and any other particle-based visual effects entirely through code.

### Quick Start Example

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("GameScene");

// Create entity with particle system
var smokeEntity = scene.CreateEntity("Smoke");
var vfx = smokeEntity.AddParticleSystem();

// Configure particle system
vfx.Control = "Play";
vfx.Speed = 1.0f;

// Create a billboard emitter
var smoke = vfx.CreateBillboardEmitter("SmokeEmitter", (5.0f, 7.0f));

// Set material and texture
var smokeTexture = project.FindAsset("SMO001", AssetType.Texture);
smoke.SetTextureMaterial(smokeTexture, hdrMultiplier: 1.0f, alphaAdditive: 0.5f);

// Add flipbook animation
smoke.SetFlipbookAnimation(8, 8, 0, 64);

// Configure spawning
smoke.SetPerSecondSpawner(10.0f, looping: true);

// Add initializers
smoke.AddInitialSize((0.7f, 1.5f));
smoke.AddInitialPosition((-0.1f, 0.0f, -0.1f), (0.1f, 0.2f, 0.1f));
smoke.AddInitialVelocity((-0.1f, 0.5f, -0.1f), (0.1f, 0.7f, 0.1f));
smoke.AddInitialRotation((-360.0f, 360.0f));

// Add color fade over time
smoke.AddColorFade(
    (0.0f, 1.0f, 1.0f, 1.0f, 0.0f),   // Start: white, transparent
    (0.5f, 1.0f, 1.0f, 1.0f, 0.5f),   // Middle: white, semi-transparent
    (1.0f, 1.0f, 1.0f, 1.0f, 0.0f));  // End: white, transparent

// Add emitter to particle system
vfx.AddEmitter(smoke);

// Save
scene.Save();
```

### ParticleSystemWrapper Class

Represents a ParticleSystemComponent on an entity. This is the main container for all particle emitters.

#### Adding to an Entity

##### `entity.AddParticleSystem()`

Extension method to add a ParticleSystemComponent to an entity.

```csharp
var entity = scene.CreateEntity("VFX");
var vfx = entity.AddParticleSystem();
```

#### Particle System Properties

##### `string Control`

Playback control: `"Play"`, `"Pause"`, or `"Stop"`.

```csharp
vfx.Control = "Play";
```

##### `float ResetSeconds`

How long (in seconds) before the particle system loops or resets.

```csharp
vfx.ResetSeconds = 3.0f; // Loop every 3 seconds
```

##### `float Speed`

Time scale multiplier for the entire particle system. `1.0` is normal speed, `2.0` is double speed, `0.5` is half speed.

```csharp
vfx.Speed = 1.5f; // 1.5x speed
```

##### `SetColor(float r, float g, float b, float a)`

Sets the overall tint color for the particle system (multiplies with emitter colors).

```csharp
vfx.SetColor(1.0f, 0.5f, 0.5f, 1.0f); // Red tint
```

#### Managing Emitters

##### `void AddEmitter(VFXEmitter emitter)`

Adds an emitter to the particle system.

```csharp
var fire = vfx.CreateBillboardEmitter("fire", (1.0f, 2.0f));
// Configure fire emitter...
vfx.AddEmitter(fire);
```

##### `List<VFXEmitter> GetEmitters()`

Gets all emitters in this particle system.

```csharp
var emitters = vfx.GetEmitters();
foreach (var emitter in emitters)
{
    Console.WriteLine($"Emitter: {emitter.EmitterName}");
}
```

##### `bool RemoveEmitter(string emitterKey)`

Removes an emitter by its internal key. Returns true if removed successfully.

**Note**: Emitter keys are internal identifiers (GUIDs). This is a low-level method - typically you would rebuild particle systems rather than selectively removing emitters.

### VFXEmitter Class

Represents a single particle emitter within a particle system. Each emitter controls particle spawning, appearance, behavior, and lifetime.

#### Creating Emitters

##### `CreateBillboardEmitter(string? name = null, (float min, float max)? lifetime = null)`

Creates a billboard emitter (particles always face the camera).

**Parameters:**

- `name`: Optional emitter name (e.g., "fire", "smoke", "sparks") - defaults to null
- `lifetime`: Particle lifetime range in seconds (min, max) - defaults to (1.0, 1.0)

```csharp
var smoke = vfx.CreateBillboardEmitter("smoke", (5.0f, 7.0f));
```

##### `CreateOrientedQuadEmitter(string? name = null, (float min, float max)? lifetime = null, bool scaleLength = true, float lengthFactor = 1.0f)`

Creates an oriented quad emitter (particles oriented by velocity direction - good for sparks, trails).

**Parameters:**

- `name`: Optional emitter name - defaults to null
- `lifetime`: Particle lifetime range - defaults to (0.5, 1.5)
- `scaleLength`: Scale particle length based on velocity - default true
- `lengthFactor`: Length scaling factor - default 1.0

```csharp
var sparks = vfx.CreateOrientedQuadEmitter("sparks", (0.5f, 1.5f),
    scaleLength: true, lengthFactor: 0.05f);
```

##### `CreateRibbonEmitter(string? name = null, (float min, float max)? lifetime = null, int segments = 15, VFXSmoothingPolicy smoothingPolicy = VFXSmoothingPolicy.Best, int maxParticles = 50)`

Creates a ribbon/trail emitter (particles connect into a continuous trail).

**Parameters:**

- `name`: Optional emitter name
- `lifetime`: Particle lifetime range - defaults to (1.0, 1.0)
- `segments`: Number of segments in the ribbon - default 15
- `smoothingPolicy`: Smoothing quality (`VFXSmoothingPolicy.None`, `VFXSmoothingPolicy.Fast`, `VFXSmoothingPolicy.Best`) - default Best
- `maxParticles`: Maximum particles - default 50

```csharp
var trail = vfx.CreateRibbonEmitter("trail", (1.0f, 2.0f), segments: 20);
```

#### Emitter Properties

##### `string? EmitterName`

Optional name for this emitter.

```csharp
emitter.EmitterName = "MainFire";
```

##### `(float min, float max) ParticleLifetime`

How long particles live (in seconds) before disappearing.

```csharp
emitter.ParticleLifetime = (2.0f, 4.0f); // Live 2-4 seconds
```

##### `int? MaxParticlesOverride`

Maximum number of particles this emitter can have alive at once. If not set, uses default.

```csharp
emitter.MaxParticlesOverride = 100;
```

##### `int? DrawPriority`

Draw order priority. Higher values draw later (on top).

```csharp
emitter.DrawPriority = 10; // Draw on top
```

##### `string? SimulationSpace`

`"World"` or `"Local"`. Local space means particles move with the entity.

```csharp
emitter.SimulationSpace = "Local"; // Particles follow entity
```

##### `string? SortingPolicy`

How particles are sorted: `"ByOrder"` (for ribbons/trails), `"ByDepth"`, etc.

```csharp
emitter.SortingPolicy = "ByOrder"; // For ribbons
```

#### Material and Appearance

##### `SetTextureMaterial(AssetReference texture, float hdrMultiplier = 1.0f, float alphaAdditive = 0.0f)`

Sets the particle texture with HDR color multiplier and additive blending.

**Parameters:**

- `texture`: Texture asset reference
- `hdrMultiplier`: HDR brightness multiplier (values > 1.0 create glow)
- `alphaAdditive`: Additive blending amount (0.0 = normal, 1.0 = fully additive)

```csharp
var fireTexture = project.FindAsset("FIR001", AssetType.Texture);
emitter.SetTextureMaterial(fireTexture, hdrMultiplier: 10.0f, alphaAdditive: 0.8f);
```

##### `SetTextureMaterial(AssetReference texture, float hdrR, float hdrG, float hdrB, float alphaAdditive = 0.0f)`

Sets texture material with separate RGB HDR multipliers for color tinting.

```csharp
// Orange fire glow
emitter.SetTextureMaterial(fireTexture,
    hdrR: 50.0f, hdrG: 20.0f, hdrB: 10.0f, alphaAdditive: 0.8f);
```

##### `SetFlipbookAnimation(int xDivisions, int yDivisions, int startFrame, int animationSpeed)`

Enables flipbook/spritesheet animation for the texture.

**Parameters:**

- `xDivisions`: Number of columns in the spritesheet
- `yDivisions`: Number of rows in the spritesheet
- `startFrame`: Starting frame index (0-based)
- `animationSpeed`: Animation speed (frames per second)

```csharp
// 8x8 spritesheet, start at frame 0, play at 64 fps
emitter.SetFlipbookAnimation(8, 8, 0, 64);
```

#### Spawning Particles

##### `SetBurstSpawner(int particleCount, bool oneShot = true, (float min, float max)? delay = null)`

Spawns a burst of particles all at once.

**Parameters:**

- `particleCount`: Number of particles to spawn
- `oneShot`: If true, spawns once and stops; if false, repeats
- `delay`: Optional delay range before spawning (min, max) in seconds

```csharp
// Spawn 50 particles once, after 0.5 second delay
emitter.SetBurstSpawner(50, oneShot: true, delay: (0.5f, 0.5f));
```

##### `SetPerSecondSpawner(float particlesPerSecond, bool looping = true, (float min, float max)? delay = null, (float min, float max)? duration = null)`

Spawns particles continuously over time.

**Parameters:**

- `particlesPerSecond`: Spawn rate
- `looping`: If true, repeats; if false, runs once
- `delay`: Optional delay before starting
- `duration`: Optional duration to spawn for

```csharp
// Spawn 20 particles/second continuously
emitter.SetPerSecondSpawner(20.0f, looping: true);

// Spawn 500 particles/second for 0.1 seconds after 0.05 second delay
emitter.SetPerSecondSpawner(500.0f, looping: false,
    delay: (0.05f, 0.05f), duration: (0.1f, 0.1f));
```

##### `SetPerFrameSpawner(float particlesPerFrame, float framerate = 60.0f, bool looping = true)`

Spawns particles every frame (useful for trails).

```csharp
emitter.SetPerFrameSpawner(2.0f, framerate: 60.0f, looping: true);
```

#### Particle Initializers

Initializers set the initial properties of particles when they spawn.

##### `AddInitialSize((float min, float max) sizeRange, float? scaleUniform = null)`

Sets initial particle size range.

```csharp
emitter.AddInitialSize((0.5f, 1.5f)); // Random size between 0.5 and 1.5
emitter.AddInitialSize((1.0f, 1.0f), scaleUniform: 0.5f); // Size 1.0, scaled by 0.5
```

##### `AddInitialPosition((float x, float y, float z) min, (float x, float y, float z) max)`

Sets initial particle position offset range (local to emitter).

```csharp
// Spawn in a cube region
emitter.AddInitialPosition((-1.0f, 0.0f, -1.0f), (1.0f, 0.0f, 1.0f));
```

##### `AddInitialVelocity((float x, float y, float z) min, (float x, float y, float z) max)`

Sets initial particle velocity range.

```csharp
// Launch upward with some variation
emitter.AddInitialVelocity((-0.2f, 1.0f, -0.2f), (0.2f, 2.0f, 0.2f));
```

##### `AddInitialRotation((float min, float max) angleRange)`

Sets initial particle rotation angle range (in degrees).

```csharp
emitter.AddInitialRotation((-360.0f, 360.0f)); // Random rotation
```

##### `AddInitialColor((float r, float g, float b, float a) colorMin, (float r, float g, float b, float a) colorMax)`

Sets initial particle color range.

```csharp
// Orange to yellow
emitter.AddInitialColor(
    (1.0f, 0.5f, 0.0f, 1.0f),  // Orange
    (1.0f, 1.0f, 0.0f, 1.0f)); // Yellow
```

##### `AddSpawnOrder()`

Adds spawn order information (needed for ribbons/trails).

```csharp
emitter.AddSpawnOrder(); // Required for ribbon emitters
```

#### Particle Updaters

Updaters modify particle properties over their lifetime.

##### `AddColorFade(params (float time, float r, float g, float b, float a)[] keyframes)`

Animates particle color over lifetime. Time goes from 0.0 (birth) to 1.0 (death).

```csharp
emitter.AddColorFade(
    (0.0f, 1.0f, 1.0f, 1.0f, 0.0f),    // Start: white, transparent
    (0.2f, 1.0f, 0.8f, 0.5f, 0.5f),    // 20%: orange, semi-visible
    (0.5f, 1.0f, 0.5f, 0.0f, 1.0f),    // 50%: red-orange, fully visible
    (1.0f, 0.5f, 0.2f, 0.1f, 0.0f));   // End: dark, transparent
```

##### `AddSizeOverTime(params (float time, float size)[] keyframes)`

Animates particle size over lifetime.

```csharp
emitter.AddSizeOverTime(
    (0.0f, 0.5f),  // Start small
    (0.5f, 1.5f),  // Grow in middle
    (1.0f, 0.2f)); // Shrink at end
```

##### `AddForceField(VFXFieldShape fieldShape = VFXFieldShape.Sphere, (float x, float y, float z)? scale = null, float forceDirected = 0.0f, float forceVortex = 0.0f, float forceRepulsive = 0.0f, float energyConservation = 0.2f)`

Applies force field effects to particles.

**Parameters:**

- `fieldShape`: Shape of the force field (`VFXFieldShape.Cylinder`, `VFXFieldShape.Sphere`, etc.) - defaults to Sphere
- `scale`: Size of the force field volume - defaults to (1, 1, 1) if not specified
- `forceDirected`: Strength pulling toward field center (negative pushes away) - default 0.0
- `forceVortex`: Rotational/swirling force strength - default 0.0
- `forceRepulsive`: Outward explosive force (negative pulls inward) - default 0.0
- `energyConservation`: How much velocity is preserved (0.0 = full dampening, 1.0 = no dampening) - default 0.2

```csharp
// Upward swirling fire effect
emitter.AddForceField(
    fieldShape: VFXFieldShape.Cylinder,
    scale: (2.0f, 5.0f, 2.0f),
    forceDirected: 0.1f,
    forceVortex: 0.5f,
    forceRepulsive: -3.0f,
    energyConservation: 0.6f);
```

### Complete Examples

#### Fire Effect with Sparks

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");

var fireEntity = scene.CreateEntity("Fire");
var vfx = fireEntity.AddParticleSystem();
vfx.Control = "Play";

// Fire emitter
var fire = vfx.CreateBillboardEmitter("flames", (1.0f, 2.0f));
var fireTexture = project.FindAsset("FIR001", AssetType.Texture);
fire.SetTextureMaterial(fireTexture, hdrMultiplier: 10.0f, alphaAdditive: 0.8f);
fire.SetFlipbookAnimation(8, 8, 0, 64);
fire.SetPerSecondSpawner(20.0f);
fire.AddInitialSize((0.7f, 1.2f));
fire.AddInitialPosition((-0.35f, 0.0f, -0.35f), (0.35f, 0.5f, 0.35f));
fire.AddInitialVelocity((-0.2f, 0.5f, -0.2f), (0.2f, 1.0f, 0.2f));
fire.AddInitialRotation((-360.0f, 360.0f));
fire.AddInitialColor((1.0f, 0.62f, 0.2f, 1.0f), (1.0f, 0.56f, 0.2f, 1.0f));
fire.AddForceField(VFXFieldShape.Cylinder, (2.0f, 5.0f, 2.0f),
    forceDirected: 0.1f, forceVortex: 0.5f, forceRepulsive: -3.0f);
vfx.AddEmitter(fire);

// Sparks emitter
var sparks = vfx.CreateOrientedQuadEmitter("sparks", (0.5f, 1.5f));
var dotaTexture = project.FindAsset("dota", AssetType.Texture);
sparks.SetTextureMaterial(dotaTexture, hdrMultiplier: 50.0f);
sparks.SetPerSecondSpawner(50.0f);
sparks.AddInitialSize((0.1f, 0.25f), scaleUniform: 0.05f);
sparks.AddInitialVelocity((-0.1f, 0.5f, -0.1f), (0.1f, 1.0f, 0.1f));
sparks.AddInitialPosition((-0.6f, 0.0f, -0.6f), (0.6f, 1.0f, 0.6f));
sparks.AddInitialColor((1.0f, 0.07f, 0.0f, 1.0f), (1.0f, 0.19f, 0.0f, 1.0f));
vfx.AddEmitter(sparks);

scene.Save();
```

#### Explosion Effect

```csharp
var explosionEntity = scene.CreateEntity("Explosion");
var vfx = explosionEntity.AddParticleSystem();
vfx.ResetSeconds = 3.0f; // Reset every 3 seconds
vfx.Control = "Play";

// Explosion fire flash
var explosionFire = vfx.CreateBillboardEmitter("explosionfire", (1.0f, 1.3f));
var expTexture = project.FindAsset("EXP001", AssetType.Texture);
explosionFire.SetTextureMaterial(expTexture, hdrMultiplier: 50.0f);
explosionFire.SetFlipbookAnimation(8, 8, 8, 56);
explosionFire.SetBurstSpawner(10, oneShot: true);
explosionFire.AddInitialSize((1.0f, 2.0f));
explosionFire.AddInitialPosition((-0.2f, 0.0f, -0.2f), (0.2f, 0.0f, 0.2f));
explosionFire.AddInitialVelocity((-0.5f, -0.2f, -0.5f), (0.5f, 0.5f, 0.5f));
explosionFire.AddInitialRotation((-360.0f, 360.0f));
explosionFire.AddColorFade(
    (0.0f, 1.0f, 1.0f, 1.0f, 1.0f),
    (0.5f, 0.3f, 0.3f, 0.3f, 0.25f),
    (1.0f, 0.0f, 0.0f, 0.0f, 0.0f));
vfx.AddEmitter(explosionFire);

// Smoke plume
var smoke = vfx.CreateBillboardEmitter("smoke", (1.0f, 2.0f));
var smokeTexture = project.FindAsset("SMO001", AssetType.Texture);
smoke.SetTextureMaterial(smokeTexture, hdrMultiplier: 1.0f);
smoke.SetFlipbookAnimation(8, 8, 0, 64);
smoke.SetBurstSpawner(10, oneShot: true);
smoke.AddInitialVelocity((-0.4f, -0.4f, -0.4f), (0.4f, 0.4f, 0.4f));
smoke.AddInitialRotation((-360.0f, 360.0f));
smoke.AddInitialPosition((-0.3f, -0.3f, -0.3f), (0.3f, 0.3f, 0.3f));
smoke.AddColorFade(
    (0.0f, 0.5f, 0.25f, 0.1f, 1.0f),
    (0.5f, 0.5f, 0.2f, 0.1f, 0.7f),
    (1.0f, 0.1f, 0.1f, 0.1f, 0.0f));
vfx.AddEmitter(smoke);

// Sparks burst
var sparks = vfx.CreateBillboardEmitter("sparks", (0.7f, 1.4f));
var sparkTexture = project.FindAsset("dota", AssetType.Texture);
sparks.SetTextureMaterial(sparkTexture, hdrR: 50.0f, hdrG: 20.0f, hdrB: 20.0f);
sparks.SetBurstSpawner(50, oneShot: true, delay: (0.5f, 0.5f));
sparks.AddInitialVelocity((-0.5f, -0.5f, -0.5f), (0.5f, 0.5f, 0.5f));
sparks.AddInitialSize((0.1f, 0.2f), scaleUniform: 0.1f);
sparks.AddInitialPosition((-0.6f, -0.6f, -0.6f), (0.6f, 0.6f, 0.6f));
sparks.AddInitialColor((1.0f, 0.18f, 0.04f, 1.0f), (1.0f, 0.3f, 0.0f, 1.0f));
sparks.AddForceField(VFXFieldShape.Sphere, (2.0f, 2.0f, 2.0f),
    forceDirected: 0.3f, forceVortex: 0.5f, forceRepulsive: -0.3f);
vfx.AddEmitter(sparks);

scene.Save();
```

### Advanced: Custom Emitter Configuration

For advanced use cases, you can directly access and modify emitter properties:

```csharp
var emitter = new VFXEmitter
{
    EmitterName = "CustomEffect",
    ParticleLifetime = (2.0f, 3.0f),
    MaxParticlesOverride = 200,
    DrawPriority = 15,
    SimulationSpace = "Local",
    SortingPolicy = "ByDepth"
};

// Set custom shape
emitter.SetShapeBuilder(VFXShapeType.Billboard, new Dictionary<string, object>
{
    ["SamplerPosition"] = "null",
    ["SamplerSize"] = "null",
    ["SamplerRotation"] = "null"
});

// Add custom updater
emitter.AddUpdater("Gravity", new Dictionary<string, object>
{
    ["!UpdaterGravity"] = "",
    ["GravitationalAcceleration"] = new Dictionary<string, object>
    {
        ["X"] = 0.0f,
        ["Y"] = -9.8f,
        ["Z"] = 0.0f
    }
});

vfx.AddEmitter(emitter);
```

### Tips and Best Practices

1. **Texture Requirements**: Ensure textures exist in your project before referencing them. Use `project.FindAsset()` to safely locate textures.

2. **Performance**: Keep `MaxParticlesOverride` reasonable. Too many particles can impact performance.

3. **Lifetime vs Loop Time**: Set `ResetSeconds` on the ParticleSystem to control when effects loop, and `ParticleLifetime` on emitters to control individual particle duration.

4. **HDR Values**: Use HDR multipliers > 1.0 for glowing effects. Values of 10-50 create nice bloom/glow.

5. **Additive Blending**: Use `alphaAdditive` for fire, explosions, and glowing effects. Use 0.0 for solid smoke or fog.

6. **Color Curves**: Define at least 2 keyframes for color/size curves. More keyframes create smoother transitions.

7. **Force Fields**: Experiment with force field combinations:
   
   - Upward fire: negative repulsive force
   - Explosion: positive repulsive force with high energy conservation
   - Vortex: combine vortex force with directed force

8. **Testing**: Create effects in a test scene first, then copy to your game scenes once perfected.

---

## Entity & Component System

### Entity Class

**Namespace:** `HS.Stride.Editor.Toolkit.Core`

Represents a game object in a scene or prefab.

#### Properties

##### `string Id`

The entity's unique GUID.

##### `string Name`

The entity's display name.

##### `string? Folder`

The folder path this entity belongs to (e.g., "Enemies/Bosses").

##### `Dictionary<string, Component> Components`

Dictionary of all loaded components (key = component GUID without hyphens).

##### `PrefabData? ParentPrefab`

Prefab information if this entity is a prefab instance.

#### Methods

##### `bool HasComponent(string componentType)`

Checks if the entity has a specific component.

**Parameters:**

- `componentType` (string) - Component type (can be partial match)

**Returns:** `true` if component exists

**Example:**

```csharp
if (entity.HasComponent("ModelComponent"))
{
    // Entity has a model
}

// Works with custom components
if (entity.HasComponent("CharacterController"))
{
    var controller = entity.GetComponent("CharacterController");
}
```

##### `Component? GetComponent(string componentType)`

Gets a component by type. Uses **lazy loading** - components are loaded from raw YAML only when accessed.

**Parameters:**

- `componentType` (string) - Component type (can be partial match)

**Returns:** Component or null if not found

**Example:**

```csharp
var transform = entity.GetComponent("TransformComponent");
var customComp = entity.GetComponent("MyGame.InventoryComponent");
```

##### Typed Wrapper Methods

These are shortcuts for common built-in components that return typed wrappers:

- `TransformWrapper? GetTransform()` - Returns TransformWrapper with typed properties
- `ModelWrapper? GetModel()` - Returns ModelWrapper with model/material methods
- `StaticColliderWrapper? GetStaticCollider()` - Returns StaticColliderWrapper with shape methods
- `RigidbodyWrapper? GetRigidbody()` - Returns RigidbodyWrapper with physics properties
- `LightWrapper? GetLight()` - Returns LightWrapper with light properties
- `ParticleSystemWrapper? GetParticleSystem()` - Returns ParticleSystemWrapper for VFX
- `void RemoveParticleSystem()` - Removes the particle system component from the entity

**Example:**

```csharp
var transform = entity.GetTransform();
transform.SetPosition(10, 5, 0);

var model = entity.GetModel();
var collider = entity.GetStaticCollider();
```

##### Component Adding Methods

Add components directly on the entity with typed wrappers:

- `Component AddComponent(string componentType)` - Adds any component (returns generic Component)
- `StaticColliderWrapper AddStaticCollider()` - Adds static collider (returns wrapper)
- `RigidbodyWrapper AddRigidbody(float mass = 1.0f, bool isKinematic = false)` - Adds rigidbody (returns wrapper)
- `ModelWrapper AddModel()` - Adds a model component (returns wrapper)
- `LightWrapper AddLight()` - Adds a light component (returns wrapper)
- `ParticleSystemWrapper AddParticleSystem()` - Adds a particle system component (returns wrapper)

**Example:**

```csharp
// Add with typed wrapper
var collider = entity.AddStaticCollider();
collider.AddBoxShape(2.0f, 1.0f, 2.0f);
collider.Friction = 0.8f;

// Add rigidbody
var rb = entity.AddRigidbody(mass: 10.0f, isKinematic: false);
rb.AddSphereShape(0.5f);

// Add custom component
var custom = entity.AddComponent("MyGame.HealthComponent");
custom.Set("MaxHealth", 100.0f);
```

##### Component Removal Method

- `void RemoveComponent(string componentType)` - Removes a component from the entity

**Example:**

```csharp
entity.RemoveComponent("StaticColliderComponent");
```

##### Component Cloning Method

- `Component CloneComponent(Component sourceComponent)` - Clones a component from another entity, copying all properties with new GUIDs

**Example:**

```csharp
var healthComponent = sourceEntity.GetComponent("HealthComponent");
targetEntity.CloneComponent(healthComponent);
```

##### Hierarchy Navigation Methods

Navigate entity parent-child relationships in the transform hierarchy.

###### `List<Entity> GetChildren()`

Gets all direct child entities from the Transform.Children property.

**Returns:** List of child entities (empty if no children)

**Example:**

```csharp
var parent = scene.FindEntityByName("House");
var children = parent.GetChildren();

foreach (var child in children)
{
    Console.WriteLine($"Child: {child.Name}");
}
```

###### `Entity? FindChildByName(string childName)`

Finds a direct child entity by exact name match.

**Parameters:**

- `childName` (string) - Exact name of the child entity

**Returns:** Child entity or null if not found

**Example:**

```csharp
var house = scene.FindEntityByName("House");
var door = house.FindChildByName("Door");

if (door != null)
{
    door.GetTransform().SetPosition(5, 0, 0);
}
```

###### `List<Entity> FindChildrenByName(string pattern)`

Finds child entities by name pattern (supports `*` and `?` wildcards).

**Parameters:**

- `pattern` (string) - Pattern with wildcards

**Returns:** List of matching child entities

**Example:**

```csharp
var house = scene.FindEntityByName("House");

// Find all windows
var windows = house.FindChildrenByName("Window*");

foreach (var window in windows)
{
    window.AddStaticCollider().AddBoxShape(1, 2, 0.1f);
}
```

###### `Entity? GetParent()`

Gets the parent entity (if this entity is a child of another).

**Returns:** Parent entity or null if this is a root entity

**Example:**

```csharp
var door = scene.FindEntityByName("Door");
var parent = door.GetParent();

if (parent != null)
{
    Console.WriteLine($"Door's parent is: {parent.Name}");
}
```

###### `List<Entity> GetDescendants()`

Gets all descendant entities recursively (children, grandchildren, etc.).

**Returns:** List of all descendants

**Example:**

```csharp
var house = scene.FindEntityByName("House");

// Get all descendants (children, grandchildren, etc.)
var allDescendants = house.GetDescendants();

Console.WriteLine($"House has {allDescendants.Count} total descendants");

// Find all models in the house hierarchy
var modelsInHouse = allDescendants.Where(e => e.HasComponent("ModelComponent")).ToList();
```

###### `bool HasChildren()`

Checks if this entity has any child entities.

**Returns:** `true` if entity has children

**Example:**

```csharp
if (entity.HasChildren())
{
    Console.WriteLine($"{entity.Name} is a parent");
}
```

**Hierarchy Navigation Use Cases:**

```csharp
// Example: Find all colliders in a prefab instance
var prefabInstance = scene.FindEntityByName("EnemyGroup");
var allDescendants = prefabInstance.GetDescendants();
var collidersInPrefab = allDescendants
    .Where(e => e.HasComponent("StaticColliderComponent"))
    .ToList();

// Example: Modify all children
var parent = scene.FindEntityByName("LightGroup");
foreach (var child in parent.GetChildren())
{
    var light = child.GetLight();
    if (light != null)
    {
        light.Intensity = 5.0f;
    }
}

// Example: Navigate up the parent chain
var leaf = scene.FindEntityByName("DeepChild");
var current = leaf;
while (current != null)
{
    Console.WriteLine($"Parent: {current.Name}");
    current = current.GetParent();
}
```

### Component Class

**Namespace:** `HS.Stride.Editor.Toolkit.Core`

Represents a component with its type, ID, and property data.

#### Properties

##### `string Key`

The component's key (GUID without hyphens) used in the entity's Components dictionary.

##### `string Type`

The component's type name (e.g., "TransformComponent", "MyNamespace.CustomComponent").

##### `string Id`

The component's unique GUID.

##### `Dictionary<string, object> Properties`

The component's property data.

##### `string RawContent`

The raw YAML content for this component (for advanced scenarios).

#### Methods

##### `T? Get<T>(string propertyName)`

Gets a property value with automatic type conversion.

**Type Parameter:**

- `T` - The expected return type (float, bool, string, etc.)

**Parameters:**

- `propertyName` (string) - Property name using dot notation for nested properties

**Returns:** Value of type T, or default(T) if not found or conversion fails

**NOTE:** Only properties saved in the .sdscene file (visible in Stride's Property Grid) can be accessed. Internal script variables that aren't serialized by Stride won't be available.

**Example:**

```csharp
var component = entity.GetComponent("CharacterController");

// Simple properties
var speed = component.Get<float>("MoveSpeed");
var isGrounded = component.Get<bool>("IsGrounded");
var name = component.Get<string>("CharacterName");

// Nested properties
var posX = component.Get<float>("Position.X");

// Getting a List property (returns as Dictionary<string, object> by default)
var myItems = component.Get<Dictionary<string, object>>("Inventory.Items");
if (myItems != null)
{
    foreach (var itemEntry in myItems.Values)
    {
        if (itemEntry is Dictionary<string, object> item)
        {
            Console.WriteLine($"Item: {item["Name"]}, Quantity: {item["Quantity"]}");
        }
    }
}
```

##### `void Set(string propertyName, object value)`

Sets a property value, creating nested dictionaries as needed.

**Parameters:**

- `propertyName` (string) - Property name using dot notation
- `value` (object) - Value to set

**Throws:**

- `InvalidOperationException` - In Strict mode (default), throws if property doesn't exist in the component's C# script

**NOTE:** Only properties that Stride serializes (visible in Stride's Property Grid) will persist when saved. Setting internal script variables that aren't serialized by Stride won't have any effect. In Strict mode, the toolkit validates properties exist before allowing you to set them, helping catch typos early.

**Example:**

```csharp
var component = entity.GetComponent("CharacterController");

// Simple properties
component.Set("MoveSpeed", 5.5f);
component.Set("JumpHeight", 2.0f);

// Nested properties
component.Set("Stats.Strength", 10);
component.Set("Stats.Agility", 15);

// Setting a List or Dictionary property requires recreating the underlying dictionary structure
// For more convenient methods to modify collections, see "Working with Collections" further below.
component.Set("Inventory.Capacity", 20); // Setting a simple property in a nested structure

// Example of setting an entire list/array (e.g., of strings)
// Note: Stride serializes collections as GUID-keyed dictionaries in YAML.
// For simpler modification of lists/dictionaries, use the helper methods like AddToList(), SetDictionary(), SetList().
component.Set("AllowedWeapons", new Dictionary<string, object>
{
    { Guid.NewGuid().ToString("N"), "Sword" },
    { Guid.NewGuid().ToString("N"), "Axe" }
});
```

##### `Dictionary<string, object>? GetMultiValueProperty(string propertyName)`

Gets a property that contains multiple fields (e.g., Vector3 with X,Y,Z or Color with R,G,B,A).

**Parameters:**

- `propertyName` (string) - Property name (not a path)

**Returns:** Dictionary or null if not found or not a multi-field type

**Example:**

```csharp
var positionDict = component.GetMultiValueProperty("Position");
if (positionDict != null)
{
    var x = Convert.ToSingle(positionDict["X"]);
    var y = Convert.ToSingle(positionDict["Y"]);
    var z = Convert.ToSingle(positionDict["Z"]);
}
```

##### `void SetMultiValueProperty(string propertyName, Dictionary<string, object> value)`

Sets a property that contains multiple fields (e.g., Vector3 with X,Y,Z or Color with R,G,B,A).

**Parameters:**

- `propertyName` (string) - Property name
- `value` (Dictionary<string, object>) - Multi-value property data

##### Getting Entity References

To read entity reference properties, use `Get<EntityRefData>()`:

```csharp
// Get an entity reference from a component
var targetRef = aiComponent.Get<EntityRefData>("Target");
if (targetRef != null)
{
    // The EntityRefData contains the reference string
    Console.WriteLine($"Target reference: {targetRef}");
}
```

##### `void SetEntityRef(string propertyName, Entity entity)`

Sets an entity reference property.

```csharp
var player = scene.FindEntityByName("Player");
aiComponent.SetEntityRef("Target", player);
```

##### Getting Asset References

To read asset reference properties, use `Get<AssetRefData>()`:

```csharp
// Get an asset reference from a component
var prefabRef = spawner.Get<AssetRefData>("EnemyPrefab");
if (prefabRef != null)
{
    // The AssetRefData contains the GUID:Path reference string
    Console.WriteLine($"Prefab reference: {prefabRef}");
}
```

**Note on Stride "URL-like" Asset References:** In Stride, assets are often referenced internally by a `GUID:Path` string (e.g., `"a1b2c3d4-e5f6-7890-abcd-ef1234567890:Textures/MyTexture"`).

##### `void SetAssetRef(string propertyName, AssetReference asset)`

Sets an asset reference property (Prefab, Texture, Model, RawAsset, etc.).
**Note on Stride "URL-like" Asset References:** This method automatically converts the provided `AssetReference` into Stride's standard `GUID:Path` string format (e.g., `"a1b2c3d4-e5f6-7890-abcd-ef1234567890:Textures/MyTexture"`) and sets it as the property value. This is how components link to assets programmatically.

```csharp
var prefab = project.FindAsset("Enemy", AssetType.Prefab);
spawner.SetAssetRef("EnemyPrefab", prefab);
```

##### `void AddToList(string propertyName, object value)`

Adds an item to a property that is a List or an array.

```csharp
var enemy1 = scene.FindEntityByName("Enemy1");
var enemy2 = scene.FindEntityByName("Enemy2");
spawnerScript.AddToList("SpawnTargets", enemy1); // Use Entity object directly
spawnerScript.AddToList("SpawnTargets", enemy2); // Use Entity object directly
```

##### `void SetDictionary(string propertyName, object key, object value)`

Sets a key-value pair in a property that is a Dictionary.

```csharp
var walkAnim = project.FindAsset("Walk", AssetType.Animation);
animator.SetDictionary("AnimationClips", "Walk", walkAnim.Reference);
```

##### `void SetList(string propertyName, IEnumerable<object> values)`

Replaces the entire content of a List or array property.

```csharp
var entity1 = scene.FindEntityByName("Point1");
var entity2 = scene.FindEntityByName("Point2");
spawner.SetList("SpawnPoints", new[] { $"ref!! {entity1.Id}", $"ref!! {entity2.Id}" });
```

---

### Working with Collections (Lists, Arrays, Dictionaries)

For more detailed examples and helper methods like `AddToList()`, `SetDictionary()`, and `SetList()` for modifying component properties that are collections, please refer to the "Writing Custom Component Properties" section under "Working with Custom Components" below.

---

## Working with Custom Components (CRITICAL)

This section is **essential** for anyone working with custom Stride components created in C#.

### Component Interaction Strategies

Working with components fundamentally involves getting and setting property values. The toolkit provides different **interaction strategies** that offer varying levels of convenience, type-safety, and code readability over this core `Get()`/`Set()` functionality.

| Strategy                    | Description                                                          | Primary Use Case                                                      | Benefits                                                                | Cautions                                                          |
|:--------------------------- |:-------------------------------------------------------------------- |:--------------------------------------------------------------------- |:----------------------------------------------------------------------- |:----------------------------------------------------------------- |
| **Direct Component Access** | Using `Component.Get<T>()` and `Component.Set()` directly.           | Custom components, quick edits, dynamic property manipulation.        | Flexible, handles any component, no wrapper code needed.                | Verbose syntax, no compile-time type checking for property names. |
| **Custom Wrapper Classes**  | Creating a dedicated C# class to abstract `Component.Get()`/`Set()`. | Your own frequently used custom components.                           | Type-safe, IntelliSense, reusable, cleaner code, custom helper methods. | Requires writing and maintaining wrapper class definitions.       |
| **Built-in Typed Wrappers** | Toolkit-provided wrappers for core Stride components.                | Core Stride components (Transform, Model, Light, Colliders, Physics). | Highly convenient, type-safe, IntelliSense, specialized helper methods. | Limited to a predefined set of core Stride components.            |

### 1. Direct Component Access (The Foundation)

This strategy involves directly using the `Component` class's generic `Get<T>()` and flexible `Set()` methods. It's the **foundational** way to interact with any component and is particularly effective for custom (C#) components. The toolkit leverages **automatic script scanning** to understand your component's properties, which then allows for validation in `Strict` mode and safe manipulation.

This is the primary method for interacting with custom (C#) components. The toolkit leverages **automatic script scanning** to understand your component's properties, which then allows for validation in `Strict` mode and safe manipulation.

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");
var player = scene.FindEntityByName("Player");

// Add custom component by class name. The toolkit automatically scans your C# scripts
// to resolve the full type and initialize properties with Stride-compatible defaults.
var health = player.AddComponent("HealthComponent");

// Set properties directly using the flexible Set() method.
// - In ProjectMode.Strict (default): This method validates that "MaxHealth" exists
//   in your C# script's public properties and checks for type compatibility.
health.Set("MaxHealth", 100.0f);
health.Set("CurrentHealth", 100.0f);
health.Set("Regeneration", 5.0f);

// Read properties using the generic Get<T>() method.
var currentHP = health.Get<float>("CurrentHealth");
var maxHP = health.Get<float>("MaxHealth");

Console.WriteLine($"Player HP: {currentHP}/{maxHP}");

scene.Save();
```

**Understanding `ProjectMode.Strict` vs. `ProjectMode.Loose`:**

The behavior of `Component.Set()` in Direct Component Access depends on the `ProjectMode` of your `StrideProject` instance:

* **`ProjectMode.Strict` (Default):**
  * **Validation:** The toolkit uses script scanning to ensure the property you're trying to `Set()` actually exists as a public property in your component's C# script and performs basic type checking.
  * **Safety:** This helps catch typos and ensures you are only modifying properties that Stride will serialize and recognize. An `InvalidOperationException` is thrown if validation fails.
* **`ProjectMode.Loose`:**
  * **Flexibility:** Allows you to `Set()` any property name. No validation is performed against your C# script.
  * **Caution:** Invalid property names (those not defined in your C# script or not serialized by Stride) will simply be ignored by Stride at runtime. Use this mode with caution for dynamic or advanced scenarios where you explicitly know what you're doing.

**When to use Direct Component Access:**

- For quick scripts and one-off modifications.
- When working with simple custom components where creating a full wrapper class is overkill.
- Prototyping and testing.

### 2. Custom Wrapper Classes (Enhancing Custom Components)

For components you use frequently, create your own wrapper class (just like the built-in wrappers):

```csharp
// Define your wrapper class
public class HealthWrapper
{
    public Component Component { get; private set; }

    public HealthWrapper(Component component)
    {
        Component = component;
    }

    // Typed properties
    public float MaxHealth
    {
        get => Component.Get<float?>("MaxHealth") ?? 100.0f;
        set => Component.Set("MaxHealth", value);
    }

    public float CurrentHealth
    {
        get => Component.Get<float?>("CurrentHealth") ?? 100.0f;
        set => Component.Set("CurrentHealth", value);
    }

    public float Regeneration
    {
        get => Component.Get<float?>("Regeneration") ?? 0.0f;
        set => Component.Set("Regeneration", value);
    }

    // Helper methods
    public void Heal(float amount)
    {
        CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
    }

    public void Damage(float amount)
    {
        CurrentHealth = Math.Max(CurrentHealth - amount, 0);
    }

    public bool IsDead() => CurrentHealth <= 0;
}

// Extension method for convenience (optional)
public static class HealthEntityExtensions
{
    public static HealthWrapper? GetHealth(this Entity entity)
    {
        var component = entity.GetComponent("MyGame.HealthComponent");
        return component != null ? new HealthWrapper(component) : null;
    }

    public static HealthWrapper AddHealth(this Entity entity, float maxHealth = 100.0f)
    {
        var component = entity.AddComponent("MyGame.HealthComponent");
        var wrapper = new HealthWrapper(component);
        wrapper.MaxHealth = maxHealth;
        wrapper.CurrentHealth = maxHealth;
        return wrapper;
    }
}
```

**Using your custom wrapper:**

```csharp
var scene = Scene.Load("Level1.sdscene");
var player = scene.FindEntityByName("Player");

// Add with wrapper
var health = player.AddHealth(maxHealth: 150.0f);
health.Regeneration = 5.0f;

// Or get existing
var existingHealth = player.GetHealth();
if (existingHealth != null)
{
    existingHealth.Damage(25.0f);
    Console.WriteLine($"HP: {existingHealth.CurrentHealth}/{existingHealth.MaxHealth}");

    if (existingHealth.IsDead())
    {
        Console.WriteLine("Player died!");
    }
}

scene.Save();
```

**When to use:**

- Components you use frequently across multiple scripts
- Complex components with many properties
- When you want helper methods and validation
- Team projects where IntelliSense helps other developers

### 3. Built-in Typed Wrappers (Simplifying Core Stride Components)

For common, built-in Stride components (like `TransformComponent`, `ModelComponent`, `LightComponent`, `StaticColliderComponent`, `RigidbodyComponent`), the toolkit provides **typed wrapper classes**. These wrappers offer highly convenient, type-safe access with IntelliSense and often include helper methods for common operations.

**Examples of Built-in Wrappers:**

* `TransformWrapper` (accessed via `entity.GetTransform()`)
* `ModelWrapper` (accessed via `entity.GetModel()`)
* `StaticColliderWrapper` (accessed via `entity.GetStaticCollider()`)
* `RigidbodyWrapper` (accessed via `entity.GetRigidbody()`)
* `LightWrapper` (accessed via `entity.GetLight()`)

You can find more details in the [Typed Component Wrappers](#typed-component-wrappers) section.

**When to use Built-in Typed Wrappers:**

- Always, when working with the core Stride components they are designed for, as they simplify common tasks and provide type safety.

---

### Comparison: The Same Operation Three Ways

To illustrate the different approaches, here's how you might set a property:

**Built-in Wrapper (Transform - Most Convenient):**

```csharp
// Use GetTransform() to get the TransformWrapper
var transform = entity.GetTransform();
transform.SetPosition(10, 5, 0); // Type-safe, specific method
```

**Custom Wrapper (Your Own Health Component - Type-safe & Reusable):**

```csharp
// Use your custom extension method to get/add your HealthWrapper
var health = entity.AddHealth(maxHealth: 100.0f);
health.Heal(50.0f); // Type-safe, custom logic
```

**Direct Component Access (Health Component - Flexible but Verbose):**

```csharp
// Use AddComponent() to get the generic Component object
var healthComponent = entity.AddComponent("MyGame.HealthComponent");
healthComponent.Set("MaxHealth", 100.0f); // Requires string property name
healthComponent.Set("CurrentHealth", 100.0f);
```

---

### Understanding Custom Components

Custom components are C# classes you create in your Stride project:

```csharp
// In your Stride project - WITH namespace
namespace MyGame.Components
{
    public class CharacterController : SyncScript
    {
        public float MoveSpeed { get; set; } = 5.0f;
        public float JumpHeight { get; set; } = 2.0f;
        public bool IsGrounded { get; set; }
        public string CharacterName { get; set; } = "Hero";
    }
}

// In your Stride project - WITHOUT namespace (note the leading dot in YAML)
public class SimpleAI : SyncScript
{
    public float DetectionRadius { get; set; } = 10.0f;
    public Entity Target { get; set; }
}
```

**When saved in a scene, they appear in YAML as:**

```yaml
Components:
    # With namespace: !Namespace.ClassName,AssemblyName
    abc123: !MyGame.Components.CharacterController,MyGame
        Id: guid-here
        MoveSpeed: 5.0
        JumpHeight: 2.0
        IsGrounded: true
        CharacterName: Hero

    # Without namespace: !.ClassName,AssemblyName (note leading dot)
    def456: !.SimpleAI,MyGame
        Id: guid-here
        DetectionRadius: 10.0
        Target: null
```

**Script Scanning Details:**

- ScriptScanner automatically detects namespace (or lack thereof)
- Assembly name extracted from nearest `.csproj` file
- Public fields and properties are extracted with their types
- Default values match Stride's serialization format

### Reading Custom Component Properties

#### Basic Example

```csharp
var scene = Scene.Load("MainScene.sdscene");

// Find entity with custom component
var player = scene.FindEntitiesWithComponent("CharacterController").First();

// Get the component
var controller = player.GetComponent("CharacterController");

// Read properties with type conversion
float moveSpeed = controller.Get<float>("MoveSpeed");
float jumpHeight = controller.Get<float>("JumpHeight");
bool isGrounded = controller.Get<bool>("IsGrounded");
string charName = controller.Get<string>("CharacterName");

Console.WriteLine($"Character: {charName}, Speed: {moveSpeed}");
```

#### Real-World Example: BackgroundComponent

From the actual test suite:

```csharp
// Custom Stride component with texture reference
var scene = Scene.Load("MainScene.sdscene");

// Find entity with BackgroundComponent
var bgEntities = scene.FindEntitiesWithComponent("BackgroundComponent");
var bgEntity = bgEntities.FirstOrDefault();

if (bgEntity != null)
{
    var bgComponent = bgEntity.GetComponent("BackgroundComponent");

    // BackgroundComponent has a Texture property (asset reference)
    var textureRef = bgComponent.Get<string>("Texture");
    // Returns: "2a0193bf-fcf1-42b4-92d8-dbde51d42cad:Textures/Skybox texture"

    Console.WriteLine($"Background uses texture: {textureRef}");
}
```

#### Real-World Example: CharacterComponent (Physics)

From the actual test suite:

```csharp
var scene = Scene.Load("MainScene.sdscene");

// CharacterComponent is a Stride physics component with many properties
var characters = scene.FindEntitiesWithComponent("CharacterComponent");
var playerEntity = characters.FirstOrDefault();

if (playerEntity != null)
{
    var charComp = playerEntity.GetComponent("CharacterComponent");

    // Read various property types
    float fallSpeed = charComp.Get<float>("FallSpeed");          // 10.0
    float friction = charComp.Get<float>("Friction");            // 0.5
    bool canSleep = charComp.Get<bool>("CanSleep");              // false
    string collisionGroup = charComp.Get<string>("CollisionGroup"); // "CharacterFilter"

    Console.WriteLine($"Character fall speed: {fallSpeed}");
    Console.WriteLine($"Collision group: {collisionGroup}");
}
```

### Writing Custom Component Properties

#### Basic Example

```csharp
var scene = Scene.Load("MainScene.sdscene");
var player = scene.FindEntityByName("Player");
var controller = player.GetComponent("CharacterController");

// Modify primitive properties
controller.Set("MoveSpeed", 7.5f);
controller.Set("JumpHeight", 3.0f);
controller.Set("CharacterName", "SuperHero");

// Save changes
scene.Save();
```

#### Setting Entity References

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");

var enemy = scene.FindEntityByName("Enemy");
var player = scene.FindEntityByName("Player");
var aiComponent = enemy.GetComponent("AIController");

// Set entity reference using helper method
aiComponent.SetEntityRef("Target", player);

scene.Save();
```

#### Setting Asset References

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");
var entity = scene.FindEntityByName("Spawner");
var spawner = entity.GetComponent("SpawnerComponent");

// Find assets
var enemyPrefab = project.FindAsset("EnemyPrefab", AssetType.Prefab);
var explosionModel = project.FindAsset("Explosion", AssetType.Model);
var database = project.FindAsset("SpawnData", AssetType.RawAsset);

// Set asset references using helper method
spawner.SetAssetRef("EnemyPrefab", enemyPrefab);
spawner.SetAssetRef("ExplosionModel", explosionModel);
spawner.SetAssetRef("DatabaseRef", database);  // RawAsset/UrlReference

scene.Save();
```

#### Reading References

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");
var enemy = scene.FindEntityByName("Enemy");
var aiComponent = enemy.GetComponent("AIController");

// Read entity reference using Get<EntityRefData>()
var targetRef = aiComponent.Get<EntityRefData>("Target");
if (targetRef != null)
{
    Console.WriteLine($"AI target reference: {targetRef}");
}

// Read asset reference using Get<AssetRefData>()
var prefabRef = aiComponent.Get<AssetRefData>("SpawnPrefab");
if (prefabRef != null)
{
    Console.WriteLine($"Will spawn prefab: {prefabRef}");
}
```

**Reference Data Types:**

- `EntityRefData` - For entity references (format: `ref!! guid`)
- `AssetRefData` - For asset references, representing Stride's "URL-like" `GUID:Path` string format.
  - Used for linking to assets like Prefabs, Models, Materials, Textures, RawAssets (which themselves can point to external content), etc. The `AssetReference.Reference` property provides this `guid:path` string.

Both types have a `Resolve()` method to get the actual object.

#### Working with Collections (Lists, Arrays, Dictionaries)

Stride serializes collections as GUID-keyed dictionaries in YAML format. The toolkit provides clean helper methods for working with these collections.

**Adding to Lists/Arrays:**

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");
var spawner = scene.FindEntityByName("EnemySpawner");
var spawnerScript = spawner.GetComponent("SpawnerScript");

// Add entities to a List<Entity>
var enemy1 = scene.FindEntityByName("Enemy1");
var enemy2 = scene.FindEntityByName("Enemy2");
spawnerScript.AddToList("SpawnTargets", enemy1); // Use Entity object directly
spawnerScript.AddToList("SpawnTargets", enemy2); // Use Entity object directly

// Add assets to a List<Prefab>
var prefab1 = project.FindAsset("ZombiePrefab", AssetType.Prefab);
var prefab2 = project.FindAsset("SkeletonPrefab", AssetType.Prefab);
spawnerScript.AddToList("PrefabList", prefab1); // Use AssetReference object directly
spawnerScript.AddToList("PrefabList", prefab2); // Use AssetReference object directly

scene.Save();
```

**Setting Dictionary Values:**

```csharp
var animator = entity.GetComponent("AnimatorScript");

// Dictionary<string, AnimationClip>
var walkAnim = project.FindAsset("Walk", AssetType.Animation);
var runAnim = project.FindAsset("Run", AssetType.Animation);
animator.SetDictionary("AnimationClips", "Walk", walkAnim.Reference);
animator.SetDictionary("AnimationClips", "Run", runAnim.Reference);

// Dictionary<int, string> (primitive types)
animator.SetDictionary("StateNames", 0, "Idle");
animator.SetDictionary("StateNames", 1, "Walking");
animator.SetDictionary("StateNames", 2, "Running");

scene.Save();
```

**Replacing Entire Lists:**

```csharp
var spawner = entity.GetComponent("SpawnerScript");

// Replace entire list with new values
var entity1 = scene.FindEntityByName("Point1");
var entity2 = scene.FindEntityByName("Point2");
var entity3 = scene.FindEntityByName("Point3");

spawner.SetList("SpawnPoints", new[]
{
    entity1, // Use Entity object directly
    entity2, // Use Entity object directly
    entity3  // Use Entity object directly
});

scene.Save();
```

**Collection Helper Methods:**

- `AddToList(propertyName, value)` - Add item to list/array
- `SetDictionary(propertyName, key, value)` - Set dictionary entry
- `SetList(propertyName, values)` - Replace entire list

**Important Notes:**

- Collections initialized as `"null"` are automatically converted to empty dictionaries
- Each item gets a unique GUID key in the YAML format
- Dictionary keys use format: `guid~key` (e.g., `"abc123~Walk"`)
- Lists use format: `guid: value`
- All changes persist when you call `Save()`

#### Batch Modification Example

```csharp
var scene = Scene.Load("MainScene.sdscene");

// Find all entities with health components
var healthEntities = scene.FindEntitiesWithComponent("HealthComponent");

// Buff all entities
foreach (var entity in healthEntities)
{
    var health = entity.GetComponent("HealthComponent");

    var currentMax = health.Get<float>("MaxHealth");
    var newMax = currentMax * 1.5f; // 50% increase

    health.Set("MaxHealth", newMax);
    health.Set("CurrentHealth", newMax);

    Console.WriteLine($"Buffed {entity.Name}: {currentMax} -> {newMax} HP");
}

scene.Save();
```

### Complex Custom Components

#### Nested Properties

Some custom components have nested structures:

```csharp
// Custom component with nested data
public class InventoryComponent : SyncScript
{
    public class InventorySlot
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public Dictionary<int, InventorySlot> Slots { get; set; }
    public int MaxSlots { get; set; } = 20;
}
```

**Accessing nested properties:**

```csharp
var inventory = entity.GetComponent("InventoryComponent");

// Access top-level property
int maxSlots = inventory.Get<int>("MaxSlots");

// Access nested dictionary
var slotsDict = inventory.GetDict("Slots");
if (slotsDict != null)
{
    foreach (var kvp in slotsDict)
    {
        var slotData = kvp.Value as Dictionary<string, object>;
        if (slotData != null)
        {
            var itemId = slotData["ItemId"] as string;
            var quantity = Convert.ToInt32(slotData["Quantity"]);
            Console.WriteLine($"Slot {kvp.Key}: {quantity}x {itemId}");
        }
    }
}
```

#### Asset References in Custom Components

Custom components can reference other assets:

```csharp
// Custom component referencing a prefab
public class SpawnerComponent : SyncScript
{
    public Entity PrefabToSpawn { get; set; }
    public float SpawnInterval { get; set; } = 5.0f;
    public int MaxSpawns { get; set; } = 10;
}
```

**Modifying asset references:**

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");
var entity = scene.FindEntityByName("Spawner");

var spawner = entity.GetComponent("SpawnerComponent");

// Find a new prefab to spawn
var newEnemyPrefab = project.FindAsset("HarderEnemy", AssetType.Prefab);

// Set the prefab reference (guid:path format)
spawner.Set("PrefabToSpawn", newEnemyPrefab.Reference);
spawner.Set("SpawnInterval", 3.0f); // Spawn faster
spawner.Set("MaxSpawns", 20); // Spawn more

scene.Save();
```

### Save and Reload with Custom Components

**Critical:** Changes to custom components persist through save/reload:

```csharp
var project = new StrideProject(@"C:\MyGame");

// Modify
var scene = project.LoadScene("Level1");
var boss = scene.FindEntityByName("Boss");
var bossAI = boss.GetComponent("BossAIComponent");

bossAI.Set("Difficulty", "Hard");
bossAI.Set("AggressionLevel", 0.9f);

scene.Save();

// Reload and verify
var reloadedScene = project.LoadScene("Level1");
var reloadedBoss = reloadedScene.FindEntityByName("Boss");
var reloadedAI = reloadedBoss.GetComponent("BossAIComponent");

var difficulty = reloadedAI.Get<string>("Difficulty");
// difficulty == "Hard" ✓

var aggression = reloadedAI.Get<float>("AggressionLevel");
// aggression == 0.9f ✓
```

### Finding Entities with Custom Components

All find methods work with custom components:

```csharp
// By component type
var allCharacters = scene.FindEntitiesWithComponent("CharacterController");
var allInventories = scene.FindEntitiesWithComponent("InventoryComponent");

// Partial match works
var allHealthComponents = scene.FindEntitiesWithComponent("Health");
// Matches: HealthComponent, PlayerHealthComponent, EnemyHealthComponent, etc.

// Complex queries
var lowHealthEnemies = scene.FindEntities(e =>
{
    if (!e.HasComponent("EnemyAI") || !e.HasComponent("HealthComponent"))
        return false;

    var health = e.GetComponent("HealthComponent");
    var current = health.Get<float>("CurrentHealth");
    var max = health.Get<float>("MaxHealth");

    return (current / max) < 0.3f; // Less than 30% health
});
```

### Adding Custom Components to Entities

```csharp
var scene = Scene.Load("Level1.sdscene");
var newEnemy = scene.CreateEntity("Enemy_01", "Enemies");

// Add your custom component with initial properties
var enemyAI = newEnemy.AddComponent("MyGame.Components.EnemyAI");
enemyAI.Set("AggressionLevel", 0.7f);
enemyAI.Set("PatrolRadius", 15.0f);
enemyAI.Set("DetectionRange", 10.0f);
enemyAI.Set("AttackDamage", 25.0f);
enemyAI.Set("PreferredWeapon", "Sword");

// Add health component
var health = newEnemy.AddComponent("MyGame.Components.HealthComponent");
health.Set("MaxHealth", 100.0f);
health.Set("CurrentHealth", 100.0f);
health.Set("Armor", 20.0f);

scene.Save();
```

### Type Conversion Edge Cases

The `Get<T>` method handles automatic type conversion:

```csharp
var component = entity.GetComponent("CustomComponent");

// String to numeric conversion
component.Set("DamageValue", "42.5");
float damage = component.Get<float>("DamageValue"); // 42.5f ✓

// Numeric to string conversion
component.Set("Level", 10);
string levelStr = component.Get<string>("Level"); // "10" ✓

// Boolean conversion
component.Set("IsEnabled", "true");
bool enabled = component.Get<bool>("IsEnabled"); // true ✓

// If conversion fails, returns default(T)
string invalidNum = component.Get<string>("NonexistentProperty"); // null
int invalidInt = component.Get<int>("NonexistentProperty"); // 0
```

### Common Pitfalls with Custom Components

#### ❌ Don't: Use typed wrappers for custom components

```csharp
// This WON'T work - typed wrappers are only for built-in components
var controller = entity.GetCharacterController(); // No such method!
```

#### ✓ Do: Use dictionary access

```csharp
var controller = entity.GetComponent("CharacterController");
var speed = controller.Get<float>("MoveSpeed");
```

#### ❌ Don't: Assume property names match C# exactly

```csharp
// If your C# property is "moveSpeed" (camelCase)
// but YAML serializes as "MoveSpeed" (PascalCase)
var speed = controller.Get<float>("moveSpeed"); // Might return 0 or null
```

#### ✓ Do: Check actual YAML or use debug tests

```csharp
// Inspect what the parser actually sees
var allProps = controller.Properties;
foreach (var prop in allProps)
{
    Console.WriteLine($"{prop.Key}: {prop.Value}");
}
```

#### ❌ Don't: Forget to check if component exists

```csharp
var health = entity.GetComponent("HealthComponent");
float currentHP = health.Get<float>("CurrentHealth"); // NullReferenceException if no component!
```

#### ✓ Do: Check before accessing

```csharp
if (entity.HasComponent("HealthComponent"))
{
    var health = entity.GetComponent("HealthComponent");
    float currentHP = health.Get<float>("CurrentHealth");
}
```

---

## Typed Component Wrappers

For **built-in Stride components only**, the toolkit provides typed wrappers with properties and helper methods.

**Namespace:** `HS.Stride.Editor.Toolkit.Core.Wrappers`

Most wrappers include a static `CreateComponent()` method that can be used to construct a valid, default component instance, which is then typically added to an entity.

### TransformWrapper

Access via `entity.GetTransform()` which returns a `TransformWrapper`.

**Properties:**

- `Component Component` - The underlying component

**Getter Methods:**

- `Vector3Data GetPosition()` - Get current position
- `QuaternionData GetRotation()` - Get current rotation
- `Vector3Data GetScale()` - Get current scale
- `Dictionary<string, object> GetChildren()` - Get child entity references

**Setter Methods:**

- `void SetPosition(float x, float y, float z)` - Set position
- `void SetRotation(float x, float y, float z, float w)` - Set rotation quaternion
- `void SetScale(float x, float y, float z)` - Set scale
- `void SetUniformScale(float scale)` - Set uniform scale on all axes
- `void AddChild(string childEntityId)` - Add child entity
- `void RemoveChild(string childEntityId)` - Remove child entity
- `bool HasChild(string childEntityId)` - Check if has child

**Example:**

```csharp
var transform = entity.GetTransform();

// Read
var pos = transform.GetPosition();
Console.WriteLine($"Entity at {pos.X}, {pos.Y}, {pos.Z}");

// Write
transform.SetPosition(10, 5, 0);
transform.SetRotation(0, 0, 0, 1);
transform.SetScale(2, 2, 2);
transform.SetUniformScale(1.5f);

// Children
transform.AddChild(childEntity.Id);
if (transform.HasChild(childEntity.Id))
{
    transform.RemoveChild(childEntity.Id);
}
```

### ModelWrapper

Access via `entity.GetModel()` which returns a `ModelWrapper`.

**Properties:**

- `bool Enabled` - Enable/disable rendering
- `string Model` - Model asset reference (guid:path format)
- `Dictionary<string, object> Materials` - Material slots

**Methods:**

- `void SetModel(string guid, string path)`
- `void SetModel(AssetReference modelAsset)`
- `void SetMaterial(int slotIndex, AssetReference material)` - Sets material for a specific slot (0, 1, 2, etc.)

**Example:**

```csharp
var model = entity.GetModel();

// Set model from project
var newModel = project.FindAsset("PlayerModel", AssetType.Model);
model.SetModel(newModel);

// Set material for slot 0
var material = project.FindAsset("PlayerSkin", AssetType.Material);
model.SetMaterial(0, material);

// Toggle visibility
model.Enabled = false;
```

**Prefab Instance Limitation:**

Modifying components on prefab instances (entities with a `Base` section) may not persist correctly when saved. To modify entities:
- Break the prefab link in Stride first, OR
- Edit the prefab asset directly instead of the instance

### StaticColliderWrapper

Access via `entity.GetStaticCollider()` which returns a `StaticColliderWrapper`.

**Properties:**

- `Component Component` - The underlying component
- `bool CanSleep` - Physics sleep enabled
- `float Restitution` - Bounciness (0-1)
- `float Friction` - Surface friction
- `float RollingFriction` - Rolling friction
- `bool IsTrigger` - Is trigger collider
- `Dictionary<string, object> ColliderShapes` - Shape definitions

**Shape Methods:**

- `void AddBoxShape(float sizeX = 1.0f, float sizeY = 1.0f, float sizeZ = 1.0f, bool is2D = false, float offsetX = 0.0f, float offsetY = 0.0f, float offsetZ = 0.0f)`
- `void AddSphereShape(float radius = 0.5f, bool is2D = false, float offsetX = 0.0f, float offsetY = 0.0f, float offsetZ = 0.0f)`
- `void AddCapsuleShape(float length = 1.0f, float radius = 0.5f, string orientation = "UpY", float offsetX = 0.0f, float offsetY = 0.0f, float offsetZ = 0.0f)`
- `void AddCylinderShape(float height = 1.0f, float radius = 0.5f, string orientation = "UpY", float offsetX = 0.0f, float offsetY = 0.0f, float offsetZ = 0.0f)`
- `void AddConeShape(float height = 1.0f, float radius = 0.5f, string orientation = "UpY", float offsetX = 0.0f, float offsetY = 0.0f, float offsetZ = 0.0f)`
- `void AddMeshShape(AssetReference modelAsset)` - Static mesh collider (uses model geometry directly)
- `void AddConvexHullShape(AssetReference colliderShapeAsset)` - Convex hull collider (requires .sdphy file created via StrideProject.CreateColliderShape())
- `void AddPlaneShape(float normalX = 0.0f, float normalY = 1.0f, float normalZ = 0.0f, float offset = 0.0f)` - Infinite plane
- `void AddColliderShapeAsset(AssetReference colliderShapeAsset)` - Reference to .sdphy collider asset
- `void AddColliderShapeAsset(string assetGuid, string assetPath)` - Reference to .sdphy collider asset by guid/path

**Example:**

```csharp
var collider = entity.AddStaticCollider();

// Set physics properties
collider.Friction = 0.8f;
collider.Restitution = 0.0f;
collider.IsTrigger = false;

// Add shapes
collider.AddBoxShape(2.0f, 1.0f, 2.0f);
collider.AddSphereShape(radius: 0.5f, offsetY: 1.0f);
collider.AddCapsuleShape(length: 2.0f, radius: 0.5f, orientation: "UpY");

// Mesh collider from asset (uses model geometry directly)
var model = project.FindAsset("TerrainModel", AssetType.Model);
collider.AddMeshShape(model);

// Convex hull collider - requires creating .sdphy file first
var modelRef = project.FindAsset("CrateModel", AssetType.Model);
var hullAsset = project.CreateColliderShape(modelRef.Reference, "Crate_hull");
collider.AddConvexHullShape(hullAsset.GetReference());
```

**Convex Hull Workflow Example:**

Convex hulls MUST be stored in `.sdphy` files and cannot be inlined in scenes. Here's the complete workflow:

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");

// 1. Find or create the convex hull asset
var hull = project.FindAsset("Wall_hull", AssetType.ColliderShape);

if (hull == null)
{
    // Create new .sdphy file with convex hull
    var modelRef = project.FindAsset("Wall", AssetType.Model);
    var createdHull = project.CreateColliderShape(modelRef.Reference, "Wall_hull");
    hull = createdHull.GetReference();

    // Rescan to register the new asset
    project.Rescan();
}

// 2. Add convex hull to entity
var entity = scene.FindEntity("WallEntity");
var collider = entity.AddStaticCollider();
collider.AddConvexHullShape(hull); // References the .sdphy file

scene.Save();
```

### RigidbodyWrapper

Access via `entity.GetRigidbody()` which returns a `RigidbodyWrapper`.

The RigidbodyWrapper has the same shape methods as StaticColliderWrapper, plus additional physics properties.

**Properties:**

- `bool CanSleep` - Physics sleep enabled
- `bool IsKinematic` - Kinematic vs dynamic
- `float Mass` - Object mass
- `float LinearDamping` - Linear velocity damping
- `float AngularDamping` - Angular velocity damping
- `bool OverrideGravity` - Use custom gravity

**Getter/Setter Methods:**

- `Vector3Data GetGravity()` - Get custom gravity vector
- `void SetGravity(float x, float y, float z)` - Set custom gravity vector

**Example:**

```csharp
var rigidbody = entity.GetRigidbody();

rigidbody.Mass = 5.0f;
rigidbody.IsKinematic = false;
rigidbody.LinearDamping = 0.1f;
rigidbody.AngularDamping = 0.05f;

// Custom gravity
rigidbody.OverrideGravity = true;
rigidbody.SetGravity(0, -20.0f, 0);
```

### LightWrapper

Access via `entity.GetLight()` which returns a `LightWrapper`.

**Properties:**

- `float Intensity` - Light intensity
- `Dictionary<string, object> Type` - Light type configuration (Point, Directional, Spot)

**Getter/Setter Methods:**

- `ColorData GetColor()` - Get light color
- `void SetColor(float r, float g, float b, float a = 1.0f)` - Set light color

**Example:**

```csharp
var light = entity.GetLight();

light.Intensity = 2.0f;
light.SetColor(1.0f, 0.8f, 0.6f, 1.0f);
```

---

##### AssetReference Class

**Namespace:** `HS.Stride.Editor.Toolkit.Core.AssetEditing`

Represents a reference to a Stride asset.

#### Properties

##### `string Id`

The asset's unique GUID.

##### `string Name`

The asset's file name (without extension).

##### `string Path`

The asset's relative path from the Assets folder (forward slashes, no extension).

##### `string FilePath`

The asset's absolute file path on disk.

##### `AssetType Type`

The asset's type (enum).

##### `string Reference`

The reference string used in scenes: `"{Id}:{Path}"`.

**Example:**

```csharp
var prefab = scanner.FindAsset("Player", AssetType.Prefab);

Console.WriteLine($"ID: {prefab.Id}");
Console.WriteLine($"Name: {prefab.Name}");
Console.WriteLine($"Path: {prefab.Path}");
Console.WriteLine($"FilePath: {prefab.FilePath}");
Console.WriteLine($"Type: {prefab.Type}");
Console.WriteLine($"Reference: {prefab.Reference}");

// Use reference to instantiate
scene.InstantiatePrefab(prefab, new Vector3Data(0, 0, 0));
```

### AssetType Enum

```csharp
public enum AssetType
{
    Unknown,
    Prefab,      // .sdprefab
    Model,       // .sdm3d
    Material,    // .sdmat
    Texture,     // .sdtex
    Scene,       // .sdscene
    Sound,       // .sdsnd
    Animation,   // .sdanim
    Skeleton,    // .sdskel
    SpriteSheet, // .sdsheet
    Effect,      // .sdfx
    UIPage,      // .sdpage
    RawAsset     // .sdraw
}
```

### ParentType Enum

**Namespace:** `HS.Stride.Editor.Toolkit.Core`

Defines how an entity should be organized in the scene hierarchy.

```csharp
public enum ParentType
{
    Folder,  // Editor organization only (metadata, no transform hierarchy)
    Entity   // Transform hierarchy (children follow parent transforms)
}
```

#### ParentType.Folder

**Use When:** You want entities grouped in the editor for organization only

**Behavior:**

- Entities appear grouped in the editor under a folder name
- Entities remain independent in world space (no transform inheritance)
- Folder cannot be moved - it's just a metadata label (like Unreal/Stride folders)

**Example:**

```csharp
scene.CreateEntity("Enemy_01", "Enemies", ParentType.Folder);
// Entity appears in "Enemies" folder in editor, but has independent transform
```

#### ParentType.Entity

**Use When:** You need parent-child relationships with transform inheritance

**Behavior:**

- Child transforms are relative to parent entity
- Moving/rotating/scaling parent affects all children
- Parent is a real entity that can be moved in the scene (like Unity GameObjects)
- Supports nested paths: "Parent/Child/GrandChild"

**Example:**

```csharp
scene.CreateEntity("Door", "House", ParentType.Entity);
// Door's transform is relative to House entity
// Moving House moves Door with it
```

---

## Direct Asset Editing

All asset classes implement the `IStrideAsset` interface with common methods:

- `string Id` - Asset GUID
- `string FilePath` - File path
- `void Save()` - Save to original file
- `void SaveAs(string filePath)` - Save to new file

### MaterialAsset

**Namespace:** `HS.Stride.Editor.Toolkit.Core.AssetEditing`

Represents an editable Stride Material asset (.sdmat).

#### Loading

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var material = project.LoadMaterial("PlayerMat");

// Alternative: Load directly by full path
var material = MaterialAsset.Load(@"C:\MyGame\Assets\Materials\PlayerMat.sdmat");
```

#### Properties

##### `string Id`

The asset's unique GUID (inherited from `IStrideAsset`).

##### `string FilePath`

The asset's absolute file path on disk (inherited from `IStrideAsset`).

#### Methods

##### `void Save()`

Saves the material's current state back to its original file (inherited from `IStrideAsset`).

##### `void SaveAs(string filePath)`

Saves the material's current state to a new file (inherited from `IStrideAsset`).

##### `object? Get(string propertyName)`

Gets a property value by name. Supports nested paths with dot notation.
**NOTE:** Only properties saved in the `.sdmat` file (visible in Stride's Property Grid) can be accessed.
**Example:**

```csharp
// Get the direct texture ID/path reference
var textureReference = material.Get("Attributes.Diffuse.DiffuseMap.Texture");

// Get a nested property like color components
var diffuseColorR = material.Get("Attributes.Diffuse.Color.R");
```

##### `void Set(string propertyName, object value)`

Sets a property value by name. Supports nested paths with dot notation.
**NOTE:** Only properties that Stride serializes will persist when saved.
**Example:**

```csharp
// Set the direct texture ID/path reference
material.Set("Attributes.Diffuse.DiffuseMap.Texture", newTextureRef.Reference);

// Set a nested property, e.g., diffuse color
material.Set("Attributes.Diffuse.Color.R", 1.0f);
material.Set("Attributes.Diffuse.Color.G", 0.5f);
material.Set("Attributes.Diffuse.Color.B", 0.0f);
```

##### `string? GetDiffuseTexture()`

Gets the diffuse texture reference string (e.g., "guid:path").

##### `void SetDiffuseTexture(string textureReference)`

Sets the diffuse texture reference string.

**Example:**

```csharp
var newTex = project.FindAsset("NewTexture", AssetType.Texture);
if (newTex != null)
{
    material.SetDiffuseTexture(newTex.Reference);
    material.Save();
}
```

##### `(float X, float Y)? GetUVScale()`

Gets the UV scale values as a tuple (X, Y). Returns `null` if not found.

##### `void SetUVScale(float x, float y)`

Sets the UV scale.

**Example:**

```csharp
material.SetUVScale(2.0f, 2.0f);
material.Save();
```

##### `Dictionary<string, object> GetAllProperties()`

Gets all properties as a dictionary (for inspection/debugging).

### TextureAsset

**Namespace:** `HS.Stride.Editor.Toolkit.Core.AssetEditing`

Represents an editable Stride Texture asset (.sdtex).

#### Loading

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var texture = project.LoadTexture("Player");

// Alternative: Load directly by full path
var texture = TextureAsset.Load(@"C:\MyGame\Assets\Textures\Player.sdtex");
```

#### Properties

##### `string Id`

The asset's unique GUID (inherited from `IStrideAsset`).

##### `string FilePath`

The asset's absolute file path on disk (inherited from `IStrideAsset`).

##### `bool IsStreamable`

Gets or sets whether the texture is streamable.

**Example:**

```csharp
texture.IsStreamable = true;
texture.Save();
```

##### `bool? PremultiplyAlpha`

Gets or sets whether to premultiply alpha (if Type is ColorTextureType).

**Example:**

```csharp
texture.PremultiplyAlpha = false;
texture.Save();
```

#### Methods

##### `void Save()`

Saves the texture's current state back to its original file (inherited from `IStrideAsset`).

##### `void SaveAs(string filePath)`

Saves the texture's current state to a new file (inherited from `IStrideAsset`).

##### `string? GetSource()`

Gets the source file path for the texture. Returns the path without the `!file` prefix.

##### `void SetSource(string sourcePath)`

Sets the source file path for the texture. The `!file` prefix will be automatically added.

**Example:**

```csharp
// Set source to a relative path within the project
texture.SetSource("../Textures/player_diffuse.png");
texture.Save();

// Get the source path back
var sourcePath = texture.GetSource(); // Will be "../Textures/player_diffuse.png"
```

##### `object? Get(string propertyName)`

Gets a property value by name. Supports nested paths with dot notation.
**NOTE:** Only properties saved in the `.sdtex` file (visible in Stride's Property Grid) can be accessed.

##### `void Set(string propertyName, object value)`

Sets a property value by name. Supports nested paths with dot notation.
**NOTE:** Only properties that Stride serializes will persist when saved.

##### `Dictionary<string, object> GetAllProperties()`

Gets all properties as a dictionary (for inspection/debugging).

### AnimationAsset

**Namespace:** `HS.Stride.Editor.Toolkit.Core.AssetEditing`

Represents an editable Stride Animation asset (.sdanim).

#### Loading

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var anim = project.LoadAnimation("Walk");

// Alternative: Load directly by full path
var anim = AnimationAsset.Load(@"C:\MyGame\Assets\Animations\Walk.sdanim");
```

#### Properties

##### `string Id`

The asset's unique GUID (inherited from `IStrideAsset`).

##### `string FilePath`

The asset's absolute file path on disk (inherited from `IStrideAsset`).

##### `string? RepeatMode`

Gets or sets the repeat mode for the animation (e.g., "LoopInfinite", "PlayOnce").

**Example:**

```csharp
anim.RepeatMode = "LoopInfinite";
anim.Save();
```

##### `bool RootMotion`

Gets or sets whether root motion is enabled for the animation.

**Example:**

```csharp
anim.RootMotion = true;
anim.Save();
```

#### Methods

##### `void Save()`

Saves the animation's current state back to its original file (inherited from `IStrideAsset`).

##### `void SaveAs(string filePath)`

Saves the animation's current state to a new file (inherited from `IStrideAsset`).

##### `string? GetSource()`

Gets the source file path for the animation.

##### `void SetSource(string sourcePath)`

Sets the source file path for the animation.

**Example:**

```csharp
anim.SetSource("../Animations/player_walk.fbx");
anim.Save();
```

##### `string? GetSkeletonReference()`

Gets the reference to the skeleton asset used by this animation.

##### `void SetSkeletonReference(string skeletonReference)`

Sets the reference to the skeleton asset used by this animation.

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");
var anim = project.LoadAnimation("Walk");
var skeleton = project.FindAsset("PlayerSkeleton", AssetType.Skeleton);

anim.SetSkeletonReference(skeleton.Reference);
anim.Save();
```

##### `string? GetPreviewModel()`

Gets the reference to the preview model asset for this animation.

##### `void SetPreviewModel(string modelReference)`

Sets the reference to the preview model asset for this animation.

**Example:**

```csharp
var project = new StrideProject(@"C:\MyGame");
var anim = project.LoadAnimation("Walk");
var model = project.FindAsset("PlayerModel", AssetType.Model);

anim.SetPreviewModel(model.Reference);
anim.Save();
```

##### `object? Get(string propertyName)`

Gets a property value by name. Supports nested paths with dot notation.
**NOTE:** Only properties saved in the `.sdanim` file (visible in Stride's Property Grid) can be accessed.

##### `void Set(string propertyName, object value)`

Sets a property value by name. Supports nested paths with dot notation.
**NOTE:** Only properties that Stride serializes will persist when saved.

##### `Dictionary<string, object> GetAllProperties()`

Gets all properties as a dictionary (for inspection/debugging).

### ColliderShapeAsset

**Namespace:** `HS.Stride.Editor.Toolkit.Core.AssetEditing`

Represents an editable Stride ColliderShape asset (.sdphy). Used for referencing reusable collision shapes created in Stride Editor.

**IMPORTANT:** Use `StrideProject.CreateColliderShape()` to create and `StrideProject.LoadColliderShape()` to load collider shape assets. The convex hull data will be null until Stride processes the asset.

#### Loading an Existing Asset

```csharp
var project = new StrideProject(projectPath);
var colliderAsset = project.LoadColliderShape("ColliderHull"); // Load by name or path
```

#### Properties

##### `string Id`

The asset's unique GUID (inherited from `IStrideAsset`).

##### `string FilePath`

The asset's absolute file path on disk (inherited from `IStrideAsset`).

#### Methods

##### `void Save()`

Saves the collider shape asset to its current file path (inherited from `IStrideAsset`).

##### `void SaveAs(string filePath)`

Saves the collider shape asset to a new path. Must have `.sdphy` extension (inherited from `IStrideAsset`).

##### `AssetReference GetReference()`

Gets an asset reference for use in scenes. Must be called after saving the asset.

#### Example: Using an Existing Collider Asset

```csharp
var project = new StrideProject(projectPath);

// Load existing collider shape asset by name or path
var colliderAsset = project.LoadColliderShape("CrateCollider");

// Use in scene
var scene = project.LoadScene("Level1");
var entity = scene.FindEntity("Crate");
entity.AddStaticCollider().AddColliderShapeAsset(colliderAsset.GetReference());
scene.Save();
```

### PrefabAsset

For detailed information on creating and manipulating Prefab assets, please see the [Prefab Creation (Programmatically)](#prefab-creation-programmatically) section, which includes the full `Prefab` class API.

### Generic Asset Editing: SoundAsset, SkeletonAsset, SpriteSheetAsset, EffectAsset

**Namespace:** `HS.Stride.Editor.Toolkit.Core.AssetEditing`

These asset types typically involve fewer direct manipulation methods than `MaterialAsset`. For `SoundAsset`, `SkeletonAsset`, `SpriteSheetAsset`, and `EffectAsset`, interactions primarily involve loading the asset and then using the generic `Get()` and `Set()` methods to modify their properties.

They all implement the `IStrideAsset` interface, providing:

- `string Id` - The asset's unique GUID.
- `string FilePath` - The asset's absolute file path on disk.
- `void Save()` - Saves the asset's current state back to its original file.
- `void SaveAs(string filePath)` - Saves the asset's current state to a new file.

These assets can be loaded using their static `Load(string filePath)` method.

#### SoundAsset

Represents an editable Stride Sound asset (`.sdsnd`).

##### Loading

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var sound = project.LoadSound("Explosion");

// Alternative: Load directly by full path
var sound = SoundAsset.Load(@"C:\MyGame\Assets\Sounds\Explosion.sdsnd");
```

##### Properties & Methods (Generic)

Sound assets often have properties like `Stream`, `Spatialized`, `Volume`, or `Loop`. You can access and modify these using the generic `Get` and `Set` methods.

**Example:**

```csharp
// Load a sound asset
var project = new StrideProject(@"C:\MyGame");
var sound = project.LoadSound("Explosion");

// Get and set properties
var isStreamed = sound.Get<bool>("Stream");
sound.Set("Volume", 0.75f);
sound.Set("Spatialized", true);
sound.Save();

// Get all properties for inspection
var allSoundProps = sound.GetAllProperties();
foreach (var prop in allSoundProps)
{
    Console.WriteLine($"Sound Property - {prop.Key}: {prop.Value}");
}
```

#### SkeletonAsset

Represents an editable Stride Skeleton asset (`.sdskel`).

##### Loading

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var skeleton = project.LoadSkeleton("PlayerSkeleton");

// Alternative: Load directly by full path
var skeleton = SkeletonAsset.Load(@"C:\MyGame\Assets\Characters\PlayerSkeleton.sdskel");
```

##### Properties & Methods (Generic)

Skeleton assets may have properties related to their bone structure or retargeting.

**Example:**

```csharp
// Load a skeleton asset
var project = new StrideProject(@"C:\MyGame");
var skeleton = project.LoadSkeleton("PlayerSkeleton");

// Access generic properties
var rootBoneName = skeleton.Get<string>("RootBone");
skeleton.Set("EnableRetargeting", true);
skeleton.Save();
```

#### SpriteSheetAsset

Represents an editable Stride Sprite Sheet asset (`.sdsheet`).

##### Loading

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var spriteSheet = project.LoadSpriteSheet("Icons");

// Alternative: Load directly by full path
var spriteSheet = SpriteSheetAsset.Load(@"C:\MyGame\Assets\UI\Icons.sdsheet");
```

##### Properties & Methods (Generic)

Sprite sheets contain definitions for individual sprites. Properties might include texture references or metadata per sprite.

**Example:**

```csharp
// Load a sprite sheet asset
var project = new StrideProject(@"C:\MyGame");
var spriteSheet = project.LoadSpriteSheet("Icons");

// Access generic properties (e.g., source texture)
var sourceTextureRef = spriteSheet.Get<string>("Texture");

// Modify properties (e.g., adding a new sprite or modifying existing frames)
// Note: More complex modifications might require deeper inspection of YAML structure
spriteSheet.Set("Sprites.new_icon.Region.X", 0);
spriteSheet.Set("Sprites.new_icon.Region.Y", 0);
spriteSheet.Save();
```

#### EffectAsset

Represents an editable Stride Effect asset (`.sdfx`).

##### Loading

```csharp
// Recommended: Use StrideProject to load by name
var project = new StrideProject(@"C:\MyGame");
var effect = project.LoadEffect("MyCustomEffect");

// Alternative: Load directly by full path
var effect = EffectAsset.Load(@"C:\MyGame\Assets\Materials\MyCustomEffect.sdfx");
```

##### Properties & Methods (Generic)

Effect assets often define shader parameters.

**Example:**

```csharp
// Load an effect asset
var project = new StrideProject(@"C:\MyGame");
var effect = project.LoadEffect("MyCustomEffect");

// Access generic properties (e.g., tweaking shader uniforms)
var blendMode = effect.Get<string>("BlendState");
effect.Set("MyShaderParameter.Strength", 0.5f);
effect.Save();
```

##### Generic Get/Set Methods

All these asset types support the following generic methods:

- `object? Get(string propertyName)`: Gets a property value by name, supporting dot notation for nested properties.
- `void Set(string propertyName, object value)`: Sets a property value by name, supporting dot notation for nested properties.
- `Dictionary<string, object> GetAllProperties()`: Returns all properties as a dictionary for inspection.

**NOTE:** All `Get` and `Set` methods only work with properties that Stride serializes to the asset file and are visible in Stride's Property Grid.

---

## Data Types & Utilities

### Vector3Data

**Namespace:** `HS.Stride.Editor.Toolkit.Core`

Represents a 3D vector.

#### Properties

- `float X`, `float Y`, `float Z`

#### Static Properties

- `Vector3Data Zero` - (0, 0, 0)
- `Vector3Data One` - (1, 1, 1)

#### Methods

##### `Dictionary<string, object> ToMultiValueProperty()`

Converts to dictionary for component properties.

##### `static Vector3Data FromMultiValueProperty(Dictionary<string, object>? dict)`

Creates from dictionary.

**Example:**

```csharp
var pos = new Vector3Data(10, 5, 3);
var transform = entity.GetTransform();
transform.SetPosition(pos.X, pos.Y, pos.Z);

// Read position
var currentPos = transform.GetPosition();
Console.WriteLine($"Position: {currentPos.X}, {currentPos.Y}, {currentPos.Z}");
```

### QuaternionData

**Namespace:** `HS.Stride.Editor.Toolkit.Core`

Represents a rotation quaternion.

#### Properties

- `float X`, `float Y`, `float Z`, `float W`

#### Static Properties

- `QuaternionData Identity` - (0, 0, 0, 1)

#### Methods

- `Dictionary<string, object> ToMultiValueProperty()`
- `static QuaternionData FromMultiValueProperty(Dictionary<string, object>? dict)`

### ColorData

**Namespace:** `HS.Stride.Editor.Toolkit.Core`

Represents an RGBA color.

#### Properties

- `float R`, `float G`, `float B`, `float A`

#### Static Properties

- `ColorData White` - (1, 1, 1, 1)
- `ColorData Black` - (0, 0, 0, 1)

#### Methods

- `Dictionary<string, object> ToMultiValueProperty()`
- `static ColorData FromMultiValueProperty(Dictionary<string, object>? dict)`

**Example:**

```csharp
var light = entity.GetLight();
light.SetColor(1.0f, 0.5f, 0.0f, 1.0f);
```

### PrefabData

**Namespace:** `HS.Stride.Editor.Toolkit.Core.DataTypes`

Represents prefab instance data for entities that are instances of prefabs.

#### Properties

- `string PrefabSourcePath` - The asset path to the source prefab
- `string PrefabEntityId` - The root entity ID within the prefab
- `string InstanceId` - Unique identifier for this prefab instance

**Example:**

```csharp
var entity = scene.FindEntityByName("EnemyInstance");
if (entity.ParentPrefab != null)
{
    Console.WriteLine($"Prefab source: {entity.ParentPrefab.PrefabSourcePath}");
    Console.WriteLine($"Instance ID: {entity.ParentPrefab.InstanceId}");
}
```

### EntityRefData

**Namespace:** `HS.Stride.Editor.Toolkit.Core.DataTypes`

Represents an entity reference stored in a component property. Used to read entity references from components.

#### Methods

- `static EntityRefData? Parse(string refString)` - Parses an entity reference string

**Example:**

```csharp
// Read an entity reference from a component
var targetRef = component.Get<EntityRefData>("TargetEntity");
if (targetRef != null)
{
    Console.WriteLine($"Entity reference: {targetRef}");
}

// To set an entity reference, use SetEntityRef
component.SetEntityRef("TargetEntity", someEntity);
```

### AssetRefData

**Namespace:** `HS.Stride.Editor.Toolkit.Core.DataTypes`

Represents an asset reference stored in a component property. Used to read asset references (prefabs, textures, materials, etc.) from components.

#### Methods

- `static AssetRefData? Parse(string refString)` - Parses an asset reference string (GUID:Path format)

**Example:**

```csharp
// Read an asset reference from a component
var prefabRef = component.Get<AssetRefData>("SpawnPrefab");
if (prefabRef != null)
{
    Console.WriteLine($"Asset reference: {prefabRef}");
}

// To set an asset reference, use SetAssetRef
var prefab = project.FindAsset("EnemyPrefab", AssetType.Prefab);
component.SetAssetRef("SpawnPrefab", prefab);
```

---

## Error Handling

### Exceptions Thrown by the API

All public methods validate their parameters and throw appropriate exceptions:

#### `ArgumentNullException`

Thrown when required string parameters are null or whitespace.

**Methods that throw:**

- All `Scene` methods (Load, Find, Create, Add, Remove, Save)
- All `Entity.GetComponent` calls
- All Asset `Load` and `SaveAs` methods
- `StrideProject` constructor
- Component `Get<T>` and `Set` methods (when propertyPath is null)

**Example:**

```csharp
try
{
    var scene = Scene.Load(null); // Throws ArgumentNullException
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Invalid parameter: {ex.ParamName}");
}
```

#### `FileNotFoundException`

Thrown when trying to load a file that doesn't exist.

**Methods that throw:**

- `Scene.Load(string filePath)` when file not found
- All Asset `Load` methods when file not found

**Example:**

```csharp
try
{
    var scene = Scene.Load("NonexistentScene.sdscene");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Scene file not found: {ex.FileName}");
}
```

#### `ArgumentException`

Thrown for invalid arguments beyond null checks.

**Methods that throw:**

- `StrideProject(string projectPath)` when path is not a Stride project

**Example:**

```csharp
try
{
    var project = new StrideProject(@"C:\NotAStrideProject");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid project: {ex.Message}");
}
```

### Safe Operations with Return Values

Some methods return null or default values instead of throwing:

#### Methods that return null

- `Entity.GetComponent(string componentType)` - Returns null if component not found
- `Component.Get<T>(string propertyPath)` - Returns default(T) if property not found
- `Scene.FindEntityById/ByName` - Returns null if entity not found
- `StrideProject.FindAsset` - Returns null if asset not found
- All asset `Get` methods - Return null if property not found

**Example:**

```csharp
// Safe pattern
var entity = scene.FindEntityByName("Player");
if (entity != null)
{
    var health = entity.GetComponent("HealthComponent");
    if (health != null)
    {
        var hp = health.Get<float>("CurrentHealth");
        Console.WriteLine($"Player HP: {hp}");
    }
}
```

### Best Practices for Error Handling

```csharp
// Validate before loading
if (File.Exists(scenePath))
{
    var scene = Scene.Load(scenePath);
    // ...
}

// Check component existence
if (entity.HasComponent("HealthComponent"))
{
    var health = entity.GetComponent("HealthComponent");
    // ...
}

// Null-coalescing for defaults
var speed = controller?.Get<float>("MoveSpeed") ?? 5.0f;

// Try-catch for file operations
try
{
    scene.Save();
}
catch (IOException ex)
{
    Console.WriteLine($"Save failed: {ex.Message}");
}
```

---

## Advanced Workflows & Best Practices

This section provides guidance on common usage patterns, performance considerations, safety practices, and advanced workflows to help you get the most out of the toolkit.

### 1. Batch Operations and Automation

Leverage the toolkit's file-based nature for mass modifications and automated tasks.

#### Replacing Placeholders with Prefabs

This example demonstrates how to find placeholder entities in a scene and replace them with actual prefab instances based on their names.

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");

var placeholders = scene.FindEntitiesByName("Placeholder_*");
foreach (var placeholder in placeholders)
{
    // Extract prefab name from placeholder's name
    var prefabName = placeholder.Name.Replace("Placeholder_", "");
    var prefab = project.FindAsset(prefabName, AssetType.Prefab);

    if (prefab != null)
    {
        // Get position from placeholder
        var transform = placeholder.GetTransform();
        var pos = transform.GetPosition();

        // Instantiate prefab at placeholder's position
        var instance = scene.InstantiatePrefab(prefab, pos, placeholder.Folder);

        // Remove the original placeholder entity
        scene.RemoveEntity(placeholder);

        Console.WriteLine($"Replaced {placeholder.Name} with {prefab.Name}");
    }
}

scene.Save();
```

#### Batch Modify Materials Across All Scenes

This example shows how to iterate through multiple scenes and modify material properties based on certain criteria.

```csharp
var project = new StrideProject(@"C:\MyGame");

var newTexture = project.FindAsset("NewGroundTexture", AssetType.Texture); // An asset to apply

foreach (var sceneRef in project.GetScenes())
{
    var scene = project.LoadScene(sceneRef.Name);

    // Find all entities with models that use a "Ground" material
    var groundObjects = scene.FindEntities(e =>
    {
        if (!e.HasComponent("ModelComponent"))
            return false;

        var model = e.GetModel();
        // Check if any of the model's materials contain "Ground" in their name
        return model.Materials.Values.Any(m =>
        {
            var matDict = m as Dictionary<string, object>;
            return matDict != null &&
                   matDict.ContainsKey("Name") &&
                   matDict["Name"].ToString().Contains("Ground");
        });
    });

    // Update texture on all matching materials
    foreach (var obj in groundObjects)
    {
        var model = obj.GetModel();
        foreach (var materialSlot in model.Materials.Values)
        {
            // Assuming materialSlot is a Component (MaterialComponent)
            var materialComponent = materialSlot as Component;
            if (materialComponent != null &&
                materialComponent.Get<string>("Name").Contains("Ground") &&
                newTexture != null)
            {
                // Set the diffuse texture for the material
                materialComponent.Set("Diffuse.Texture", newTexture.Reference);
            }
        }
    }

    scene.Save();
    Console.WriteLine($"Updated {sceneRef.Name}: {groundObjects.Count} objects with new ground texture.");
}
```

#### Procedural Level Generation

Automate the creation of complex scenes using prefab instantiation and entity manipulation.

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("ProcGenLevel"); // Load an empty or template scene

var tilePrefab = project.FindAsset("FloorTile", AssetType.Prefab);
var wallPrefab = project.FindAsset("Wall", AssetType.Prefab);

// Generate a 10x10 grid level
for (int x = 0; x < 10; x++)
{
    for (int z = 0; z < 10; z++)
    {
        // Place floor tiles
        var pos = new Vector3Data(x * 2.0f, 0, z * 2.0f);
        scene.InstantiatePrefab(tilePrefab, pos, "FloorTiles"); // Parent under a "FloorTiles" folder

        // Place walls on the edges of the grid
        if (x == 0 || x == 9 || z == 0 || z == 9)
        {
            var wallPos = new Vector3Data(x * 2.0f, 1.0f, z * 2.0f);
            scene.InstantiatePrefab(wallPrefab, wallPos, "Walls"); // Parent under a "Walls" folder
        }
    }
}

scene.Save();
Console.WriteLine("Procedurally generated level saved!");
```

### 2. Custom Component Workflows

Efficiently manage and validate custom C# components within your Stride project.

#### Health System Balancing

Adjust properties of custom components across multiple entities based on game design requirements.

```csharp
var project = new StrideProject(@"C:\MyGame");
var scene = project.LoadScene("Level1");

// Find all entities with health components
var healthEntities = scene.FindEntitiesWithComponent("HealthComponent");

// Categorize targets by type for selective balancing
var players = healthEntities.Where(e => e.Name.Contains("Player")).ToList();
var enemies = healthEntities.Where(e => e.Name.Contains("Enemy")).ToList();
var bosses = healthEntities.Where(e => e.Name.Contains("Boss")).ToList();

// Apply balancing changes based on difficulty settings
float difficultyMultiplier = 1.5f; // Example: Hard mode setting

foreach (var enemy in enemies)
{
    var health = enemy.GetComponent("HealthComponent");
    var baseHP = health.Get<float>("MaxHealth");
    var newHP = baseHP * difficultyMultiplier; // Increase enemy health

    health.Set("MaxHealth", newHP);
    health.Set("CurrentHealth", newHP); // Set current health to new max

    Console.WriteLine($"Buffed {enemy.Name}: {currentMax} -> {newHP} HP");
}

foreach (var boss in bosses)
{
    var health = boss.GetComponent("HealthComponent");
    var baseHP = health.Get<float>("MaxHealth");
    var newHP = baseHP * (difficultyMultiplier * 2.0f); // Bosses scale even more

    health.Set("MaxHealth", newHP);
    health.Set("CurrentHealth", newHP);

    // If the boss also has an AttackComponent, increase its damage
    if (boss.HasComponent("AttackComponent"))
    {
        var attack = boss.GetComponent("AttackComponent");
        var baseDamage = attack.Get<float>("Damage");
        attack.Set("Damage", baseDamage * difficultyMultiplier);
    }
    Console.WriteLine($"Buffed {boss.Name}: {currentMax} -> {newHP} HP");
}

scene.Save();
Console.WriteLine($"Balanced {enemies.Count} enemies and {bosses.Count} bosses for hard mode.");
```

#### Custom Component Validation

Implement checks to ensure custom components adhere to game design rules or prevent common errors.

```csharp
var scene = Scene.Load("Level1.sdscene");
var errors = new List<string>();

// Find all entities with a custom SpawnerComponent
var spawners = scene.FindEntitiesWithComponent("SpawnerComponent");

foreach (var spawner in spawners)
{
    var comp = spawner.GetComponent("SpawnerComponent");

    // Validate spawn interval property
    var interval = comp.Get<float>("SpawnInterval");
    if (interval < 1.0f)
    {
        errors.Add($"{spawner.Name}: Spawn interval too low ({interval}s). Must be at least 1.0s.");
    }

    // Validate max spawns property
    var maxSpawns = comp.Get<int>("MaxSpawns");
    if (maxSpawns <= 0)
    {
        errors.Add($"{spawner.Name}: Invalid max spawns ({maxSpawns}). Must be greater than 0.");
    }

    // Validate if a prefab is assigned to "PrefabToSpawn"
    if (comp.Get<AssetRefData>("PrefabToSpawn") == null)
    {
        errors.Add($"{spawner.Name}: No prefab assigned to 'PrefabToSpawn' property.");
    }
}

if (errors.Any())
{
    Console.WriteLine("Validation errors found:");
    errors.ForEach(Console.WriteLine);
}
else
{
    Console.WriteLine($"All {spawners.Count} spawners validated successfully!");
}
```

### 3. Asset Pipeline Automation

Automate routine tasks such as setting properties across numerous assets.

#### Set All Animations to Loop

Ensure that specific animations (e.g., "Idle", "Walk", "Run") are set to loop indefinitely.

```csharp
var project = new StrideProject(@"C:\MyGame");

var animations = project.GetAnimations();

foreach (var animRef in animations.Where(a =>
    a.Name.Contains("Idle") ||
    a.Name.Contains("Walk") ||
    a.Name.Contains("Run")))
{
    var anim = project.LoadAnimation(animRef.Name);

    if (anim.RepeatMode != "LoopInfinite")
    {
        anim.RepeatMode = "LoopInfinite";
        anim.Save();
        Console.WriteLine($"Set {animRef.Name} to loop indefinitely.");
    }
}
```

### 4. Performance Considerations

Optimize your toolkit scripts for efficiency, especially when dealing with large projects.

#### Use Lazy Loading Effectively

Components are loaded from YAML only when accessed. This ensures that you only parse data when it's genuinely needed, significantly improving performance for large scenes or prefabs.

```csharp
// ✓ Efficient - only loads required components
var scene = Scene.Load("HugeScene.sdscene");
var players = scene.FindEntitiesByName("Player*");
foreach (var player in players)
{
    var transform = player.GetTransform(); // The TransformComponent is loaded only when GetTransform() is called
    // ... other operations that only access specific components
}

// ✗ Less Efficient - forces loading of all components initially (via .Components property)
var scene = Scene.Load("HugeScene.sdscene");
foreach (var entity in scene.AllEntities)
{
    foreach (var comp in entity.Components.Values) // Accessing .Components property here loads all components of *this entity*
    {
        // Avoid iterating through all components if you only need a specific one
    }
}
```

#### Batch Similar Operations

Group your file I/O operations and modifications to minimize overhead.

```csharp
// ✓ Efficient: Load the scene once, perform all modifications, then save once.
var scene = Scene.Load("Level1.sdscene");

var enemies = scene.FindEntitiesByName("Enemy_*");
foreach (var enemy in enemies)
{
    // Perform multiple modifications on the enemy entity or its components
    enemy.GetTransform().SetPosition(0, 0, 0);
    enemy.GetComponent("HealthComponent").Set("CurrentHealth", 100f);
}

scene.Save(); // One save operation after all changes are made

// ✗ Inefficient: Repeatedly loading and saving the same scene is slow.
foreach (var enemyName in new[] { "Enemy_1", "Enemy_2", "Enemy_3" })
{
    var scene = Scene.Load("Level1.sdscene"); // Load scene repeatedly
    var enemy = scene.FindEntityByName(enemyName);
    // Modify...
    scene.Save(); // Save scene repeatedly
}
```

### 5. Safety and Error Handling

Implement robust checks to prevent issues and handle unexpected scenarios gracefully.

#### Always Validate Before Accessing

Before attempting to access properties or components, always check if they exist to prevent `NullReferenceException`s.

```csharp
// ✓ Safe
if (entity.HasComponent("HealthComponent"))
{
    var health = entity.GetComponent("HealthComponent");
    var hp = health.Get<float>("CurrentHealth");
    // ... further safe operations
}

// ✗ Unsafe - risks NullReferenceException if "HealthComponent" doesn't exist
var health = entity.GetComponent("HealthComponent");
var hp = health.Get<float>("CurrentHealth");
```

#### Use Null-Coalescing for Defaults

Provide fallback default values when retrieving properties that might be missing or `null`.

```csharp
// Using null-coalescing operator to provide a default speed if "MoveSpeed" is not found
var speed = controller?.Get<float>("MoveSpeed") ?? 5.0f;
var maxHP = health?.Get<float>("MaxHealth") ?? 100.0f;
```

#### Handle File Operations with Try-Catch

Wrap file-related operations in `try-catch` blocks to manage potential `IOException`s or `FileNotFoundException`s.

```csharp
try
{
    scene.Save();
    Console.WriteLine($"Scene saved successfully to {scene.FilePath}");
}
catch (IOException ex)
{
    Console.Error.WriteLine($"ERROR: Failed to save scene {scene.FilePath}. Details: {ex.Message}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"An unexpected error occurred while saving: {ex.Message}");
}
```

### 6. Debugging and Inspection

Tools and techniques for understanding and debugging your scene and component data.

#### Inspect Component Properties

When working with unfamiliar components or debugging, print out all properties of a component to understand its structure.

```csharp
var component = entity.GetComponent("UnknownComponent");
if (component != null)
{
    Console.WriteLine($"Component Type: {component.Type}");
    Console.WriteLine("Properties:");

    foreach (var prop in component.Properties)
    {
        Console.WriteLine($"  {prop.Key}: {prop.Value} ({prop.Value?.GetType().Name})");
    }
}
```

#### Inspect Asset Properties

Similarly, you can inspect all raw properties of an asset for debugging purposes.

```csharp
var material = MaterialAsset.Load("SomeMaterial.sdmat");
var allProps = material.GetAllProperties();

foreach (var prop in allProps)
{
    Console.WriteLine($"{prop.Key}: {prop.Value}");
}
```

### 7. Naming Conventions and Organization

Maintain a clean and searchable project structure.

#### Use Consistent Entity Names

Adopt a clear naming convention for entities, especially when using wildcard searches.

```csharp
// ✓ Good - Easy to query using patterns
"Enemy_Goblin_01", "Enemy_Goblin_02", "Enemy_Orc_01"

var allEnemies = scene.FindEntitiesByName("Enemy_*");
var allGoblins = scene.FindEntitiesByName("Enemy_Goblin_*");

// ✗ Inconsistent - Hard to query and manage
"Goblin1", "enemy_orc", "Orc_Enemy_03"
```

#### Use Folders for Organization

Organize entities within scenes or prefabs into logical folders.

```csharp
// Create entities within specific folders
var enemy = scene.CreateEntity("Goblin_01", "Enemies/Goblins");
var item = scene.CreateEntity("HealthPotion", "Items/Consumables");
```
