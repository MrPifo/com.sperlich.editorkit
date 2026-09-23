using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Rotary dial control for a number field, in a <see cref="SInspectorAttribute"/> inspector. Either a
	/// custom <c>[min, max]</c> range, or one of the <see cref="KnobRange"/> presets for the common angle
	/// conventions (0..360°, -180..180°, 0..2π, -π..π) so an angle field doesn't need to spell those out.
	///
	/// <code>[Knob(0f, 20f)] public float volume;
	/// [Knob(KnobRange.DegreesSigned180)] public float heading;
	/// [Knob(KnobRange.RadiansSignedPi)] public float headingRad;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class KnobAttribute : Attribute {

		public float Min { get; }
		public float Max { get; }
		/// <summary>Dial size in pixels. Set it positionally on the <c>(min, max)</c> overload, or by name on the
		/// preset overload: <c>[Knob(KnobRange.Unit01, Diameter = 60)]</c>.</summary>
		public float Diameter { get; set; } = 42f;

		public KnobAttribute(float min, float max, float diameter = 42f) {
			Min = min;
			Max = max;
			Diameter = diameter;
		}

		// No positional diameter here: with one, the literal 0 converts to KnobRange, so [Knob(0, 90)] silently
		// bound to (KnobRange.Degrees0To360, diameter: 90) instead of the (min, max) overload.
		public KnobAttribute(KnobRange range) {
			(Min, Max) = range switch {
				KnobRange.Degrees0To360 => (0f, 360f),
				KnobRange.DegreesSigned180 => (-180f, 180f),
				KnobRange.Radians0To2Pi => (0f, 6.2831855f),
				KnobRange.RadiansSignedPi => (-3.14159265f, 3.14159265f),
				KnobRange.Unit01 => (0f, 1f),
				_ => (0f, 1f),
			};
		}
	}
}
