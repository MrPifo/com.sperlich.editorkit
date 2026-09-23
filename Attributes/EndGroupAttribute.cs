using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>Closes the current <see cref="BoxAttribute"/> box early — and any <see cref="SubBoxAttribute"/>
	/// open inside it. Put it on the FIRST field that should render outside the box again; it takes effect
	/// before that field. To close only a sub-box use <see cref="EndSubBoxAttribute"/>. Marker attribute.
	///
	/// <code>[Box("Movement")] public float speed;
	/// public float turnRate;
	/// [EndGroup] public float unrelated;   // drawn outside the box</code></summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class EndGroupAttribute : Attribute { }
}
