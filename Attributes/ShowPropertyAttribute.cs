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

		public ShowPropertyAttribute(string label = null, int pollMs = 250) {
			Label = label;
			PollMs = pollMs;
		}
	}
}
