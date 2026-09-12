using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Overrides the accent colour of the field's interactive control in a <see cref="SInspectorAttribute"/>
	/// inspector — the fill of a <c>[Range]</c> slider, a bool's <see cref="PillToggle"/> on-colour, a
	/// <c>[Knob]</c>'s arc, an enum dropdown / <c>[EnumToggleButtons]</c> selection, and a collection's
	/// (array / <c>List&lt;&gt;</c> / <c>SDictionary&lt;,&gt;</c> / <c>SHashSet&lt;&gt;</c>) header. Falls back
	/// to the theme accent when not set.
	///
	/// <code>[AccentColor(TintColor.Teal)] public float health;</code>
	/// <code>[AccentColor("#ff6b6b")] public List&lt;string&gt; tags;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class AccentColorAttribute : Attribute {

		/// <summary>Accent colour as a hex string. Takes priority over <see cref="Tint"/>.</summary>
		public string ColorHex { get; }

		/// <summary>Accent colour from the <see cref="Sperlich.EditorKit.TintColor"/> palette. Only used when
		/// <see cref="ColorHex"/> is <c>null</c>.</summary>
		public TintColor Tint { get; }

		/// <summary>Pick the accent as a hex string (e.g. <c>"#4ecdc4"</c>).</summary>
		public AccentColorAttribute(string colorHex) {
			ColorHex = colorHex;
		}

		/// <summary>Pick the accent from the built-in <see cref="Sperlich.EditorKit.TintColor"/> palette.</summary>
		public AccentColorAttribute(TintColor tint) {
			Tint = tint;
		}
	}
}
