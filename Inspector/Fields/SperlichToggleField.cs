using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// A <see cref="PillToggle"/> wired to a <c>bool</c> <see cref="SerializedProperty"/>: writes on click,
	/// follows external changes (Undo, multi-edit) via <c>TrackPropertyValue</c>, and dims to a "—" glyph
	/// when the selected objects disagree. Standalone so any Sperlich inspector can drop in a bound pill
	/// without re-wiring it (see also <see cref="SperlichFieldColumn.Property"/>).
	/// </summary>
	public sealed class SperlichToggleField : VisualElement {

		private readonly PillToggle pill;
		private readonly Label mixed;

		public SperlichToggleField(SerializedProperty boolProp) {
			style.flexDirection = FlexDirection.Row;
			style.alignItems = Align.Center;
			// Hand cursor over the toggle, and a click anywhere on this element toggles — the pill's own
			// click is StopPropagation'd, so no double toggle. Keep this element sized to its content
			// (the caller may reset flexGrow) so the cursor/click area matches the visible control.
			style.flexGrow = 0;
			SperlichEditorWidgets.SetHoverCursor(this, MouseCursor.Link);

			void Toggle() {
				boolProp.boolValue = !boolProp.boolValue;
				boolProp.serializedObject.ApplyModifiedProperties();
				Refresh(boolProp);
			}

			pill = new PillToggle(boolProp.boolValue);
			pill.Clicked += Toggle;
			Add(pill);

			RegisterCallback<ClickEvent>(_ => Toggle());

			mixed = new Label("—") {
				pickingMode = PickingMode.Ignore,
				style = { display = DisplayStyle.None, marginLeft = 4, color = SperlichEditorTheme.TextMuted, unityFontStyleAndWeight = FontStyle.Bold }
			};
			Add(mixed);

			Refresh(boolProp);
			this.TrackPropertyValue(boolProp, Refresh);
		}

		private void Refresh(SerializedProperty p) {
			bool m = p.hasMultipleDifferentValues;
			pill.style.opacity = m ? 0.35f : 1f;
			mixed.style.display = m ? DisplayStyle.Flex : DisplayStyle.None;
			pill.SetValue(p.boolValue);
		}
	}
}
