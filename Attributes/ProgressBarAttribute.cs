using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Renders an <c>int</c> / <c>float</c> field as a read-only progress bar in a
	/// <see cref="SInspectorAttribute"/> inspector (the interactive counterpart is a normal slider). The
	/// fill follows the serialized value event-driven; only a dynamic range (<see cref="MinMember"/> /
	/// <see cref="MaxMember"/>) is polled.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class ProgressBarAttribute : Attribute {

		public float Min { get; }
		public float Max { get; }

		/// <summary>Fill colour as HTML string. <c>null</c> = fall back to <see cref="Tint"/>, then the theme
		/// accent.</summary>
		public string Color { get; }

		/// <summary>Fill colour from the <see cref="Sperlich.EditorKit.TintColor"/> palette. Only used when
		/// <see cref="Color"/> is <c>null</c>.</summary>
		public TintColor Tint { get; }

		/// <summary>Bar height in pixels.</summary>
		public int Height { get; }

		/// <summary>Optional member name (field / property / parameterless method returning a number) that
		/// supplies a live lower bound; polled ~250 ms.</summary>
		public string MinMember { get; }

		/// <summary>Optional member name that supplies a live upper bound; polled ~250 ms.</summary>
		public string MaxMember { get; }

		/// <summary>Draw notch marks along the bar.</summary>
		public bool Segmented { get; }

		/// <summary>Overlay the value text on the bar.</summary>
		public bool ShowValue { get; }

		/// <summary>Show the overlay as a percentage (e.g. <c>50%</c>) instead of <c>value / max</c>.</summary>
		public bool Percent { get; }

		public ProgressBarAttribute(float min, float max, string color = null, int height = 18,
			string minMember = null, string maxMember = null, bool segmented = false, bool showValue = true,
			bool percent = false, TintColor tint = TintColor.None) {
			Min = min;
			Max = max;
			Color = color;
			Height = height;
			MinMember = minMember;
			MaxMember = maxMember;
			Segmented = segmented;
			ShowValue = showValue;
			Percent = percent;
			Tint = tint;
		}
	}
}
