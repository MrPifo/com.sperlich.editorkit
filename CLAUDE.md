# Sperlich EditorKit (`com.sperlich.editorkit`)

Editor-UI toolkit for Unity 6 (UI Toolkit). It gives every Sperlich tool one dark theme, reusable widgets and an attribute-driven auto-inspector. Standalone, reusable package. It is used by BattleTanks but must not depend on it.

## Rules

**Docs (mandatory).** `Documentation~/index.html` is the reference for every attribute. Every change must update it in the same task. Do not report a task as done before the docs match the code.
- New attribute: add an `A(...)` entry with signature, short description, C# sample and live preview.
- Changed or removed attribute (parameters, defaults, behavior, targets, `AllowMultiple`): update or remove its entry.
- New or changed enum value, palette color or theme token: update the Reference section.
- Visual change to a widget: update the matching `.si-*` CSS in the docs.
- Update `README.md` when the change affects what it lists.
- Docs are English, KISS, and must not reference other inspector tools.

**Architecture**
- Two assemblies. `Sperlich.EditorKit.Attributes` is runtime: attribute types and enums only, no `UnityEditor`. `Sperlich.EditorKit` is editor-only: theme, widgets, inspector engine.
- Every attribute is `[Conditional("UNITY_EDITOR")]` with an explicit `[AttributeUsage]`. Game code never needs `#if UNITY_EDITOR`.
- No dependency on game code (`BattleTanks` namespace or assemblies). Keep the public API stable.
- Attribute names get an `S` prefix only when they would clash with Unity or .NET types (`SButton`, `SReadOnly`, `SRow`, `SMinMax`).
- An attribute that takes a color offers a hex string and a `TintColor` overload. `UnityEngine.Color` cannot be an attribute argument.

**UI**
- UI Toolkit only. No IMGUI.
- Build UI with `SperlichEditorWidgets` and `SperlichEditorTheme`. Reuse or extend an existing widget before writing a new one. Never hardcode colors; use theme tokens.
- `SperlichInspector.uss` mirrors the theme colors, because USS cannot read C# constants. Keep both in sync.
- Rows use one shared label column (`SperlichFieldColumn`). Do not use Unity's aligned-field automation.
- Keep multi-object editing and prefab overrides working (`SperlichPrefabOverride`).
- Refresh event-driven with `TrackPropertyValue`. Poll (about 200 to 250 ms) only when the driver is not a serialized field.
- Remember UI state (collapse, selected tab) in `EditorPrefs`, keyed by type and field.

**Code style**
- Tabs, K&R braces, PascalCase types and members, camelCase fields.
- XML doc (`///`) only on public API that needs it, English, short. Inline `//` for notes inside method bodies.
- LINQ is fine in editor code for readability. No LINQ in code that awaits.
- Commit only when asked.

## Structure

| Path | What |
|---|---|
| `Attributes/` | Runtime attribute classes, `SInspectorEnums.cs`, `SDictionary`, `SHashSet` |
| `SperlichEditorTheme.cs` | Color tokens (mirrored in `Inspector/SperlichInspector.uss`) |
| `SperlichEditorWidgets*.cs` | Widgets, one partial file per widget (Button, Knob, Stepper, ProgressBar, Tag, Number, RangeSlider, MinMaxSlider, CollectionList, Expandable, ...) |
| `SperlichFieldColumn.cs` | Label + control rows with one shared label column |
| `PillToggle.cs` | Pill toggle control |
| `Inspector/SperlichInspectorPlan.cs` | Reflection cache: reads attributes into `MemberMeta` once per type |
| `Inspector/SperlichInspectorEngine*.cs` | Builds the inspector: `.cs` rows and collections, `.Members` methods and live values, `.Groups` Box and SubBox, `.Tabs`, `.Decorations` per-row extras and conditions |
| `Inspector/SInspectorEditor.cs` | Fallback editor. Runs only when a type has no other `[CustomEditor]` |
| `Inspector/SCollectionDrawers.cs` | Property drawers for `SDictionary` and `SHashSet` outside `[SInspector]` |
| `Inspector/PropertyDrawerRegistry.cs` | Finds field types that have their own `PropertyDrawer` |
| `Inspector/SperlichPrefabOverride.cs` | Prefab-override bar and Apply/Revert menu |
| `Editor/SInspectorMenu.cs` | `Tools ▸ Sperlich ▸ SInspector ▸ Show Script Field` |
| `Documentation~/index.html` | Interactive attribute docs (self-contained, open in a browser) |

## Adding an attribute

1. Add the class in `Attributes/` (and any enum in `SInspectorEnums.cs`).
2. Read it into a `MemberMeta` field in `SperlichInspectorPlan`.
3. Render it in the matching `SperlichInspectorEngine*.cs`. Put a new control in its own `SperlichEditorWidgets.<Name>.cs`.
4. Update the docs and, if needed, the README.

## What exists

- **Core:** `SInspector`
- **Layout:** `SRow`, `Box`, `EndGroup`, `SubBox`, `EndSubBox`, `TabGroup`, `HLine`, `SMinMax`, `Expandable`
- **Appearance:** `SReadOnly`, `Label`/`LabelText`, `TintColor`, `AccentColor`, `InspectorAccent`, `Tag`, `SuffixLabel`, `Unit`, `InfoBox`
- **Controls:** `EnumToggleButtons`, `Percent`, `Knob`, `Stepper`, `ProgressBar`, `Scene`, `Required`
- **Logic:** `ShowIf`, `HideIf`, `EnableIf`, `DisableIf`, `ShowInPlayMode`/`HideInEditorMode`, `ShowInEditMode`/`HideInPlayMode`, `OnValueChanged`
- **Methods and live values:** `SButton`, `ButtonGroup`, `InlineButton`, `ShowProperty`, `ShowField`
- **Types:** `SDictionary<TKey, TValue>`, `SHashSet<T>`
- **Inspector features:** collections as cards (reorder, remove), `[SerializeReference]` type picker, multi-object editing, prefab overrides, Script-row toggle
