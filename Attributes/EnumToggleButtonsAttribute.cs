using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Renders an enum field as a row of toggle buttons instead of a dropdown in a
	/// <see cref="SInspectorAttribute"/> inspector. A plain enum becomes a single-select segmented control;
	/// a <c>[Flags]</c> enum becomes a multi-select toggle bar.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class EnumToggleButtonsAttribute : Attribute {

		/// <summary>Row label override. <c>null</c> = the field's display name.</summary>
		public string Label { get; }

		/// <summary>Maximum number of buttons per row before breaking into the next row. &lt;= 0 means all in a single row.</summary>
		public int ButtonsPerRow { get; }

		public EnumToggleButtonsAttribute(string label = null, int buttonsPerRow = 0) {
			Label = label;
			ButtonsPerRow = buttonsPerRow;
		}

		public EnumToggleButtonsAttribute(int buttonsPerRow) {
			Label = null;
			ButtonsPerRow = buttonsPerRow;
		}
	}
}
