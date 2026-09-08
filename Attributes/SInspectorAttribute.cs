using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Put this on a <see cref="UnityEngine.MonoBehaviour"/> or <see cref="UnityEngine.ScriptableObject"/>
	/// class to render its whole inspector in the Sperlich EditorKit style (dark neon theme, pill toggles,
	/// flat dropdowns, drag-number fields) instead of Unity's default inspector. Embedded Sperlich drawers
	/// (e.g. <c>SEvent</c>) then blend in seamlessly.
	///
	/// <para>Without this attribute nothing changes — the class keeps rendering with Unity's default
	/// inspector. A type that already has its own <c>[CustomEditor]</c> (e.g. <c>SText</c>) is never
	/// affected.</para>
	///
	/// <para>Marked <see cref="ConditionalAttribute"/> for <c>UNITY_EDITOR</c>: in a player build the C#
	/// compiler drops every usage of this attribute from the IL automatically, so game code never needs an
	/// <c>#if UNITY_EDITOR</c> guard around it.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class SInspectorAttribute : Attribute { }
}
