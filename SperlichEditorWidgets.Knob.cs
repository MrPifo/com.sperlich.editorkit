using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Rotary dial for <c>[Knob]</c>: drag around the circle to set a number in
		/// <paramref name="min"/>..<paramref name="max"/>; the field to the right stays typeable.</summary>
		public static VisualElement CreateKnobField(SerializedProperty prop, float min, float max, float diameter, Color? accent = null) {
			Color acc = accent ?? SperlichEditorTheme.ButtonAccent;
			bool isInt = prop.propertyType == SerializedPropertyType.Integer;
			float range = Mathf.Max(1e-5f, max - min);

			float Read() => isInt ? prop.intValue : prop.floatValue;
			void Write(float v) {
				v = Mathf.Clamp(v, min, max);
				if (isInt) prop.intValue = Mathf.RoundToInt(v); else prop.floatValue = v;
				prop.serializedObject.ApplyModifiedProperties();
			}
			float ValueToDeg(float v) => Mathf.Clamp01((v - min) / range) * 360f;
			float DegToValue(float deg) => min + deg / 360f * range;
			float Radius() => diameter * 0.5f - 3f;

			var root = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };
			var pad = new VisualElement { pickingMode = PickingMode.Position, style = { width = diameter, height = diameter, flexShrink = 0, marginRight = 8 } };
			SetHoverCursor(pad, MouseCursor.MoveArrow);

			pad.generateVisualContent += mgc => {
				Painter2D g = mgc.painter2D;
				Vector2 c = new Vector2(diameter * 0.5f, diameter * 0.5f);
				float r = Radius();
				if (r <= 0f) return;

				g.lineWidth = 1.5f;
				g.strokeColor = SperlichEditorTheme.BorderStrong;
				g.BeginPath();
				g.Arc(c, r, 0f, 360f);
				g.Stroke();

				float deg = ValueToDeg(Read());
				if (deg > 0.01f) {
					g.lineWidth = 3f;
					g.strokeColor = acc;
					g.BeginPath();
					g.Arc(c, r, -90f, deg - 90f);
					g.Stroke();
				}

				float rad = (deg - 90f) * Mathf.Deg2Rad;
				Vector2 h = c + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
				g.fillColor = acc;
				g.BeginPath();
				g.Arc(h, 4f, 0f, 360f);
				g.Fill();
			};

			void SetFromLocal(Vector2 local) {
				Vector2 c = new Vector2(diameter * 0.5f, diameter * 0.5f);
				Vector2 d = local - c;
				if (d.sqrMagnitude < 1e-6f) return;
				float deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f;
				deg = (deg + 360f) % 360f;
				Write(DegToValue(deg));
				pad.MarkDirtyRepaint();
			}

			bool dragging = false;
			pad.RegisterCallback<PointerDownEvent>(e => {
				dragging = true;
				pad.CapturePointer(e.pointerId);
				SetFromLocal((Vector2)e.localPosition);
				e.StopPropagation();
			});
			pad.RegisterCallback<PointerMoveEvent>(e => { if (dragging) SetFromLocal((Vector2)e.localPosition); });
			pad.RegisterCallback<PointerUpEvent>(e => { if (dragging) { dragging = false; pad.ReleasePointer(e.pointerId); } });
			pad.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);

			BindableElement field;
			if (isInt) { var f = new IntegerField(); f.BindProperty(prop); field = f; }
			else { var f = new FloatField(); f.BindProperty(prop); field = f; }
			field.style.flexGrow = 1;
			field.style.minWidth = 0;
			SperlichFieldColumn.HideInternalLabel(field);

			root.Add(pad);
			root.Add(field);
			root.TrackPropertyValue(prop, _ => pad.MarkDirtyRepaint());
			return root;
		}
	}
}
