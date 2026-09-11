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
		public TagColor Color { get; }

		public TagAttribute(string label, TagColor color = TagColor.Cyan) {
			Label = label;
			Color = color;
		}
	}
}
