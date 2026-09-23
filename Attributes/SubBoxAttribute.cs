using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Starts a smaller box nested inside a <see cref="BoxAttribute"/> box (or on its own). The first field
	/// carrying this attribute opens the sub-box; every following field is drawn inside it until the next
	/// <c>[SubBox]</c>, an <see cref="EndSubBoxAttribute"/>, or the end of the surrounding <c>[Box]</c>
	/// (<c>[Box]</c> / <c>[Header]</c> / <see cref="EndGroupAttribute"/>). Sub-boxes do not nest.
	/// Use <c>[EndSubBox]</c> on the first field after the group to leave it without opening a new one.
	///
	/// <para><see cref="ConditionalAttribute"/> for <c>UNITY_EDITOR</c> — stripped from player builds, no
	/// <c>#if</c> needed at call sites.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class SubBoxAttribute : Attribute {

		/// <summary>Optional header drawn on the sub-box. <c>null</c> = frame only.</summary>
		public string Label { get; }

		/// <summary>When <c>true</c> the sub-box is a collapsible chevron section (state remembered in <c>EditorPrefs</c>).</summary>
		public bool Collapsable { get; }

		/// <summary>Initial expanded state the first time the sub-box is shown (only with <see cref="Collapsable"/>).</summary>
		public bool Expanded { get; }

		/// <summary>Accent bar colour as HTML string (e.g. <c>"#4ecdc4"</c>). <c>null</c> = fall back to <see cref="SidebarTint"/>.</summary>
		public string SidebarColor { get; }

		/// <summary>Accent bar colour from the <see cref="Sperlich.EditorKit.TintColor"/> palette.</summary>
		public TintColor SidebarTint { get; }

		/// <summary>When <c>true</c> the sub-box is drawn as a flat header row with indented content instead of an outlined frame.</summary>
		public bool Flat { get; }

		/// <summary>Name of a string member (field, property or parameterless method) shown right-aligned in the
		/// header, e.g. a short summary of the values inside. Refreshed while the inspector is open.</summary>
		public string Summary { get; }

		public SubBoxAttribute(string label = null, bool collapsable = false, bool expanded = true,
			string sidebarColor = null, TintColor sidebarTint = TintColor.None, bool flat = false, string summary = null) {
			Flat = flat;
			Summary = summary;
			Label = label;
			Collapsable = collapsable;
			Expanded = expanded;
			SidebarColor = sidebarColor;
			SidebarTint = sidebarTint;
		}
	}
}
