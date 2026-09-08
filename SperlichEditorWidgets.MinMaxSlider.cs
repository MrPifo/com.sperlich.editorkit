using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>
		/// Dual-handle "from–to" range slider for a <c>Vector2</c> / <c>Vector2Int</c> property (x = low,
		/// y = high), bounded to <paramref name="min"/>..<paramref name="max"/>. Matches the single range
		/// slider (design "C"): thin track, filled band between the handles, two compact drag/type number
		/// fields on the right. Drag a handle, drag the band to move both, or type. Driven by
		/// <c>[SMinMax(min, max)]</c> in the auto inspector.
		/// </summary>
		public static VisualElement CreateMinMaxSlider(SerializedProperty vec2Prop, float min, float max, bool intMode, Color? accent = null, int fieldWidth = 52) {
			Color acc = accent ?? SperlichEditorTheme.ButtonAccent;
			Color gripLine = new Color(0.06f, 0.13f, 0.22f);

			SerializedProperty xP = vec2Prop.FindPropertyRelative("x") ?? vec2Prop.FindPropertyRelative("m_X");
			SerializedProperty yP = vec2Prop.FindPropertyRelative("y") ?? vec2Prop.FindPropertyRelative("m_Y");
			if (xP == null || yP == null) {
				var pf = new PropertyField(vec2Prop);
				return pf;
			}
			bool isIntProp = xP.propertyType == SerializedPropertyType.Integer;

			float Low() => isIntProp ? xP.intValue : xP.floatValue;
			float High() => isIntProp ? yP.intValue : yP.floatValue;

			void Store(SerializedProperty p, float v) {
				if (isIntProp) {
					int iv = Mathf.RoundToInt(v);
					if (p.intValue != iv) { p.intValue = iv; p.serializedObject.ApplyModifiedProperties(); }
				} else if (!Mathf.Approximately(p.floatValue, v)) {
					p.floatValue = v;
					p.serializedObject.ApplyModifiedProperties();
				}
			}

			void WriteLow(float v) {
				if (intMode) v = Mathf.Round(v);
				Store(xP, Mathf.Clamp(v, min, High()));
			}
			void WriteHigh(float v) {
				if (intMode) v = Mathf.Round(v);
				Store(yP, Mathf.Clamp(v, Low(), max));
			}

			var root = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, overflow = Overflow.Hidden } };

			var trackArea = new VisualElement {
				pickingMode = PickingMode.Position,
				style = { flexGrow = 1, flexShrink = 1, minWidth = 60, height = 18, position = Position.Relative, marginRight = 8 }
			};

			const float inset = 6f;
			var travel = new VisualElement {
				style = { position = Position.Absolute, left = inset, right = inset, top = 0, bottom = 0 }
			};
			trackArea.Add(travel);

			var track = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = { position = Position.Absolute, left = 0, right = 0, top = 7, height = 4, backgroundColor = SperlichEditorTheme.BgDark }
			};
			SetRadius(track, 2);
			travel.Add(track);

			var band = new VisualElement { style = { position = Position.Absolute, top = 7, height = 4, backgroundColor = acc } };
			SetRadius(band, 2);
			SetHoverCursor(band, MouseCursor.SlideArrow);
			travel.Add(band);

			VisualElement MakeHandle() {
				var h = new VisualElement {
					style = {
						position = Position.Absolute, top = 1, width = 12, height = 16, backgroundColor = acc,
						flexDirection = FlexDirection.Column, justifyContent = Justify.Center, alignItems = Align.Center,
						translate = new Translate(Length.Percent(-50f), 0f, 0f),
					}
				};
				SetRadius(h, 3);
				SetHoverCursor(h, MouseCursor.SlideArrow);
				h.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { width = 4, height = 1, backgroundColor = gripLine } });
				h.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { width = 4, height = 1, marginTop = 2, backgroundColor = gripLine } });
				return h;
			}
			var handleLow = MakeHandle();
			var handleHigh = MakeHandle();
			travel.Add(handleLow);
			travel.Add(handleHigh);

			float T(float v) => max > min ? Mathf.Clamp01(Mathf.InverseLerp(min, max, v)) : 0f;

			Action refresh = null;

			var (fromField, syncFrom) = MakeVirtualDragNumber("", Low, v => { WriteLow(v); refresh(); }, 1f, null);
			var (toField, syncTo) = MakeVirtualDragNumber("", High, v => { WriteHigh(v); refresh(); }, 1f, null);
			fromField.style.width = fieldWidth;
			fromField.style.flexShrink = 0;
			toField.style.width = fieldWidth;
			toField.style.flexShrink = 0;

			refresh = () => {
				float tl = T(Low());
				float th = T(High());
				band.style.left = Length.Percent(tl * 100f);
				band.style.width = Length.Percent(Mathf.Max(0f, th - tl) * 100f);
				handleLow.style.left = Length.Percent(tl * 100f);
				handleHigh.style.left = Length.Percent(th * 100f);
				syncFrom();
				syncTo();
			};

			float ValueAtLocalX(float localX) {
				float w = trackArea.resolvedStyle.width;
				float usable = w - 2f * inset;
				if (usable <= 0f) return min;
				return Mathf.Lerp(min, max, Mathf.Clamp01((localX - inset) / usable));
			}

			// 0 = none, 1 = low handle, 2 = high handle, 3 = whole band
			int drag = 0;
			float bandGrabValue = 0f, bandGrabLow = 0f, bandGrabHigh = 0f;

			trackArea.RegisterCallback<PointerDownEvent>(e => {
				float v = ValueAtLocalX(e.localPosition.x);
				var t = e.target as VisualElement;
				if (t == handleLow) drag = 1;
				else if (t == handleHigh) drag = 2;
				else if (t == band) {
					drag = 3;
					bandGrabValue = v; bandGrabLow = Low(); bandGrabHigh = High();
				} else {
					// click on empty track -> grab the nearer handle
					drag = Mathf.Abs(v - Low()) <= Mathf.Abs(v - High()) ? 1 : 2;
				}

				if (drag == 1) WriteLow(v);
				else if (drag == 2) WriteHigh(v);
				refresh();
				trackArea.CapturePointer(e.pointerId);
				e.StopPropagation();
			});
			trackArea.RegisterCallback<PointerMoveEvent>(e => {
				if (drag == 0) return;
				float v = ValueAtLocalX(e.localPosition.x);
				if (drag == 1) {
					WriteLow(v);
				} else if (drag == 2) {
					WriteHigh(v);
				} else {
					float span = bandGrabHigh - bandGrabLow;
					float delta = Mathf.Clamp(v - bandGrabValue, min - bandGrabLow, max - bandGrabHigh);
					float lo = bandGrabLow + delta;
					if (intMode) lo = Mathf.Round(lo);
					Store(xP, Mathf.Clamp(lo, min, max - span));
					Store(yP, Mathf.Clamp(lo + span, min + span, max));
				}
				refresh();
			});
			trackArea.RegisterCallback<PointerUpEvent>(e => {
				if (drag == 0) return;
				drag = 0;
				trackArea.ReleasePointer(e.pointerId);
			});
			trackArea.RegisterCallback<PointerCaptureOutEvent>(_ => drag = 0);

			root.Add(trackArea);

			var fieldsWrap = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 0 } };
			fieldsWrap.Add(fromField);
			fieldsWrap.Add(new Label("–") { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginLeft = 3, marginRight = 3, flexShrink = 0 } });
			fieldsWrap.Add(toField);
			root.Add(fieldsWrap);

			root.TrackPropertyValue(vec2Prop, _ => refresh());
			root.RegisterCallback<GeometryChangedEvent>(_ => refresh());
			refresh();
			return root;
		}
	}
}
