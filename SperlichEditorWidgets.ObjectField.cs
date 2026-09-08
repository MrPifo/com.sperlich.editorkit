using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>
		/// A bound <see cref="ObjectField"/> with a small grey "×" at the far right that clears the reference
		/// in one click — Unity's own field makes nulling a reference needlessly fiddly. The × only shows
		/// while the field actually holds something.
		/// </summary>
		public static VisualElement CreateObjectField(SerializedProperty objProp, Type objectType, bool allowSceneObjects) {
			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };

			var field = new ObjectField {
				objectType = objectType != null && typeof(UnityEngine.Object).IsAssignableFrom(objectType) ? objectType : typeof(UnityEngine.Object),
				allowSceneObjects = allowSceneObjects,
				style = { flexGrow = 1, flexShrink = 1, minWidth = 0 },
			};
			field.BindProperty(objProp);
			SperlichFieldColumn.HideInternalLabel(field);

			var clear = new Label("×") {
				tooltip = "Clear (set to None)",
				pickingMode = PickingMode.Position,
				style = {
					width = 16, flexShrink = 0, marginLeft = 2,
					unityTextAlign = TextAnchor.MiddleCenter, fontSize = 13,
					unityFontStyleAndWeight = FontStyle.Bold,
					color = SperlichEditorTheme.TextMuted,
				}
			};
			SetHoverCursor(clear, MouseCursor.Link);
			clear.RegisterCallback<MouseEnterEvent>(_ => clear.style.color = SperlichEditorTheme.BadgeDangerBg);
			clear.RegisterCallback<MouseLeaveEvent>(_ => clear.style.color = SperlichEditorTheme.TextMuted);
			clear.RegisterCallback<ClickEvent>(evt => {
				evt.StopPropagation();
				objProp.objectReferenceValue = null;
				objProp.serializedObject.ApplyModifiedProperties();
			});

			void Sync() => clear.style.display = objProp.objectReferenceValue != null ? DisplayStyle.Flex : DisplayStyle.None;
			Sync();
			row.TrackPropertyValue(objProp, _ => Sync());

			row.Add(field);
			row.Add(clear);
			return row;
		}
	}
}
