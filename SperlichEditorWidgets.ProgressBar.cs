using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Read-only progress bar for <c>[ProgressBar]</c>: a rounded dark track with an accent fill,
		/// an optional <c>value / max</c> overlay and optional notch marks. Returns the element plus three
		/// setters — <c>setValue</c> (event-driven from the field), <c>setRange</c> (for a dynamic min/max
		/// member) and <c>setDashed</c> (switches the fill to a diagonal-striped look, e.g. to mark a delay/wait
		/// phase as visually distinct from a normal fill).</summary>
		public static (VisualElement root, Action<float> setValue, Action<float, float> setRange, Action<bool> setDashed) CreateProgressBar(
			float value, float min, float max, Color fill, int height, bool segmented, bool showValue, bool percent = false) {

			int h = Mathf.Max(2, height);
			// Corner radius must never exceed half the track's own height, or UIToolkit's rounding can
			// swallow the fill/track color entirely on very thin bars (looked like a colorless flash instead
			// of a growing bar once height was lowered below the old fixed radius of 3/2).
			int trackRadius = Mathf.Clamp(h / 2, 1, 3);
			int fillRadius = Mathf.Clamp(h / 2, 1, 2);
			Color overflow = new Color(0.85f, 0.35f, 0.25f);

			var track = CreateBox(trackRadius, SperlichEditorTheme.BorderSubtle);
			track.style.height = h;
			track.style.flexGrow = 1;
			track.style.backgroundColor = SperlichEditorTheme.BgDark;
			track.style.position = Position.Relative;
			track.style.overflow = Overflow.Hidden;

			var fillEl = new VisualElement {
				style = {
					position = Position.Absolute, left = 0, top = 0, bottom = 0,
					backgroundColor = fill,
				}
			};
			SetRadius(fillEl, fillRadius);
			track.Add(fillEl);

			bool dashed = false;

			// Normal mode: a subtle vertical gradient on the fill for a bit of depth.
			// Dashed mode (delay/wait phases): diagonal stripes instead, so it reads as "not a real step
			// playing" at a glance rather than a normal solid progress fill.
			fillEl.generateVisualContent += ctx => {
				Rect r = ctx.visualElement.contentRect;
				if (r.width <= 0f || r.height <= 0f) return;
				var p = ctx.painter2D;
				if (dashed) {
					p.strokeColor = new Color(0f, 0f, 0f, 0.35f);
					p.lineWidth = Mathf.Max(1f, r.height * 0.35f);
					float stripeSpacing = Mathf.Max(4f, r.height);
					float diag = r.width + r.height;
					p.BeginPath();
					for (float x = -r.height; x < diag; x += stripeSpacing) {
						p.MoveTo(new Vector2(x, r.height));
						p.LineTo(new Vector2(x + r.height, 0));
					}
					p.Stroke();
					return;
				}
				p.fillColor = new Color(0f, 0f, 0f, 0.12f);
				p.BeginPath();
				p.MoveTo(new Vector2(0, r.height * 0.55f));
				p.LineTo(new Vector2(r.width, r.height * 0.55f));
				p.LineTo(new Vector2(r.width, r.height));
				p.LineTo(new Vector2(0, r.height));
				p.ClosePath();
				p.Fill();
			};

			Label overlayLabel = null;
			if (showValue) {
				overlayLabel = new Label {
					pickingMode = PickingMode.Ignore,
					style = {
						position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0,
						unityTextAlign = TextAnchor.MiddleCenter,
						fontSize = 10, color = SperlichEditorTheme.TextPrimary,
						unityFontStyleAndWeight = FontStyle.Bold,
					}
				};
				track.Add(overlayLabel);
			}

			if (segmented) {
				var notches = new VisualElement { pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0 } };
				notches.generateVisualContent += ctx => {
					Rect r = ctx.visualElement.contentRect;
					if (r.width <= 0f) return;
					var p = ctx.painter2D;
					p.lineWidth = 1;
					p.strokeColor = new Color(0f, 0f, 0f, 0.3f);
					p.BeginPath();
					for (int s = 1; s < 10; s++) {
						float x = r.width * (s / 10f);
						p.MoveTo(new Vector2(x, 0));
						p.LineTo(new Vector2(x, r.height));
					}
					p.Stroke();
				};
				track.Add(notches);
			}

			float curMin = min, curMax = max, curVal = value;

			Color dashedFill = Color.Lerp(fill, new Color(0.6f, 0.6f, 0.6f), 0.5f);

			void Redraw() {
				float span = curMax - curMin;
				float t = span > 0.0001f ? Mathf.Clamp01((curVal - curMin) / span) : 0f;
				fillEl.style.width = Length.Percent(t * 100f);
				bool over = curVal > curMax + 0.0001f;
				fillEl.style.backgroundColor = over ? overflow : (dashed ? dashedFill : fill);
				if (overlayLabel != null) {
					float rawT = span > 0.0001f ? (curVal - curMin) / span : 0f;
					overlayLabel.text = percent
						? Mathf.RoundToInt(rawT * 100f) + "%"
						: FormatPbValue(curVal) + " / " + FormatPbValue(curMax);
				}
			}

			Redraw();

			void SetValue(float v) { curVal = v; Redraw(); }
			void SetRange(float lo, float hi) { curMin = lo; curMax = hi; Redraw(); }
			void SetDashed(bool d) { if (dashed == d) return; dashed = d; Redraw(); fillEl.MarkDirtyRepaint(); }

			return (track, SetValue, SetRange, SetDashed);
		}

		private static string FormatPbValue(float v) {
			if (Mathf.Approximately(v, Mathf.Round(v))) return Mathf.RoundToInt(v).ToString();
			return v.ToString("0.##");
		}
	}
}
