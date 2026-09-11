using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Draws a field disabled (dimmed, not editable) in a <see cref="SInspectorAttribute"/> inspector.
	///
	/// <para>Named <c>SReadOnly</c> (not <c>ReadOnly</c>) to avoid colliding with
	/// <c>System.ComponentModel.ReadOnlyAttribute</c> and hand-rolled project <c>ReadOnlyAttribute</c>s.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class SReadOnlyAttribute : Attribute { }
}
