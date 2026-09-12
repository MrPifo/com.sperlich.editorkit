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

		/// <summary>Optional accent bar drawn on the left edge of the box's header (collapsable) or the whole
		/// box (non-collapsable), as an HTML colour string (e.g. <c>"#4ecdc4"</c>). <c>null</c> = fall back to
		/// <see cref="SidebarTint"/>, then <see cref="SidebarR"/>.</summary>
		public string SidebarColor { get; }

		/// <summary>Sidebar colour from the <see cref="Sperlich.EditorKit.TintColor"/> palette — the easiest way
		/// to pick a sidebar colour without hunting for a hex code. Only used when <see cref="SidebarColor"/> is
		/// <c>null</c>.</summary>
		public TintColor SidebarTint { get; }

		/// <summary>Sidebar colour as raw RGBA channels (0-1) instead of a hex string or palette entry —
		/// attributes can't take a <c>UnityEngine.Color</c> directly since it isn't a constant-expressible type.
		/// Only used when <see cref="SidebarColor"/> is <c>null</c>, <see cref="SidebarTint"/> is
		/// <see cref="Sperlich.EditorKit.TintColor.None"/>, and <see cref="SidebarR"/> is not negative (the
		/// sentinel for "unset").</summary>
		public float SidebarR { get; }
		public float SidebarG { get; }
		public float SidebarB { get; }
		public float SidebarA { get; }

		public BoxAttribute(string label = null, bool collapsable = false, bool expanded = true, bool readOnly = false,
			string sidebarColor = null, TintColor sidebarTint = TintColor.None,
			float sidebarR = -1f, float sidebarG = 0f, float sidebarB = 0f, float sidebarA = 1f) {
			Label = label;
			Collapsable = collapsable;
			Expanded = expanded;
			ReadOnly = readOnly;
			SidebarColor = sidebarColor;
			SidebarTint = sidebarTint;
			SidebarR = sidebarR;
			SidebarG = sidebarG;
			SidebarB = sidebarB;
			SidebarA = sidebarA;
		}
	}
}
