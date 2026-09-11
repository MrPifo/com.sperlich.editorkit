using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Draws a clickable button for a parameterless method in a <see cref="SInspectorAttribute"/> inspector.
	/// By default the button sits at the method's declaration position relative to the surrounding members;
	/// <see cref="After"/> / <see cref="Before"/> pin it next to a named serialized field instead, so the
	/// method body can live anywhere in the file without moving the button.
	///
	/// <para>Named <c>SButton</c> (not <c>Button</c>) to avoid colliding with
	/// <c>UnityEngine.UIElements.Button</c> / <c>UnityEngine.UI.Button</c>.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class SButtonAttribute : Attribute {

		/// <summary>Button caption. <c>null</c> = the method name, prettified.</summary>
		public string Label { get; }

		/// <summary>Width preset. <see cref="ButtonSize.Full"/> (default) stretches to the row width.</summary>
		public ButtonSize Size { get; }

		/// <summary>Height preset.</summary>
		public ButtonSize Height { get; }

		/// <summary>Horizontal placement when <see cref="Size"/> is not <see cref="ButtonSize.Full"/>.</summary>
		public ButtonAnchor Anchor { get; }

		/// <summary>Serialized field name — the button is placed directly after that field's row.</summary>
		public string After { get; }

		/// <summary>Serialized field name — the button is placed directly before that field's row.</summary>
		public string Before { get; }

		/// <summary>Optional Unity builtin icon name (resolved via <c>EditorGUIUtility.IconContent</c>).</summary>
		public string Icon { get; }

		public SButtonAttribute(string label = null, ButtonSize size = ButtonSize.Full, ButtonSize height = ButtonSize.Normal,
			ButtonAnchor anchor = ButtonAnchor.Center, string after = null, string before = null, string icon = null) {
			Label = label;
			Size = size;
			Height = height;
			Anchor = anchor;
			After = after;
			Before = before;
			Icon = icon;
		}
	}
}
