# Sperlich EditorKit

Shared editor-UI building blocks for Sperlich tools:

- `SperlichEditorTheme` – common colors / spacing tokens
- `SperlichEditorWidgets` – reusable UIToolkit widgets (cards, value rows, compact fields, …)
- `PillToggle` – segmented pill toggle control

Two assemblies:

- `Sperlich.EditorKit.Attributes` (runtime, all platforms) – just the attribute types
  (`[SInspector]`, …). Each is `[Conditional("UNITY_EDITOR")]`, so a player build drops every
  usage from the IL automatically – game code never needs an `#if UNITY_EDITOR` guard.
- `Sperlich.EditorKit` (editor-only) – the theme, widgets and the auto-inspector engine.

## `[SInspector]` – whole-inspector takeover

Put `[SInspector]` on a `MonoBehaviour` or `ScriptableObject` class:

```csharp
[SInspector]
public class EnemySpawner : MonoBehaviour {
    [Header("Spawning")]
    [SerializeField, Range(0f, 10f)] float spawnRate = 2f;
    [SerializeField] SpawnMode mode;
    [SerializeField] SEvent onSpawned;
}
```

The entire inspector then renders in the Sperlich style (pill toggles, flat dropdowns,
drag-number fields, dark theme), so embedded Sperlich drawers such as `SEvent` blend in
instead of clashing with Unity's default UI.

- A blanket `isFallback` editor drives this – it only runs when a type has **no other**
  `[CustomEditor]`, so `SText`, `FlexContainer`, `Transform`, … keep their own inspectors.
- Without `[SInspector]` the class renders exactly like Unity's default inspector.
- Multi-object editing and prefab overrides (blue bar, bold label, Apply/Revert menu) work
  as usual. `[Header]`, `[Space]`, `[Tooltip]`, `[Range]`, `[TextArea]`/`[Multiline]` are
  honoured. Collections, `[SerializeReference]` and types with their own `PropertyDrawer`
  fall back to a themed `PropertyField`.

Grouping / conditional attributes (`[SBoxGroup]`, `[SFoldout]`, `[SShowIf]`, `[SButton]`, …)
are a planned Phase-2 extension on top of this engine.

## Installation

Unity > Window > Package Manager > + > Add package from git URL:

```
https://github.com/MrPifo/com.sperlich.editorkit.git
```
