using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Assigns a field to a tab of a tab bar, in a <see cref="SInspectorAttribute"/> inspector. Fields sharing
	/// the same <paramref name="group"/> are collected into one tab bar (built where the first of them appears
	/// in the class) with one tab per distinct <paramref name="tab"/> name, in first-seen order. Fields need
	/// not be declared contiguously. Selected tab persists (per group, per type) across reselect and domain
	/// reload, like <see cref="BoxAttribute"/>'s collapse state.
	///
	/// <code>[TabGroup("Movement")] public float moveSpeed;
	/// [TabGroup("Movement")] public float turnSpeed;
	/// [TabGroup("Combat")] public int damage;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class TabGroupAttribute : Attribute {

		public string Tab { get; }
		public string Group { get; }

		public TabGroupAttribute(string tab, string group = "Tabs") {
			Tab = tab;
			Group = group;
		}
	}
}
