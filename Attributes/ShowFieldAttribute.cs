using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Shows a non-serialized field as a read-only live value in a <see cref="SInspectorAttribute"/>
	/// inspector (polled, default 250 ms, never written back). On a serialized field this attribute is a
	/// no-op — the normal field row already shows it.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class ShowFieldAttribute : Attribute {

		/// <summary>Row label override. <c>null</c> = the field name, prettified.</summary>
		public string Label { get; }

		/// <summary>Poll interval in milliseconds.</summary>
		public int PollMs { get; }

		public ShowFieldAttribute(string label = null, int pollMs = 250) {
			Label = label;
			PollMs = pollMs;
		}
	}
}
