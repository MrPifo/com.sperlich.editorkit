using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>Inverse of <see cref="ShowIfAttribute"/>: hides the field while the condition holds. Same
	/// matching rules (truthy / equals-any / <c>[Flags]</c> contains).</summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class HideIfAttribute : Attribute {

		/// <summary>Name of the driver member (field / property / parameterless method) on the same object.</summary>
		public string Member { get; }

		/// <summary>Values to match. Empty = "truthy" check.</summary>
		public object[] Values { get; }

		public HideIfAttribute(string member, params object[] values) {
			Member = member;
			Values = values ?? Array.Empty<object>();
		}
	}
}
