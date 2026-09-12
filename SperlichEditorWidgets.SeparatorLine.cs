using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Horizontal separator line for <c>[HLine]</c>. <paramref name="style"/> picks solid /
		/// dashed / dotted; a non-empty <paramref name="label"/> can be placed inline or above the line,
		/// aligned left / center / right.</summary>
		public static VisualElement CreateSeparatorLine(string label, Color color, LineStyle style, HLineAlign align = HLineAlign.Center, HLinePlacement placement = HLinePlacement.Inline, int thickness = 1) {
			if (string.IsNullOrEmpty(label)) {
				var wrap = new VisualElement {
					style = {
						flexDirection = FlexDirection.Row, alignItems = Align.Center,
						marginTop = 7, marginBottom = 7, flexShrink = 0,
					}
				};
				wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
				return wrap;
			}

			if (placement == HLinePlacement.Above) {
				var wrap = new VisualElement {
					style = {
						flexDirection = FlexDirection.Column,
						marginTop = 8, marginBottom = 6, flexShrink = 0,
					}
				};

				Align alignSelf = align switch {
					HLineAlign.Left => Align.FlexStart,
					HLineAlign.Right => Align.FlexEnd,
					_ => Align.Center
				};

				var lbl = new Label(label) {
					style = {
						fontSize = 10,
						color = SperlichEditorTheme.TextMuted,
						marginBottom = 3,
						alignSelf = alignSelf,
						unityFontStyleAndWeight = FontStyle.Bold,
						letterSpacing = 0.5f,
					}
				};
				wrap.Add(lbl);
				wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
				return wrap;
			} else {
				var wrap = new VisualElement {
					style = {
						flexDirection = FlexDirection.Row, alignItems = Align.Center,
						marginTop = 7, marginBottom = 7, flexShrink = 0,
					}
				};

				var lbl = new Label(label) {
					style = {
						fontSize = 10, color = SperlichEditorTheme.TextMuted,
						marginLeft = 8, marginRight = 8, flexShrink = 0,
						unityFontStyleAndWeight = FontStyle.Bold,
					}
				};

				if (align == HLineAlign.Left) {
					wrap.Add(MakeLineSegment(color, style, thickness, grow: 0f, fixedWidth: 12f));
					wrap.Add(lbl);
					wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
				} else if (align == HLineAlign.Right) {
					wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
					wrap.Add(lbl);
					wrap.Add(MakeLineSegment(color, style, thickness, grow: 0f, fixedWidth: 12f));
				} else {
					wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
					wrap.Add(lbl);
					wrap.Add(MakeLineSegment(color, style, thickness, grow: 1f));
				}
				return wrap;
			}
		}

		private static VisualElement MakeLineSegment(Color color, LineStyle style, int thickness, float grow, float fixedWidth = 0f) {
			var seg = new VisualElement {
				style = {
					flexGrow = grow,
					flexShrink = grow > 0f ? 1 : 0,
					height = Mathf.Max(thickness, style == LineStyle.Solid ? thickness : 2)
				}
			};
			if (fixedWidth > 0f) {
				seg.style.width = fixedWidth;
				seg.style.flexGrow = 0;
			}

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
