using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Tints a field's label (and optionally its input background) in a <see cref="SInspectorAttribute"/>
	/// inspector.
	///
	/// <para>Named <c>TintColor</c> (not <c>Color</c>) to avoid CS1614 against <c>UnityEngine.Color</c>.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class TintColorAttribute : Attribute {

		/// <summary>Colour as <c>"red"</c> / <c>"#RRGGBB"</c> / <c>"#RRGGBBAA"</c>. <c>null</c> when constructed
		/// via the <see cref="TintColor"/> palette overload instead.</summary>
		public string Color { get; }

		/// <summary>Colour from the <see cref="Sperlich.EditorKit.TintColor"/> palette, set only by the enum
		/// overload. <see cref="Sperlich.EditorKit.TintColor.None"/> means "use <see cref="Color"/> instead".</summary>
		public TintColor Tint { get; }

		/// <summary>Also tint the input container background (at low alpha for readability).</summary>
		public bool Background { get; }

		public TintColorAttribute(string color, bool background = false) {
			Color = color;
			Background = background;
		}

		/// <summary>Pick the colour from the built-in <see cref="Sperlich.EditorKit.TintColor"/> palette instead
		/// of typing a hex code.</summary>
		public TintColorAttribute(TintColor tint, bool background = false) {
			Tint = tint;
			Background = background;
		}
	}
}
