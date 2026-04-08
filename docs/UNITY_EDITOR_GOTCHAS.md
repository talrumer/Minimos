# Unity Editor Scripting — Gotchas & Lessons Learned

> Hard-won knowledge from building Minimos. Read this before writing ANY Editor script.

---

## 🔧 SerializedObject & Property Names

### Private [SerializeField] fields → use camelCase
```csharp
[SerializeField] private int teamIndex;  // → FindProperty("teamIndex") ✅
```

### Public fields → use PascalCase (exact field name)
```csharp
public AnnouncerEvent EventType;  // → FindPropertyRelative("EventType") ✅
                                  // → FindPropertyRelative("eventType") ❌ returns null!
```

### Nested properties → use dot path
```csharp
so.FindProperty("NetworkConfig.NetworkTransport")  // ✅
so.FindProperty("NetworkConfig.Prefabs.NetworkPrefabsLists")  // ✅
```

### Always check for null — silent failure is the default
```csharp
var prop = so.FindProperty("myField");
if (prop != null)
    prop.objectReferenceValue = value;
else
    Debug.LogWarning($"⚠️ Property 'myField' not found on {so.targetObject.GetType().Name}");
```

---

## 🏗️ Prefab vs Scene Object Wiring

### ❌ Components on the SAME prefab can't reference each other via SerializedObject
```csharp
// This WILL NOT persist after SaveAsPrefabAsset:
var netManager = go.AddComponent<NetworkManager>();
var transport = go.AddComponent<UnityTransport>();
var so = new SerializedObject(netManager);
so.FindProperty("NetworkConfig.NetworkTransport").objectReferenceValue = transport;  // ❌ Lost on save
```

### ✅ Solution: Create as scene object instead of prefab
Self-references (component A → component B on same GameObject) work fine on scene objects. If the object needs to be in a scene anyway (like GameBootstrap), don't make it a prefab.

### ✅ Alternative: Save prefab first, then load and wire
```csharp
PrefabUtility.SaveAsPrefabAsset(go, path);
Object.DestroyImmediate(go);

var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
var so = new SerializedObject(prefab.GetComponent<MyComponent>());
// Wire references to OTHER assets (not same-prefab components)
so.FindProperty("someAssetRef").objectReferenceValue = someAsset;
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssets();
```

**This works for references to external assets (ScriptableObjects, other prefabs) but NOT for references to components on the same prefab.**

---

## 🌐 NetworkManager Specifics

### AddComponent<NetworkManager> auto-initializes with DefaultNetworkPrefabs
The project's `DefaultNetworkPrefabs.asset` (with `IsDefault: true`) is auto-assigned. SerializedObject changes get overwritten by this initialization.

### ✅ Use the public API AFTER AddComponent to override
```csharp
var netManager = go.AddComponent<NetworkManager>();
// ... later, after initialization ...
netManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
netManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(myList);
EditorUtility.SetDirty(netManager);
```

### NetworkPrefabsList assets must be created and saved before referencing
```csharp
var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
AssetDatabase.CreateAsset(list, path);
AssetDatabase.SaveAssets();
list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(path);  // Reload!
list.Add(new NetworkPrefab { Prefab = myPrefab });
EditorUtility.SetDirty(list);
AssetDatabase.SaveAssets();
```

---

## 📷 Cinemachine 3.x API

### CinemachineFollow damping is NOT a direct property
```csharp
// ❌ Wrong (Cinemachine 2.x API)
followComp.Damping = new Vector3(0.5f, 0.5f, 0.5f);

// ✅ Correct (Cinemachine 3.x)
followComp.TrackerSettings.PositionDamping = new Vector3(0.5f, 0.5f, 0.5f);
```

### CinemachineRotationComposer.Damping IS a direct Vector2
```csharp
rotComposer.Damping = new Vector2(2f, 2f);  // ✅ This one works directly
```

---

## 🔤 Namespace Collisions

### `Minimos.Camera` collides with `UnityEngine.Camera`
```csharp
// ❌ Ambiguous
Camera.main  // Resolves to Minimos.Camera.main → compile error

// ✅ Fully qualify
UnityEngine.Camera.main

// ✅ Or use alias at top of file
using MinimosCamera = Minimos.Camera;
```

### `Minimos.Input` collides with `UnityEngine.Input`
```csharp
// ❌ Ambiguous
Input.GetKeyDown(KeyCode.Space)

// ✅ Fully qualify
UnityEngine.Input.GetKeyDown(KeyCode.Space)
```

---

## 🔊 AssetDatabase.FindAssets

### Searches ALL subfolders recursively
```csharp
// This finds clips in Android/, Normal/, Normal (fx)/, etc.
AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Casual Game Announcer" });
```

### Paths with spaces and special chars work fine
Unity handles `Assets/Casual Game Announcer/Normal (fx)/vo_commence_Let's_Go_01.wav` correctly.

### Always reload assets after CreateAsset + SaveAssets
```csharp
AssetDatabase.CreateAsset(obj, path);
AssetDatabase.SaveAssets();
obj = AssetDatabase.LoadAssetAtPath<MyType>(path);  // Reload from disk!
```

---

## 🎮 Netcode for GameObjects

### string[] can't be serialized in RPCs
```csharp
// ❌ Netcode ILPP error
[ClientRpc]
void MyRpc(string[] names) { }

// ✅ Pass individual strings
[ClientRpc]
void MyRpc(int count, string name0, string name1, string name2) { }
```

### NetworkVariable and RPCs require specific types
Always check Netcode docs for serializable types. Custom structs need `INetworkSerializable`.

---

## 🏛️ General Editor Script Patterns

### Mark scene dirty after modifications
```csharp
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
```

### Use Undo for scene objects (so Ctrl+Z works)
```csharp
var go = new GameObject("MyObject");
Undo.RegisterCreatedObjectUndo(go, "Create MyObject");
```

### Use EditorUtility.SetDirty for assets modified via public API
```csharp
myScriptableObject.somePublicField = newValue;
EditorUtility.SetDirty(myScriptableObject);
AssetDatabase.SaveAssets();
```

### EnsureDirectory recursive helper (Unity needs parent folders first)
```csharp
private static void EnsureDirectory(string path)
{
    if (!AssetDatabase.IsValidFolder(path))
    {
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (parent != null && !AssetDatabase.IsValidFolder(parent))
            EnsureDirectory(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
```

### Progress bar for long operations
```csharp
EditorUtility.DisplayProgressBar("Title", "Step description...", 0.5f);
// ... work ...
EditorUtility.ClearProgressBar();
```
