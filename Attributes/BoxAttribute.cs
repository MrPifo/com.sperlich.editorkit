using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Starts a visual box in a <see cref="SInspectorAttribute"/> inspector. The first field carrying this
	/// attribute opens the box; every following field is drawn inside it until the next <c>[Box]</c>, the
	/// next <c>[Header]</c>, or an explicit <see cref="EndGroupAttribute"/>. Boxes do not nest.
	///
	/// <para><see cref="ConditionalAttribute"/> for <c>UNITY_EDITOR</c> — stripped from player builds, no
	/// <c>#if</c> needed at call sites.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class BoxAttribute : Attribute {

		/// <summary>Optional bold header drawn on the box. <c>null</c> = frame only.</summary>
		public string Label { get; }

		/// <summary>When <c>true</c> the box is a collapsible chevron section; its open state is remembered
		/// per inspector in <c>EditorPrefs</c> and survives reselect / domain reload.</summary>
		public bool Collapsable { get; }

		/// <summary>Initial expanded state the first time the box is shown (only meaningful together with
		/// <see cref="Collapsable"/>).</summary>
		public bool Expanded { get; }

		/// <summary>Disables every control inside the box (display only, values stay editable via code).</summary>
		public bool ReadOnly { get; }

		public BoxAttribute(string label = null, bool collapsable = false, bool expanded = true, bool readOnly = false) {
			Label = label;
			Collapsable = collapsable;
			Expanded = expanded;
			ReadOnly = readOnly;
		}
	}
}
