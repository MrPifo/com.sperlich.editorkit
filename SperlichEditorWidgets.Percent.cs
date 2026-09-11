using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Editable 0-100% slider+field for <c>[Percent]</c>: the underlying float stays in
		/// <paramref name="min"/>..<paramref name="max"/>, only the displayed/typed number is remapped.</summary>
		public static VisualElement CreatePercentField(SerializedProperty prop, float min, float max, Color? accent = null) {
			float range = Mathf.Max(1e-5f, max - min);
			float ToPercent(float v) => (v - min) / range * 100f;
			float FromPercent(float p) => min + p / 100f * range;

			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };
			VisualElement grip = CreateDragGrip();

			var field = new FloatField { style = { flexGrow = 1, minWidth = 0 } };
			SperlichFieldColumn.HideInternalLabel(field);
			VisualElement textInput = field.Q("unity-text-input");
			if (textInput != null) textInput.style.minWidth = 0;

			float Read() => prop.propertyType == SerializedPropertyType.Integer ? prop.intValue : prop.floatValue;
			field.SetValueWithoutNotify(Mathf.Clamp(ToPercent(Read()), 0f, 100f));

			var dragger = new FieldMouseDragger<float>(field);
			dragger.SetDragZone(grip);

			field.RegisterValueChangedCallback(evt => {
				float clamped = Mathf.Clamp(evt.newValue, 0f, 100f);
				if (!Mathf.Approximately(clamped, evt.newValue)) field.SetValueWithoutNotify(clamped);
				prop.floatValue = FromPercent(clamped);
				prop.serializedObject.ApplyModifiedProperties();
			});

			row.Add(grip);
			row.Add(field);
			row.Add(new Label("%") { pickingMode = PickingMode.Ignore, style = { fontSize = 11, color = SperlichEditorTheme.TextMuted, marginLeft = 3, flexShrink = 0 } });
			row.TrackPropertyValue(prop, p => field.SetValueWithoutNotify(Mathf.Clamp(ToPercent(Read()), 0f, 100f)));
			return row;
		}
	}
}
