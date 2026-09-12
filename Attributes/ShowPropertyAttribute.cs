using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Shows a C# property as a read-only live value in a <see cref="SInspectorAttribute"/> inspector. The
	/// value is polled (default every 250 ms) and never written back or serialized. The row appears where
	/// the property is declared in the file.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class ShowPropertyAttribute : Attribute {

		/// <summary>Row label override. <c>null</c> = the property name, prettified.</summary>
		public string Label { get; }

		/// <summary>Poll interval in milliseconds.</summary>
		public int PollMs { get; }

		/// <summary>Optional suffix text (e.g. unit like "m", "ms") appended to the formatted value.</summary>
		public string Suffix { get; set; }

		/// <summary>When true, formats the value as a badge/pill (e.g. TRUE / FALSE).</summary>
		public bool Badge { get; set; }

		/// <summary>Optional tint colour from the <see cref="TintColor"/> palette.</summary>
		public TintColor Tint { get; set; } = TintColor.None;

		/// <summary>Optional HTML colour string (e.g. "#4ecdc4").</summary>
		public string ColorHex { get; set; }

		public ShowPropertyAttribute(string label = null, int pollMs = 250) {
			Label = label;
			PollMs = pollMs;
		}

		public ShowPropertyAttribute(string label, string suffix, int pollMs = 250) {
			Label = label;
			Suffix = suffix;
			PollMs = pollMs;
		}
	}
}
