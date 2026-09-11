using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Adds a small button to the right of a field's row in a <see cref="SInspectorAttribute"/> inspector,
	/// wired to a parameterless method on the same object. Can be applied multiple times to chain buttons.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class InlineButtonAttribute : Attribute {

		/// <summary>Name of the parameterless method to invoke.</summary>
		public string Method { get; }

		/// <summary>Button caption. <c>null</c> = the method name, prettified.</summary>
		public string Label { get; }

		/// <summary>Optional Unity builtin icon name (<c>EditorGUIUtility.IconContent</c>).</summary>
		public string Icon { get; }

		public InlineButtonAttribute(string method, string label = null, string icon = null) {
			Method = method;
			Label = label;
			Icon = icon;
		}
	}
}
