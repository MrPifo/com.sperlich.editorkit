using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Small colored pill badge next to the field's label, in a <see cref="SInspectorAttribute"/> inspector.
	/// Purely decorative — for flagging a field as experimental, deprecated-but-kept, designer-only, etc.
	/// Stackable: several <c>[Tag]</c> attributes render as several pills in declaration order.
	///
	/// <code>[Tag("BETA", TagColor.Cyan)] public float chaosFactor;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class TagAttribute : Attribute {

		public string Label { get; }

		/// <summary>Preset pill colour. Ignored when <see cref="ColorHex"/> or <see cref="Tint"/> is set.</summary>
		public TagColor Color { get; }

		/// <summary>Custom colour as hex string, taking priority over <see cref="Color"/> and <see cref="Tint"/>.</summary>
		public string ColorHex { get; }

		/// <summary>Custom colour from the <see cref="Sperlich.EditorKit.TintColor"/> palette, taking priority
		/// over <see cref="Color"/> (but not <see cref="ColorHex"/>).</summary>
		public TintColor Tint { get; }

		public TagAttribute(string label, TagColor color = TagColor.Cyan, string colorHex = null, TintColor tint = TintColor.None) {
			Label = label;
			Color = color;
			ColorHex = colorHex;
			Tint = tint;
		}

		/// <summary>Pick the pill colour from the built-in <see cref="Sperlich.EditorKit.TintColor"/> palette
		/// instead of the smaller <see cref="TagColor"/> preset set.</summary>
		public TagAttribute(string label, TintColor tint) {
			Label = label;
			Tint = tint;
		}

		/// <summary>Pick the pill colour as a hex string instead of a <see cref="TagColor"/> preset.</summary>
		public TagAttribute(string label, string colorHex) {
			Label = label;
			ColorHex = colorHex;
		}
	}
}
