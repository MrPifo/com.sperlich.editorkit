using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Shows the field only while a condition on another member of the same object holds, in a
	/// <see cref="SInspectorAttribute"/> inspector. The condition is re-evaluated live (event-driven for a
	/// serialized driver field, polled otherwise).
	///
	/// <list type="bullet">
	/// <item><c>[ShowIf(nameof(enabled))]</c> — driver is "truthy" (bool <c>true</c>, non-zero number,
	/// non-empty string, non-null reference).</item>
	/// <item><c>[ShowIf(nameof(mode), Mode.Advanced)]</c> — driver equals the value (enum / bool / number /
	/// string). Pass several values to match any of them.</item>
	/// <item><c>[ShowIf(nameof(flags), Feature.Audio)]</c> — when the driver is a <c>[Flags]</c> enum: visible
	/// while it contains those bits.</item>
	/// </list>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class ShowIfAttribute : Attribute {

		/// <summary>Name of the driver member (field / property / parameterless method) on the same object.</summary>
		public string Member { get; }

		/// <summary>Values to match. Empty = "truthy" check.</summary>
		public object[] Values { get; }

		public ShowIfAttribute(string member, params object[] values) {
			Member = member;
			Values = values ?? Array.Empty<object>();
		}
	}
}
