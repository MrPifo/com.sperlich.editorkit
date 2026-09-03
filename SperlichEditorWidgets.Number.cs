using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		// ============================ drag-to-scrub number field ==============================

		/// <summary>Zahlenfeld mit kleinem Ziehgriff (6-Punkt-Icon) links: rechts/links ziehen ändert den Wert,
		/// wie Unitys klassischer Label-Drag. Für Float- und Int-Properties. Das Eingabefeld selbst bleibt
		/// normal tippbar.</summary>
		public static VisualElement CreateDragNumberField(SerializedProperty prop, float sensitivity = 1f, float min = float.MinValue, float max = float.MaxValue) {
			bool isInt = prop.propertyType == SerializedPropertyType.Integer;

			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };

			VisualElement grip = MakeDragGrip();

			BindableElement input;
			if (isInt) {
				var f = new IntegerField();
				f.BindProperty(prop);
				if (min > int.MinValue || max < int.MaxValue) {
					f.RegisterValueChangedCallback(evt => {
						int clamped = Mathf.Clamp(evt.newValue, (int)min, (int)max);
						if (clamped != evt.newValue) f.value = clamped;
					});
				}
				var dragger = new FieldMouseDragger<int>(f);
				dragger.SetDragZone(grip);
				input = f;
			} else {
				var f = new FloatField();
				f.BindProperty(prop);
				if (min > float.MinValue || max < float.MaxValue) {
					f.RegisterValueChangedCallback(evt => {
						float clamped = Mathf.Clamp(evt.newValue, min, max);
						if (clamped != evt.newValue) f.value = clamped;
					});
				}
				var dragger = new FieldMouseDragger<float>(f);
				dragger.SetDragZone(grip);
				input = f;
			}
			input.style.flexGrow = 1;
			input.style.marginLeft = 2;
			SperlichFieldColumn.HideInternalLabel(input);

			row.Add(grip);
			row.Add(input);
			return row;
		}

		/// <summary>Zahlenfeld mit Ziehgriff links und kleinen Rauf/Runter-Schritt-Pfeilen (▲/▼) rechts für 1-Klick-Inkrementierung.</summary>
		public static VisualElement CreateSteppedNumberField(SerializedProperty prop, int step = 1, int min = int.MinValue, int max = int.MaxValue) {
			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };

			VisualElement grip = MakeDragGrip();

			var input = new IntegerField();
			input.BindProperty(prop);
			if (min > int.MinValue || max < int.MaxValue) {
				input.RegisterValueChangedCallback(evt => {
					int clamped = Mathf.Clamp(evt.newValue, min, max);
					if (clamped != evt.newValue) input.value = clamped;
				});
			}
			var dragger = new FieldMouseDragger<int>(input);
			dragger.SetDragZone(grip);
			input.style.flexGrow = 1;
			input.style.marginLeft = 2;
			input.style.marginRight = 2;
			SperlichFieldColumn.HideInternalLabel(input);

			var spinnerCol = new VisualElement {
				style = {
					width = 14,
					height = 18,
					flexDirection = UnityEngine.UIElements.FlexDirection.Column,
					justifyContent = Justify.Center,
					alignItems = Align.Center,
					backgroundColor = SperlichEditorTheme.ButtonBg,
					borderTopWidth = 1,
					borderBottomWidth = 1,
					borderLeftWidth = 1,
					borderRightWidth = 1,
					borderTopColor = SperlichEditorTheme.ButtonBorder,
					borderBottomColor = SperlichEditorTheme.ButtonBorder,
					borderLeftColor = SperlichEditorTheme.ButtonBorder,
					borderRightColor = SperlichEditorTheme.ButtonBorder,
					borderTopLeftRadius = 2,
					borderTopRightRadius = 2,
					borderBottomLeftRadius = 2,
					borderBottomRightRadius = 2
				}
			};

			var upBtn = new Label("▴") {
				pickingMode = PickingMode.Position,
				style = {
					fontSize = 9,
					unityTextAlign = TextAnchor.MiddleCenter,
					color = SperlichEditorTheme.TextMuted,
					height = 10,
					width = 14,
					paddingTop = 0,
					paddingBottom = 0
				}
			};
			SetHoverCursor(upBtn, MouseCursor.Link);
			upBtn.RegisterCallback<MouseEnterEvent>(_ => upBtn.style.color = SperlichEditorTheme.TextPrimary);
			upBtn.RegisterCallback<MouseLeaveEvent>(_ => upBtn.style.color = SperlichEditorTheme.TextMuted);
			upBtn.RegisterCallback<ClickEvent>(_ => {
				prop.intValue = Mathf.Clamp(prop.intValue + step, min, max);
				prop.serializedObject.ApplyModifiedProperties();
			});

			var downBtn = new Label("▾") {
				pickingMode = PickingMode.Position,
				style = {
					fontSize = 9,
					unityTextAlign = TextAnchor.MiddleCenter,
					color = SperlichEditorTheme.TextMuted,
					height = 10,
					width = 14,
					paddingTop = 0,
					paddingBottom = 0
				}
			};
			SetHoverCursor(downBtn, MouseCursor.Link);
			downBtn.RegisterCallback<MouseEnterEvent>(_ => downBtn.style.color = SperlichEditorTheme.TextPrimary);
			downBtn.RegisterCallback<MouseLeaveEvent>(_ => downBtn.style.color = SperlichEditorTheme.TextMuted);
			downBtn.RegisterCallback<ClickEvent>(_ => {
				prop.intValue = Mathf.Clamp(prop.intValue - step, min, max);
				prop.serializedObject.ApplyModifiedProperties();
			});

			spinnerCol.Add(upBtn);
			spinnerCol.Add(downBtn);

			row.Add(grip);
			row.Add(input);
			row.Add(spinnerCol);
			return row;
		}

		/// <summary>Kleines 2×3-Punkte-Ziehgriff-Icon (Sperlich-Stil), gezeichnet mit Painter2D — kein Font-Glyph.</summary>
		private static VisualElement MakeDragGrip() {
			var g = new VisualElement { pickingMode = PickingMode.Position };
			g.style.width = 12;
			g.style.minWidth = 12;
			g.style.flexShrink = 0;
			g.style.alignSelf = Align.Stretch;
			SetHoverCursor(g, MouseCursor.SlideArrow);

			Color idle = SperlichEditorTheme.TextMuted;
			Color hot = SperlichEditorTheme.TextPrimary;
			Color[] tint = { idle };

			g.generateVisualContent += mgc => {
				Painter2D p = mgc.painter2D;
				p.fillColor = tint[0];
				float h = g.contentRect.height;
				if (float.IsNaN(h) || h <= 0f) h = 18f;
				float midY = h * 0.5f;
				float[] xs = { 4f, 8f };
				float[] ys = { midY - 4f, midY, midY + 4f };
				foreach (float x in xs) {
					foreach (float y in ys) {
						p.BeginPath();
						p.Arc(new Vector2(x, y), 1.1f, 0f, 360f);
						p.Fill();
					}
				}
			};
			g.RegisterCallback<MouseEnterEvent>(_ => { tint[0] = hot; g.MarkDirtyRepaint(); });
			g.RegisterCallback<MouseLeaveEvent>(_ => { tint[0] = idle; g.MarkDirtyRepaint(); });
			return g;
		}

		/// <summary>True, wenn das Feld hinter <paramref name="prop"/> ein <see cref="RangeAttribute"/> trägt
		/// (dann lieber Unitys Slider behalten statt es zum Drag-Zahlenfeld zu machen).</summary>
		public static bool PropertyHasRange(SerializedProperty prop) {
			return TryGetRange(prop, out _, out _);
		}

		/// <summary>Ermittelt min/max, falls das Feld hinter <paramref name="prop"/> ein <see cref="RangeAttribute"/> trägt.</summary>
		public static bool TryGetRange(SerializedProperty prop, out float min, out float max) {
			min = 0f; max = 1f;
			if (prop?.serializedObject?.targetObject == null) return false;
			Type t = prop.serializedObject.targetObject.GetType();
			while (t != null && t != typeof(object)) {
				FieldInfo fi = t.GetField(prop.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (fi != null && fi.GetCustomAttribute(typeof(RangeAttribute)) is RangeAttribute range) {
					min = range.min;
					max = range.max;
					return true;
				}
				t = t.BaseType;
			}
			return false;
		}

		// ============================ radial Vector2 (shadow offset) ==========================

		/// <summary>Runder Offset-Regler für ein Vector2-Property: der Anker im Kreis lässt sich frei drehen
		/// und verschieben. Auf der Oberfläche steht KEIN Vector2, sondern ein <b>Winkelfeld (-180..180°)</b>
		/// und ein <b>Offset-Feld</b> (0..<paramref name="maxMagnitude"/>). Winkel 0° = rechts, +90° = oben.</summary>
		public static VisualElement CreateRadialVector2Field(SerializedProperty vec2Prop, float maxMagnitude, Color? accent = null, float diameter = 42f) {
			Color acc = accent ?? SperlichEditorTheme.ButtonAccent;
			SerializedProperty xP = vec2Prop.FindPropertyRelative("x");
			SerializedProperty yP = vec2Prop.FindPropertyRelative("y");

			var root = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };

			var pad = new VisualElement { pickingMode = PickingMode.Position };
			pad.style.width = diameter;
			pad.style.height = diameter;
			pad.style.flexShrink = 0;
			SetHoverCursor(pad, MouseCursor.MoveArrow);

			float Radius() => diameter * 0.5f - 3f;

			float lastAngleDeg = 0f;
			Vector2 initV = new Vector2(xP.floatValue, yP.floatValue);
			if (initV.sqrMagnitude > 1e-10f) {
				lastAngleDeg = Mathf.Atan2(initV.y, initV.x) * Mathf.Rad2Deg;
			}

			float CurAngle() {
				Vector2 v = new Vector2(xP.floatValue, yP.floatValue);
				if (v.sqrMagnitude > 1e-10f) {
					lastAngleDeg = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
				}
				return lastAngleDeg;
			}
			float CurDist() => new Vector2(xP.floatValue, yP.floatValue).magnitude;

			void WritePolar(float angleDeg, float dist) {
				lastAngleDeg = angleDeg;
				dist = Mathf.Clamp(dist, 0f, maxMagnitude);
				float rad = angleDeg * Mathf.Deg2Rad;
				xP.floatValue = Mathf.Cos(rad) * dist;
				yP.floatValue = Mathf.Sin(rad) * dist;
				vec2Prop.serializedObject.ApplyModifiedProperties();
				pad.MarkDirtyRepaint();
			}

			pad.generateVisualContent += mgc => {
				Painter2D g = mgc.painter2D;
				Vector2 c = new Vector2(diameter * 0.5f, diameter * 0.5f);
				float r = Radius();

				g.lineWidth = 1f;
				g.strokeColor = SperlichEditorTheme.BorderStrong;
				g.BeginPath();
				g.Arc(c, r, 0f, 360f);
				g.Stroke();

				g.strokeColor = SperlichEditorTheme.BorderSubtle;
				g.BeginPath();
				g.MoveTo(new Vector2(c.x - r, c.y));
				g.LineTo(new Vector2(c.x + r, c.y));
				g.MoveTo(new Vector2(c.x, c.y - r));
				g.LineTo(new Vector2(c.x, c.y + r));
				g.Stroke();

				Vector2 v = new Vector2(xP.floatValue, yP.floatValue);
				float mag = Mathf.Clamp01(v.magnitude / Mathf.Max(1e-4f, maxMagnitude));
				float rad = (v.sqrMagnitude > 1e-8f ? Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg : lastAngleDeg) * Mathf.Deg2Rad;
				Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
				Vector2 h = c + new Vector2(dir.x, -dir.y) * (mag * r);

				g.lineWidth = 1.5f;
				g.strokeColor = acc;
				g.BeginPath();
				g.MoveTo(c);
				g.LineTo(h);
				g.Stroke();

				g.fillColor = acc;
				g.BeginPath();
				g.Arc(h, 4f, 0f, 360f);
				g.Fill();
			};

			var (angleField, syncAngle) = MakeVirtualDragNumber("Angle", CurAngle, a => WritePolar(Mathf.Repeat(a + 180f, 360f) - 180f, CurDist()), 1.0f, "°");
			var (distField, syncDist) = MakeVirtualDragNumber("Offset", CurDist, d => WritePolar(CurAngle(), d), 0.004f, null);

			void SetFromLocal(Vector2 local) {
				Vector2 c = new Vector2(diameter * 0.5f, diameter * 0.5f);
				float r = Radius();
				Vector2 d = local - c;
				d.y = -d.y; // Bildschirm runter -> Wert hoch
				if (d.sqrMagnitude > 1e-6f) {
					lastAngleDeg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
				}
				float m = d.magnitude / Mathf.Max(1e-4f, r);
				if (m > 1f) d /= m; // auf den Kreis begrenzen
				Vector2 val = d / r * maxMagnitude;
				xP.floatValue = val.x;
				yP.floatValue = val.y;
				vec2Prop.serializedObject.ApplyModifiedProperties();
				pad.MarkDirtyRepaint();
				syncAngle();
				syncDist();
			}

			bool drag = false;
			pad.RegisterCallback<PointerDownEvent>(e => {
				drag = true;
				pad.CapturePointer(e.pointerId);
				SetFromLocal((Vector2)e.localPosition);
				e.StopPropagation();
			});
			pad.RegisterCallback<PointerMoveEvent>(e => { if (drag) SetFromLocal((Vector2)e.localPosition); });
			pad.RegisterCallback<PointerUpEvent>(e => { if (drag) { drag = false; pad.ReleasePointer(e.pointerId); } });
			pad.RegisterCallback<PointerCaptureOutEvent>(_ => drag = false);

			var fields = new VisualElement { style = { flexGrow = 1, marginLeft = 8 } };
			fields.Add(angleField);
			fields.Add(distField);

			root.Add(pad);
			root.Add(fields);
			root.TrackPropertyValue(vec2Prop, _ => { pad.MarkDirtyRepaint(); syncAngle(); syncDist(); });
			return root;
		}

		/// <summary>Kleines Drag-Zahlenfeld (Griff + Feld), das NICHT an ein SerializedProperty gebunden ist,
		/// sondern über <paramref name="get"/>/<paramref name="set"/> läuft (für abgeleitete Werte wie Winkel /
		/// Abstand). Die zurückgegebene Action schreibt den aktuellen <paramref name="get"/>-Wert ins Feld.</summary>
		private static (VisualElement element, Action sync) MakeVirtualDragNumber(string caption, Func<float> get, Action<float> set, float speed, string suffix) {
			var wrap = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center, marginBottom = 1 } };

			var cap = new Label(caption) { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginRight = 5, flexShrink = 0, unityTextAlign = TextAnchor.MiddleLeft } };
			VisualElement grip = MakeDragGrip();
			var field = new FloatField { style = { flexGrow = 1 } };
			SperlichFieldColumn.HideInternalLabel(field);
			field.SetValueWithoutNotify(get());

			var dragger = new FieldMouseDragger<float>(field);
			dragger.SetDragZone(grip);

			void Sync() => field.SetValueWithoutNotify(get());

			field.RegisterValueChangedCallback(e => { set(e.newValue); Sync(); });

			wrap.Add(cap);
			wrap.Add(grip);
			wrap.Add(field);
			if (string.IsNullOrEmpty(suffix) == false) {
				wrap.Add(new Label(suffix) { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginLeft = 3, flexShrink = 0 } });
			}
			return (wrap, Sync);
		}
	}
}
