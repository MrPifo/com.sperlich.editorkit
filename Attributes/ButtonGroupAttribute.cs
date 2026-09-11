using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Groups parameterless methods into one horizontal segmented button bar in a
	/// <see cref="SInspectorAttribute"/> inspector. All methods sharing the same <see cref="Group"/> key
	/// become segments of a single bar, in declaration order. The bar is placed at the declaration position
	/// of the first method in the group.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class ButtonGroupAttribute : Attribute {

		/// <summary>Shared key — methods with the same key render as one bar.</summary>
		public string Group { get; }

		/// <summary>Segment caption. <c>null</c> = the method name, prettified.</summary>
		public string Label { get; }

		/// <summary>Optional Unity builtin icon name for the segment.</summary>
		public string Icon { get; }

		public ButtonGroupAttribute(string group, string label = null, string icon = null) {
			Group = group;
			Label = label;
			Icon = icon;
		}
	}
}
