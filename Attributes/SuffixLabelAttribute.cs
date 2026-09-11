using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Appends a small grey label to a field in a <see cref="SInspectorAttribute"/> inspector — a unit, a
	/// hint, or a live value.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class SuffixLabelAttribute : Attribute {

		/// <summary>Literal text, or <c>$memberName</c> to poll a <c>string</c> field / property /
		/// parameterless method on the same object (~250 ms).</summary>
		public string Label { get; }

		/// <summary>When <c>true</c> the label is drawn inside the field at its right edge instead of after it.</summary>
		public bool Overlay { get; }

		public SuffixLabelAttribute(string label, bool overlay = false) {
			Label = label;
			Overlay = overlay;
		}
	}
}
