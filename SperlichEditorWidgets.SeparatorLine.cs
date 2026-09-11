using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Horizontal separator line for <c>[HLine]</c>. <paramref name="style"/> picks solid /
		/// dashed / dotted; a non-empty <paramref name="label"/> puts a small muted caption in the middle
		/// with the line running left and right of it.</summary>
		public static VisualElement CreateSeparatorLine(string label, Color color, LineStyle style, int thickness = 1) {
			var wrap = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row, alignItems = Align.Center,
					marginTop = 7, marginBottom = 7, flexShrink = 0,
				}
			};

			if (string.IsNullOrEmpty(label)) {
				wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
				return wrap;
			}

			wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
			wrap.Add(new Label(label) {
				style = {
					fontSize = 10, color = SperlichEditorTheme.TextMuted,
					marginLeft = 8, marginRight = 8, flexShrink = 0,
					unityFontStyleAndWeight = FontStyle.Bold,
				}
			});
			wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
			return wrap;
		}

		private static VisualElement MakeLineSegment(Color color, LineStyle style, int thickness, float grow) {
			var seg = new VisualElement { style = { flexGrow = grow, flexShrink = 1, height = Mathf.Max(thickness, style == LineStyle.Solid ? thickness : 2) } };

			if (style == LineStyle.Solid) {
				seg.style.height = thickness;
				seg.style.backgroundColor = color;
				return seg;
			}

			// Dashed / dotted: one Painter2D pass along the segment's mid-line.
			seg.style.height = Mathf.Max(2, thickness + 1);
			seg.generateVisualContent += ctx => {
				float w = ctx.visualElement.contentRect.width;
				float y = ctx.visualElement.contentRect.height * 0.5f;
				if (w <= 0f) return;

				var p = ctx.painter2D;
				p.lineWidth = thickness;
				p.strokeColor = color;
				p.lineCap = LineCap.Butt;

				float dash = style == LineStyle.Dotted ? thickness : 6f;
				float gap = style == LineStyle.Dotted ? thickness + 2f : 4f;
				float x = 0f;
				p.BeginPath();
				while (x < w) {
					float x2 = Mathf.Min(x + dash, w);
					p.MoveTo(new Vector2(x, y));
					p.LineTo(new Vector2(x2, y));
					x = x2 + gap;
				}
				p.Stroke();
			};
			return seg;
		}
	}
}
