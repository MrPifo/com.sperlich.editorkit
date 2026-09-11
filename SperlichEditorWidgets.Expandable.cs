using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Object field for <c>[Expandable]</c> with a toggle that unfolds the assigned asset's own
		/// inspector inline below it. Uses <see cref="Editor.CreateEditor"/>; falls back to
		/// <see cref="IMGUIContainer"/> for a type that has no UI Toolkit <c>CreateInspectorGUI</c>.</summary>
		public static VisualElement CreateExpandableField(SerializedProperty prop, Type objectType, bool defaultOpen, string label, Color accent) {
			var wrap = new VisualElement();
			var col = new SperlichFieldColumn(150f);

			VisualElement objField = CreateObjectField(prop, objectType, true);
			objField.style.flexGrow = 1;

			// A lone chevron next to an object field reads as "just another foldout" — spell out what it
			// does instead, and border it in the accent color so it visually matches the embedded panel it
			// opens below.
			var arrow = new Label(defaultOpen ? "▾" : "▸") { pickingMode = PickingMode.Ignore, style = { fontSize = 10, color = accent, marginRight = 3 } };
			var toggleText = new Label(defaultOpen ? "Close" : "Edit") { pickingMode = PickingMode.Ignore, style = { fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextSecondary } };
			var toggle = new VisualElement {
				pickingMode = PickingMode.Position,
				style = {
					flexDirection = FlexDirection.Row, alignItems = Align.Center, justifyContent = Justify.Center,
					height = 20, paddingLeft = 7, paddingRight = 8, flexShrink = 0, marginLeft = 6,
					backgroundColor = SperlichEditorTheme.ButtonBg,
					borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
				}
			};
			SetBorderColor(toggle, accent);
			SetRadius(toggle, 3);
			SetHoverCursor(toggle, MouseCursor.Link);
			toggle.Add(arrow);
			toggle.Add(toggleText);
			toggle.RegisterCallback<MouseEnterEvent>(_ => toggle.style.backgroundColor = SperlichEditorTheme.ButtonHoverBg);
			toggle.RegisterCallback<MouseLeaveEvent>(_ => toggle.style.backgroundColor = SperlichEditorTheme.ButtonBg);

			var controlRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };
			controlRow.Add(objField);
			controlRow.Add(toggle);

			wrap.Add(col.Row(label, controlRow));

			// The embedded panel itself: an accent-colored left edge ties it back to the toggle that opened
			// it, and a small labeled header names what's actually being edited inline — the whole point is
			// that this is NOT just another field, it's a second object's inspector living inside this one.
			var body = new VisualElement { style = { display = defaultOpen ? DisplayStyle.Flex : DisplayStyle.None, marginLeft = 16, marginTop = 3, marginBottom = 4 } };
			wrap.Add(body);

			Editor cachedEditor = null;
			UnityEngine.Object cachedTarget = null;
			bool expanded = defaultOpen;

			void RebuildBody() {
				body.Clear();
				UnityEngine.Object target = prop.objectReferenceValue;
				if (target == null) {
					body.Add(new Label("Nothing assigned.") { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, unityFontStyleAndWeight = FontStyle.Italic } });
					return;
				}
				if (cachedTarget != target) {
					if (cachedEditor != null) UnityEngine.Object.DestroyImmediate(cachedEditor);
					try { cachedEditor = Editor.CreateEditor(target); } catch (Exception e) { Debug.LogException(e); cachedEditor = null; }
					cachedTarget = target;
				}
				if (cachedEditor == null) {
					body.Add(new Label("Could not create an inline editor for this asset.") { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted } });
					return;
				}

				VisualElement box = CreateBox(4, SperlichEditorTheme.BorderSubtle);
				box.style.backgroundColor = SperlichEditorTheme.BgStepBody;
				box.style.borderLeftWidth = 3;
				box.style.borderLeftColor = accent;
				box.style.overflow = Overflow.Hidden; // keep the rounded corners clean under the header strip
				box.style.paddingLeft = 0; box.style.paddingRight = 0; box.style.paddingTop = 0; box.style.paddingBottom = 0;

				var header = new VisualElement {
					style = {
						flexDirection = FlexDirection.Row, alignItems = Align.Center,
						backgroundColor = SperlichEditorTheme.BgStep,
						paddingLeft = 8, paddingRight = 8, paddingTop = 3, paddingBottom = 3,
						borderBottomWidth = 1, borderBottomColor = SperlichEditorTheme.BorderSubtle,
					}
				};
				header.Add(new Label("⊞") { pickingMode = PickingMode.Ignore, style = { fontSize = 11, color = accent, marginRight = 5 } });
				header.Add(new Label($"Embedded — {ObjectNames.NicifyVariableName(target.GetType().Name)} · {target.name}") {
					pickingMode = PickingMode.Ignore,
					style = { fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextSecondary, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap },
				});
				box.Add(header);

				var inner = new VisualElement { style = { paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 6 } };
				VisualElement innerGui = null;
				try { innerGui = cachedEditor.CreateInspectorGUI(); } catch (Exception e) { Debug.LogException(e); }
				if (innerGui == null) {
					Editor capturedEditor = cachedEditor;
					innerGui = new IMGUIContainer(() => {
						if (capturedEditor != null && capturedEditor.target != null) capturedEditor.OnInspectorGUI();
					});
				}
				inner.Add(innerGui);
				box.Add(inner);
				body.Add(box);
			}

			void SetExpanded(bool value) {
				expanded = value;
				arrow.text = expanded ? "▾" : "▸";
				toggleText.text = expanded ? "Close" : "Edit";
				body.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
				if (expanded) RebuildBody();
			}

			toggle.RegisterCallback<ClickEvent>(evt => { evt.StopPropagation(); SetExpanded(!expanded); });
			if (defaultOpen) RebuildBody();

			wrap.TrackPropertyValue(prop, _ => { if (expanded) RebuildBody(); });
			wrap.RegisterCallback<DetachFromPanelEvent>(_ => {
				if (cachedEditor != null) { UnityEngine.Object.DestroyImmediate(cachedEditor); cachedEditor = null; }
			});
			return wrap;
		}
	}
}
