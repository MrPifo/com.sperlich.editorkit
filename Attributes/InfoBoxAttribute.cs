using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Info/warning/error help box above the field, in a <see cref="SInspectorAttribute"/> inspector.
	/// Stackable (<c>AllowMultiple</c>). Optionally shown only while <paramref name="visibleIf"/> (a member on
	/// the same object) is truthy — same "truthy" rule as a value-less <see cref="ShowIfAttribute"/>.
	///
	/// <code>[InfoBox("This value is recalculated at runtime.")]
	/// [InfoBox("Chaos mode is experimental.", InfoBoxType.Warning, visibleIf: nameof(chaosMode))]
	/// public float speed;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class InfoBoxAttribute : Attribute {

		public string Message { get; }
		public InfoBoxType Type { get; }
		/// <summary><c>null</c> = always shown.</summary>
		public string VisibleIf { get; }

		public InfoBoxAttribute(string message, InfoBoxType type = InfoBoxType.Info, string visibleIf = null) {
			Message = message;
			Type = type;
			VisibleIf = visibleIf;
		}
	}
}
