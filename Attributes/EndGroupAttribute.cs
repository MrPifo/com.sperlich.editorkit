using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>Closes the current <see cref="BoxAttribute"/> box early. Put it on the first field that
	/// should render outside the box again. Marker attribute — no parameters.</summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class EndGroupAttribute : Attribute { }
}
