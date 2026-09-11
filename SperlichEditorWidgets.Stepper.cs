using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>[-] [value] [+] number field for <c>[Stepper]</c>. Shift-click steps 10x.</summary>
		public static VisualElement CreateStepperField(SerializedProperty prop, float step, float min, float max, Color? accent = null) {
			Color acc = accent ?? SperlichEditorTheme.ButtonAccent;
			bool isInt = prop.propertyType == SerializedPropertyType.Integer;

			float Read() => isInt ? prop.intValue : prop.floatValue;
			void Write(float v) {
				v = Mathf.Clamp(v, min, max);
				if (isInt) prop.intValue = Mathf.RoundToInt(v); else prop.floatValue = v;
				prop.serializedObject.ApplyModifiedProperties();
			}

			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };

			BindableElement field;
			if (isInt) {
				var f = new IntegerField(); f.BindProperty(prop); field = f;
			} else {
				var f = new FloatField(); f.BindProperty(prop); field = f;
			}
			field.style.flexGrow = 1;
			field.style.minWidth = 0;
			field.style.marginLeft = 3;
			field.style.marginRight = 3;
			SperlichFieldColumn.HideInternalLabel(field);
			VisualElement textInput = field.Q("unity-text-input");
			if (textInput != null) { textInput.style.minWidth = 0; textInput.style.unityTextAlign = TextAnchor.MiddleCenter; }

			row.Add(StepButton("-", acc, shift => Write(Read() - step * (shift ? 10f : 1f))));
			row.Add(field);
			row.Add(StepButton("+", acc, shift => Write(Read() + step * (shift ? 10f : 1f))));
			return row;
		}

		private static VisualElement StepButton(string glyph, Color accent, Action<bool> onClick) {
			var b = new VisualElement {
				pickingMode = PickingMode.Position,
				style = {
					width = 22, height = 22, flexShrink = 0, alignItems = Align.Center, justifyContent = Justify.Center,
					backgroundColor = SperlichEditorTheme.ButtonBg,
					borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
				}
			};
			SetBorderColor(b, SperlichEditorTheme.ButtonBorder);
			SetRadius(b, 3);
			SetHoverCursor(b, MouseCursor.Link);
			b.Add(new Label(glyph) { pickingMode = PickingMode.Ignore, style = { fontSize = 13, color = accent, unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter } });
			b.RegisterCallback<MouseEnterEvent>(_ => b.style.backgroundColor = SperlichEditorTheme.ButtonHoverBg);
			b.RegisterCallback<MouseLeaveEvent>(_ => b.style.backgroundColor = SperlichEditorTheme.ButtonBg);
			b.RegisterCallback<ClickEvent>(evt => { evt.StopPropagation(); onClick(evt.shiftKey); });
			return b;
		}
	}
}
