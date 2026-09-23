using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>Closes the current <see cref="SubBoxAttribute"/> early. Put it on the FIRST field that should
	/// render outside the sub-box again; it takes effect before that field and the field stays inside the
	/// surrounding <c>[Box]</c>. To close the whole box use <see cref="EndGroupAttribute"/>. Marker attribute.
	///
	/// <code>[SubBox("Pellets")] public int amount;
	/// public float velocity;
	/// [EndSubBox] public float patrolDuration;   // back in the outer box</code></summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class EndSubBoxAttribute : Attribute { }
}
