using System;
using System.Collections.Generic;

namespace Sperlich.EditorKit {

	/// <summary>Static conversion table for <c>[Unit]</c>. Each <see cref="UnitOfMeasure"/> maps to a
	/// dimension id and a factor to that dimension's SI base unit (metre / kilogram / second /
	/// metre-per-second / radian). Conversion only succeeds between units of the same dimension.</summary>
	public static class UnitConversion {

		private static readonly Dictionary<UnitOfMeasure, (int dim, double toBase)> Table = new() {
			{ UnitOfMeasure.Millimeters,       (0, 0.001) },
			{ UnitOfMeasure.Centimeters,       (0, 0.01) },
			{ UnitOfMeasure.Meters,            (0, 1.0) },
			{ UnitOfMeasure.Kilometers,        (0, 1000.0) },
			{ UnitOfMeasure.Inches,            (0, 0.0254) },
			{ UnitOfMeasure.Feet,              (0, 0.3048) },
			{ UnitOfMeasure.Miles,             (0, 1609.344) },

			{ UnitOfMeasure.Grams,             (1, 0.001) },
			{ UnitOfMeasure.Kilograms,         (1, 1.0) },
			{ UnitOfMeasure.Tonnes,            (1, 1000.0) },
			{ UnitOfMeasure.Pounds,            (1, 0.45359237) },

			{ UnitOfMeasure.Milliseconds,      (2, 0.001) },
			{ UnitOfMeasure.Seconds,           (2, 1.0) },
			{ UnitOfMeasure.Minutes,           (2, 60.0) },
			{ UnitOfMeasure.Hours,             (2, 3600.0) },

			{ UnitOfMeasure.MetersPerSecond,   (3, 1.0) },
			{ UnitOfMeasure.KilometersPerHour, (3, 1000.0 / 3600.0) },
			{ UnitOfMeasure.MilesPerHour,      (3, 1609.344 / 3600.0) },

			{ UnitOfMeasure.Degrees,           (4, Math.PI / 180.0) },
			{ UnitOfMeasure.Radians,           (4, 1.0) },
		};

		private static readonly Dictionary<UnitOfMeasure, string> Symbols = new() {
			{ UnitOfMeasure.Millimeters, "mm" },
			{ UnitOfMeasure.Centimeters, "cm" },
			{ UnitOfMeasure.Meters, "m" },
			{ UnitOfMeasure.Kilometers, "km" },
			{ UnitOfMeasure.Inches, "in" },
			{ UnitOfMeasure.Feet, "ft" },
			{ UnitOfMeasure.Miles, "mi" },
			{ UnitOfMeasure.Grams, "g" },
			{ UnitOfMeasure.Kilograms, "kg" },
			{ UnitOfMeasure.Tonnes, "t" },
			{ UnitOfMeasure.Pounds, "lb" },
			{ UnitOfMeasure.Milliseconds, "ms" },
			{ UnitOfMeasure.Seconds, "s" },
			{ UnitOfMeasure.Minutes, "min" },
			{ UnitOfMeasure.Hours, "h" },
			{ UnitOfMeasure.MetersPerSecond, "m/s" },
			{ UnitOfMeasure.KilometersPerHour, "km/h" },
			{ UnitOfMeasure.MilesPerHour, "mph" },
			{ UnitOfMeasure.Degrees, "°" },
			{ UnitOfMeasure.Radians, "rad" },
		};

		/// <summary>Short symbol for a unit (e.g. <c>km/h</c>). Falls back to the enum name.</summary>
		public static string Symbol(UnitOfMeasure unit) => Symbols.TryGetValue(unit, out string s) ? s : unit.ToString();

		/// <summary>True when both units belong to the same dimension and can be converted into each other.</summary>
		public static bool SameDimension(UnitOfMeasure a, UnitOfMeasure b) =>
			Table.TryGetValue(a, out (int dim, double toBase) x) && Table.TryGetValue(b, out (int dim, double toBase) y) && x.dim == y.dim;

		/// <summary>Converts <paramref name="value"/> from <paramref name="from"/> to <paramref name="to"/>.
		/// Returns false (and echoes the input) on a dimension mismatch or an unknown unit.</summary>
		public static bool TryConvert(float value, UnitOfMeasure from, UnitOfMeasure to, out float result) {
			result = value;
			if (!Table.TryGetValue(from, out (int dim, double toBase) f)) return false;
			if (!Table.TryGetValue(to, out (int dim, double toBase) t)) return false;
			if (f.dim != t.dim) return false;
			result = (float)(value * f.toBase / t.toBase);
			return true;
		}
	}
}
