using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Lets an object-reference field's own inspector be expanded inline below the field, in a
	/// <see cref="SInspectorAttribute"/> inspector — edit a ScriptableObject/Component asset without leaving
	/// the parent's inspector.
	///
	/// <code>[Expandable] public BalanceData config;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class ExpandableAttribute : Attribute {

		public bool DefaultExpanded { get; }

		public ExpandableAttribute(bool defaultExpanded = false) {
			DefaultExpanded = defaultExpanded;
		}
	}
}
