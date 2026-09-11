using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Marks a numeric field with a unit of measure in a <see cref="SInspectorAttribute"/> inspector. Three
	/// forms:
	/// <list type="bullet">
	/// <item><c>[Unit("dmg/s")]</c> — a plain static suffix label.</item>
	/// <item><c>[Unit(UnitOfMeasure.Meters)]</c> — a suffix label from the enum.</item>
	/// <item><c>[Unit(UnitOfMeasure.Meters, UnitOfMeasure.Feet)]</c> — value is in <c>unit</c>, plus a
	/// read-only companion row showing it converted to <c>displayAs</c>.</item>
	/// </list>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class UnitAttribute : Attribute {

		/// <summary>The unit the stored value is in. <see cref="UnitOfMeasure.None"/> when the string ctor was used.</summary>
		public UnitOfMeasure Unit { get; }

		/// <summary>Optional second unit for the converted companion row. <see cref="UnitOfMeasure.None"/> = off.</summary>
		public UnitOfMeasure DisplayAs { get; }

		/// <summary>Literal suffix text, set only when the string ctor was used.</summary>
		public string Custom { get; }

		public UnitAttribute(string custom) {
			Custom = custom;
			Unit = UnitOfMeasure.None;
			DisplayAs = UnitOfMeasure.None;
		}

		public UnitAttribute(UnitOfMeasure unit) {
			Unit = unit;
			DisplayAs = UnitOfMeasure.None;
		}

		public UnitAttribute(UnitOfMeasure unit, UnitOfMeasure displayAs) {
			Unit = unit;
			DisplayAs = displayAs;
		}
	}
}
