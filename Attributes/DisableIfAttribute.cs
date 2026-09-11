using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>Inverse of <see cref="EnableIfAttribute"/> — disables the field while the condition holds.</summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class DisableIfAttribute : Attribute {

		public string Member { get; }
		public object[] Values { get; }

		public DisableIfAttribute(string member, params object[] values) {
			Member = member;
			Values = values ?? Array.Empty<object>();
		}
	}
}
