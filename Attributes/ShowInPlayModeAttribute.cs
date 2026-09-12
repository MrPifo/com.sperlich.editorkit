using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Only displays this element in the inspector during Play Mode (Application.isPlaying is true).
	/// Hidden in Edit Mode.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class ShowInPlayModeAttribute : Attribute { }

	/// <summary>
	/// Alias for <see cref="ShowInPlayModeAttribute"/> matching the Odin Inspector naming convention.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class HideInEditorModeAttribute : ShowInPlayModeAttribute { }
}
