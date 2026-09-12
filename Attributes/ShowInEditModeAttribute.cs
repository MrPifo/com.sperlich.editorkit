using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Only displays this element in the inspector during Edit Mode (Application.isPlaying is false).
	/// Hidden in Play Mode.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class ShowInEditModeAttribute : Attribute { }

	/// <summary>
	/// Alias for <see cref="ShowInEditModeAttribute"/> matching the Odin Inspector naming convention.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class HideInPlayModeAttribute : ShowInEditModeAttribute { }
}
