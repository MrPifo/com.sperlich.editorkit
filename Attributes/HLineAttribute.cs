using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Draws a horizontal separator line above the field in a <see cref="SInspectorAttribute"/> inspector.
	/// Can be applied multiple times to stack lines / labels.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class HLineAttribute : Attribute {

		/// <summary>Optional small centered label sitting on the line.</summary>
		public string Label { get; }

		/// <summary>Line colour as <c>"red"</c> / <c>"#RRGGBB"</c> / <c>"#RRGGBBAA"</c>
		/// (<c>ColorUtility.TryParseHtmlString</c>). <c>null</c> = fall back to <see cref="Tint"/>, then the
		/// theme's strong border colour.</summary>
		public string Color { get; }

		/// <summary>Line colour from the <see cref="Sperlich.EditorKit.TintColor"/> palette. Only used when
		/// <see cref="Color"/> is <c>null</c>.</summary>
		public TintColor Tint { get; }

		/// <summary>Solid / dashed / dotted.</summary>
		public LineStyle Style { get; }

		public HLineAttribute(string label = null, string color = null, LineStyle style = LineStyle.Solid, TintColor tint = TintColor.None) {
			Label = label;
			Color = color;
			Style = style;
			Tint = tint;
		}
	}
}
