using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Overrides the display label of a field or property in a <see cref="SInspectorAttribute"/> inspector.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class LabelAttribute : Attribute {

		/// <summary>The custom label text to display in the inspector.</summary>
		public string Text { get; }

		public LabelAttribute(string text) {
			Text = text;
		}
	}

	/// <summary>
	/// Alias for <see cref="LabelAttribute"/> matching the Odin Inspector naming convention.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class LabelTextAttribute : LabelAttribute {

		public LabelTextAttribute(string text) : base(text) { }
	}
}
