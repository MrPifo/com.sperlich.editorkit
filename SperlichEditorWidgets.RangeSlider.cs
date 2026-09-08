using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>
		/// Sperlich range slider (design "C"): a thin filled track with a rounded rectangular grip, tick
		/// marks for integer ranges, and a real drag-/type-able number field on the right (the shared
		/// <see cref="CreateDragNumberField"/> — 6-dot grip + input, clamped to the range). Drag the track
		/// or the number grip, or type a value. Replaces Unity's <c>Slider</c>/<c>SliderInt</c> for
		/// <c>[Range]</c> fields.
		/// </summary>
		/// <param name="intMode">Snap to whole steps and draw ticks. Works on both int- and float-backed
		/// properties (a float prop is still stored as a float, just snapped).</param>
		public static VisualElement CreateRangeSlider(SerializedProperty prop, float min, float max, bool intMode, Color? accent = null, int fieldWidth = 62) {
			Color acc = accent ?? SperlichEditorTheme.ButtonAccent;
			Color gripLine = new Color(0.06f, 0.13f, 0.22f);
			bool isIntProp = prop.propertyType == SerializedPropertyType.Integer;

			var root = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, overflow = Overflow.Hidden } };

			var trackArea = new VisualElement {
				pickingMode = PickingMode.Position,
				style = { flexGrow = 1, flexShrink = 1, minWidth = 40, height = 18, position = Position.Relative, marginRight = 8 }
			};
			SetHoverCursor(trackArea, MouseCursor.SlideArrow);

			// Handle travel is inset by half the handle width so the grip never spills past the row edge;
			// all % positions below are relative to this inset band.
			const float inset = 7f;
			var travel = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = { position = Position.Absolute, left = inset, right = inset, top = 0, bottom = 0 }
			};
			trackArea.Add(travel);

			var track = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = { position = Position.Absolute, left = 0, right = 0, top = 7, height = 4, backgroundColor = SperlichEditorTheme.BgDark }
			};
			SetRadius(track, 2);
			travel.Add(track);

			var fill = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = { position = Position.Absolute, left = 0, top = 7, height = 4, backgroundColor = acc }
			};
			SetRadius(fill, 2);
			travel.Add(fill);

			if (intMode) {
				int steps = Mathf.RoundToInt(max - min);
				if (steps >= 1 && steps <= 40) {
					for (int s = 0; s <= steps; s++) {
						float t = (float)s / steps;
						var tick = new VisualElement {
							pickingMode = PickingMode.Ignore,
							style = {
								position = Position.Absolute, top = 6, width = 1, height = 6,
								backgroundColor = SperlichEditorTheme.BorderStrong,
								left = Length.Percent(t * 100f),
							}
						};
						travel.Add(tick);
					}
				}
			}

			var handle = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = {
					position = Position.Absolute, top = 1, width = 14, height = 16, backgroundColor = acc,
					flexDirection = FlexDirection.Column, justifyContent = Justify.Center, alignItems = Align.Center,
					translate = new Translate(Length.Percent(-50f), 0f, 0f),
				}
			};
			SetRadius(handle, 3);
			handle.Add(new VisualElement { style = { width = 5, height = 1, backgroundColor = gripLine } });
			handle.Add(new VisualElement { style = { width = 5, height = 1, marginTop = 2, backgroundColor = gripLine } });
			travel.Add(handle);

			float Current() => isIntProp ? prop.intValue : prop.floatValue;

			void Refresh() {
				float v = Mathf.Clamp(Current(), min, max);
				float t = max > min ? Mathf.Clamp01(Mathf.InverseLerp(min, max, v)) : 0f;
				fill.style.width = Length.Percent(t * 100f);
				handle.style.left = Length.Percent(t * 100f);
			}

			void SetFromLocalX(float localX) {
				float w = trackArea.resolvedStyle.width;
				float usable = w - 2f * inset;
				if (usable <= 0f) return;
				float v = Mathf.Lerp(min, max, Mathf.Clamp01((localX - inset) / usable));
				if (intMode) v = Mathf.Round(v);

				if (isIntProp) {
					int iv = Mathf.RoundToInt(v);
					if (prop.intValue != iv) { prop.intValue = iv; prop.serializedObject.ApplyModifiedProperties(); }
				} else if (!Mathf.Approximately(prop.floatValue, v)) {
					prop.floatValue = v;
					prop.serializedObject.ApplyModifiedProperties();
				}
				Refresh();
			}

			bool dragging = false;
			trackArea.RegisterCallback<PointerDownEvent>(e => {
				dragging = true;
				trackArea.CapturePointer(e.pointerId);
				SetFromLocalX(e.localPosition.x);
				e.StopPropagation();
			});
			trackArea.RegisterCallback<PointerMoveEvent>(e => { if (dragging) SetFromLocalX(e.localPosition.x); });
			trackArea.RegisterCallback<PointerUpEvent>(e => {
				if (!dragging) return;
				dragging = false;
				trackArea.ReleasePointer(e.pointerId);
			});
			trackArea.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);

			root.Add(trackArea);

			VisualElement numberField = CreateDragNumberField(prop, 1f, min, max);
			numberField.style.width = fieldWidth;
			numberField.style.minWidth = fieldWidth;
			numberField.style.maxWidth = fieldWidth;
			numberField.style.flexGrow = 0;
			numberField.style.flexShrink = 0;
			numberField.style.overflow = Overflow.Hidden;
			root.Add(numberField);

			root.TrackPropertyValue(prop, _ => Refresh());
			root.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
			Refresh();
			return root;
		}
	}
}
