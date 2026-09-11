using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Enables (rather than shows/hides, see <see cref="ShowIfAttribute"/>) the field only while a condition on
	/// another member of the same object holds, in a <see cref="SInspectorAttribute"/> inspector. The field
	/// stays visible but greyed out and non-interactive while the condition fails. Same condition grammar as
	/// <see cref="ShowIfAttribute"/> — no values = truthy check, one or more values = equals-any /
	/// <c>[Flags]</c>-contains-any.
	///
	/// <code>[EnableIf(nameof(useCustomRange))] public float customMax;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class EnableIfAttribute : Attribute {

		public string Member { get; }
		public object[] Values { get; }

		public EnableIfAttribute(string member, params object[] values) {
			Member = member;
			Values = values ?? Array.Empty<object>();
		}
	}
}
