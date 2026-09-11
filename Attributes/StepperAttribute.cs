using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Renders a number field as a [-] [value] [+] stepper instead of a drag field, in a
	/// <see cref="SInspectorAttribute"/> inspector. Click = <paramref name="step"/>, shift-click = 10x step.
	///
	/// <code>[Stepper(1f, 0f, 99f)] public int ammoCount;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class StepperAttribute : Attribute {

		public float Step { get; }
		public float Min { get; }
		public float Max { get; }

		public StepperAttribute(float step = 1f, float min = float.MinValue, float max = float.MaxValue) {
			Step = step;
			Min = min;
			Max = max;
		}
	}
}
