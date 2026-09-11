using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Calls a method on the same object whenever the field's value changes in a
	/// <see cref="SInspectorAttribute"/> inspector. Supported signatures, checked in this order:
	/// <c>()</c>, <c>(T newValue)</c>, <c>(T oldValue, T newValue)</c> where <c>T</c> is the field type
	/// (or <c>object</c>).
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class OnValueChangedAttribute : Attribute {

		/// <summary>Name of the callback method on the declaring object.</summary>
		public string Method { get; }

		public OnValueChangedAttribute(string method) => Method = method;
	}
}
