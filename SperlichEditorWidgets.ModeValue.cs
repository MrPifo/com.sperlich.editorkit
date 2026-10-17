using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		// ============================ mode button + mode-switched value ==============================

		/// <summary>One mode of <see cref="CreateModeValueField"/>: menu name, button glyph and tooltip.</summary>
		public readonly struct ValueMode {
			public readonly string Name;
			public readonly string Glyph;
			public readonly string Tooltip;
			public ValueMode(string name, string glyph, string tooltip = null) {
				Name = name;
				Glyph = glyph;
				Tooltip = tooltip;
			}
		}

		/// <summary>Small square mode button. <paramref name="highlighted"/> = boxed accent look (a non-default mode),
		/// otherwise a flat faint glyph.</summary>
		public static Button CreateModeButton(Action onClick, string glyph, bool highlighted, string tooltip = null) {
			var button = new Button(onClick) {
				style = {
					width = 18, height = 18, flexShrink = 0,
					marginLeft = 3, marginRight = 0, marginTop = 0, marginBottom = 0,
					paddingLeft = 0, paddingRight = 0, paddingTop = 0, paddingBottom = 0,
					fontSize = 11, unityTextAlign = TextAnchor.MiddleCenter,
					borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
				}
			};
			SetModeButton(button, glyph, highlighted, tooltip);
			button.RegisterCallback<MouseEnterEvent>(_ => {
				if (!button.ClassListContains(ModeButtonActiveClass)) button.style.color = SperlichEditorTheme.TextPrimary;
			});
			button.RegisterCallback<MouseLeaveEvent>(_ => {
				if (!button.ClassListContains(ModeButtonActiveClass)) button.style.color = SperlichEditorTheme.TextFaint;
			});
			return button;
		}

		const string ModeButtonActiveClass = "sperlich-mode-button--active";

		/// <summary>Updates glyph, look and tooltip of a <see cref="CreateModeButton"/>.</summary>
		public static void SetModeButton(Button button, string glyph, bool highlighted, string tooltip = null) {
			button.text = glyph;
			button.tooltip = tooltip ?? "";
			button.EnableInClassList(ModeButtonActiveClass, highlighted);
			Color accent = SperlichEditorTheme.ButtonAccent;
			Color border = highlighted ? new Color(accent.r, accent.g, accent.b, 0.45f) : Color.clear;
			button.style.backgroundColor = highlighted ? SperlichEditorTheme.ButtonBg : Color.clear;
			button.style.color = highlighted ? accent : SperlichEditorTheme.TextFaint;
			float width = highlighted ? 1 : 0;
			button.style.borderLeftWidth = width;
			button.style.borderRightWidth = width;
			button.style.borderTopWidth = width;
			button.style.borderBottomWidth = width;
			button.style.borderLeftColor = border;
			button.style.borderRightColor = border;
			button.style.borderTopColor = border;
			button.style.borderBottomColor = border;
		}

		/// <summary>
		/// A value with several edit modes (e.g. Constant / Range). <paramref name="buildBody"/> builds the controls
		/// for a mode index; they are rebuilt when <paramref name="modeProp"/> changes (also on Undo). A mode button
		/// after the controls and the row's right-click menu ("Mode/...") switch the mode. <paramref name="onModeChange"/>
		/// runs before the change is applied (old index, new index) to carry values over. Mode 0 is the default mode
		/// and shows a flat button; other modes show it highlighted.
		/// </summary>
		public static VisualElement CreateModeValueField(SerializedProperty modeProp, ValueMode[] modes, Func<int, VisualElement> buildBody,
			Action<int, int> onModeChange = null) {
			var root = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, minWidth = 0 } };
			var body = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, minWidth = 0 } };
			root.Add(body);

			SerializedObject so = modeProp.serializedObject;
			string modePath = modeProp.propertyPath;
			int shownMode = int.MinValue;
			Button button = null;

			int CurrentMode() {
				SerializedProperty p = so.FindProperty(modePath);
				if (p == null || p.hasMultipleDifferentValues) return -1;
				int index = p.propertyType == SerializedPropertyType.Enum ? p.enumValueIndex : p.intValue;
				return Mathf.Clamp(index, 0, modes.Length - 1);
			}

			void SetMode(int index) {
				int old = CurrentMode();
				if (index == old) return;
				so.Update();
				onModeChange?.Invoke(old, index);
				SerializedProperty p = so.FindProperty(modePath);
				if (p == null) return;
				if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = index;
				else p.intValue = index;
				so.ApplyModifiedProperties();
			}

			void ShowMenu(DropdownMenu menu) {
				int current = CurrentMode();
				for (int i = 0; i < modes.Length; i++) {
					int captured = i;
					menu.AppendAction("Mode/" + modes[i].Name, _ => SetMode(captured),
						root.enabledInHierarchy ? (current == i ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal) : DropdownMenuAction.Status.Disabled);
				}
			}

			void Rebuild() {
				int mode = CurrentMode();
				if (mode == shownMode) return;
				shownMode = mode;
				body.Clear();
				body.Add(buildBody(Mathf.Max(mode, 0)));
				if (mode < 0) {
					SetModeButton(button, "—", false, "Mixed modes");
				} else {
					ValueMode m = modes[mode];
					SetModeButton(button, m.Glyph, mode > 0, (m.Tooltip ?? m.Name) + "\nClick or right-click to change the mode.");
				}
			}

			button = CreateModeButton(() => {
				var menu = new GenericMenu();
				int current = CurrentMode();
				for (int i = 0; i < modes.Length; i++) {
					int captured = i;
					menu.AddItem(new GUIContent(modes[i].Name), current == i, () => SetMode(captured));
				}
				menu.ShowAsContext();
			}, modes[0].Glyph, false);
			root.Add(button);

			root.RegisterCallback<ContextualMenuPopulateEvent>(evt => {
				evt.menu.AppendSeparator();
				ShowMenu(evt.menu);
			});
			root.AddManipulator(new ContextualMenuManipulator(_ => { }));

			Rebuild();
			root.TrackPropertyValue(modeProp, _ => Rebuild());
			return root;
		}
	}
}
