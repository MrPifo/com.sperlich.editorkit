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
  honoured. Collections render as a Sperlich card (reorder handle, per-element override bar);
  a `[SerializeReference]` list's **+** opens a type picker and appends a fresh instance. A
  single `[SerializeReference]` field and types with their own `PropertyDrawer` use a themed
  `PropertyField` wrapped with the override bar / Apply-Revert menu.
- The "Script" reference row is hidden by default (like Odin). Bring it back per type with
  `[SInspector(showScript: true)]` or globally via **Tools ▸ Sperlich ▸ SInspector ▸ Show
  Script Field**.

### Layout / formatting attributes

| Attribute | Target | What it does |
|---|---|---|
| `[SRow]` | field | lay adjacent scalar fields on one horizontal line |
| `[SMinMax(min, max)]` | Vector2 / Vector2Int | dual-handle from–to range slider |
| `[Box(label, collapsable, expanded, readOnly)]` | field | wrap this field and the following ones in a box; run ends at the next `[Box]` / `[Header]` / `[EndGroup]`. No nesting. |
| `[EndGroup]` | field | close the current `[Box]` early |
| `[HLine(label, color, style)]` | field | separator line above the field (`Solid` / `Dashed` / `Dotted`), stacks |
| `[SReadOnly]` | field | draw the field disabled |
| `[TintColor(color, background)]` | field | tint the label (and optionally the input background) |
| `[EnumToggleButtons(label)]` | enum field | segmented control (plain) / toggle bar (`[Flags]`) instead of a dropdown |
| `[SuffixLabel(text, overlay)]` | field | grey label after / inside the field; `"$member"` polls a string member |
| `[Unit(custom)]` / `[Unit(unit)]` / `[Unit(unit, displayAs)]` | numeric field | unit suffix, plus an optional read-only converted companion row |
| `[ProgressBar(min, max, color, height, minMember, maxMember, segmented, showValue, percent)]` | int / float | read-only fill bar tracking the value; `percent: true` shows `50%` instead of `value / max` |
| `[Scene(useFullPath)]` | string / int | Build-Settings scene picker (name / path / build index) |
| `[OnValueChanged(method)]` | field | call `()`, `(T new)` or `(T old, T new)` on every change; stacks |
| `[ShowIf(member, values…)]` / `[HideIf(member, values…)]` | field | show / hide the field from a live condition on another member — no values = truthy, one-or-more values = equals-any, `[Flags]` driver = contains |
| `[EnableIf(member, values…)]` / `[DisableIf(member, values…)]` | field | same condition grammar as `[ShowIf]`/`[HideIf]`, but greys the field out instead of hiding it |
| `[Required(message)]` | object reference | red edge flag + inline error while the value is `None` |
| `[InfoBox(message, type, visibleIf)]` | field | info/warning/error help box above the field; stacks; optional live `visibleIf` condition (truthy check) |
| `[Tag(label, color)]` | field | small colored pill next to the label; stacks; `color` is a `TagColor` preset |
| `[Percent(valueMin, valueMax)]` | float | editable 0-100% field; the serialized value stays in `[valueMin, valueMax]` (default 0..1) |
| `[Knob(min, max, diameter)]` / `[Knob(KnobRange, diameter)]` | int / float | rotary dial; `KnobRange` presets cover 0..360°, -180..180°, 0..2π, -π..π, 0..1 |
| `[Stepper(step, min, max)]` | int / float | `[-] [value] [+]`; shift-click steps 10x |
| `[TabGroup(tab, group)]` | field | fields sharing `group` become one tab bar, one tab per distinct `tab` name (first-seen order); fields need not be contiguous. Selected tab persists per type. |
| `[Expandable(defaultExpanded)]` | object reference | chevron unfolds the assigned asset's own inspector inline below the field |

### Methods & live displays

| Attribute | Target | What it does |
|---|---|---|
| `[SButton(label, size, height, anchor, after, before, icon)]` | parameterless method | a button; `after` / `before` pin it next to a field without moving the method |
| `[ButtonGroup(group, label, icon)]` | parameterless method | methods sharing `group` become one segmented button bar |
| `[InlineButton(method, label, icon)]` | field | small button(s) to the right of the field row; stacks |
| `[ShowProperty(label, pollMs)]` | property | read-only live value, polled (default 250 ms), never written |
| `[ShowField(label, pollMs)]` | non-serialized field | same, for a `[NonSerialized]` field |

## Installation

Unity > Window > Package Manager > + > Add package from git URL:

```
https://github.com/MrPifo/com.sperlich.editorkit.git
```
