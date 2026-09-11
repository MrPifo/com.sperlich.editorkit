namespace Sperlich.EditorKit {

	/// <summary>Width preset for <see cref="SButtonAttribute"/>. The same enum doubles as the height preset:
	/// as a height value <see cref="Full"/> behaves like <see cref="Large"/>.</summary>
	public enum ButtonSize {
		Small,
		Normal,
		Large,
		/// <summary>Stretch to 100% width. Used as a height value it maps to <see cref="Large"/>.</summary>
		Full,
	}

	/// <summary>Horizontal placement of a non-stretched button inside its row.</summary>
	public enum ButtonAnchor {
		Left,
		Center,
		Right,
	}

	/// <summary>Line rendering style for <see cref="HLineAttribute"/>.</summary>
	public enum LineStyle {
		Solid,
		Dashed,
		Dotted,
	}

	/// <summary>
	/// Curated unit list for <see cref="UnitAttribute"/>. Auto-conversion only runs between units of the
	/// same dimension (length / mass / time / speed / angle). SI base units per dimension: metre, kilogram,
	/// second, metre-per-second, radian.
	/// </summary>
	public enum UnitOfMeasure {
		None,

		// Length (base: metre)
		Millimeters,
		Centimeters,
		Meters,
		Kilometers,
		Inches,
		Feet,
		Miles,

		// Mass (base: kilogram)
		Grams,
		Kilograms,
		Tonnes,
		Pounds,

		// Time (base: second)
		Milliseconds,
		Seconds,
		Minutes,
		Hours,

		// Speed (base: metre per second)
		MetersPerSecond,
		KilometersPerHour,
		MilesPerHour,

		// Angle (base: radian)
		Degrees,
		Radians,
	}

	/// <summary>Preset palette for <see cref="TagAttribute"/> — attribute constructors can only take constant
	/// types (no <c>Color</c>), so the palette lives here and the engine maps it to a theme color.</summary>
	public enum TagColor {
		Gray,
		Blue,
		Cyan,
		Teal,
		Green,
		Yellow,
		Orange,
		Red,
		Pink,
		Purple,
	}

	/// <summary>Severity / color for <see cref="InfoBoxAttribute"/>.</summary>
	public enum InfoBoxType {
		Info,
		Warning,
		Error,
	}

	/// <summary>Value-range preset for <see cref="KnobAttribute"/> — covers the common angle conventions so a
	/// field doesn't need to spell out <c>min</c>/<c>max</c> for a plain heading/rotation value.</summary>
	public enum KnobRange {
		/// <summary><c>min</c>/<c>max</c> passed explicitly to the attribute.</summary>
		Custom,
		/// <summary>0..360 degrees.</summary>
		Degrees0To360,
		/// <summary>-180..180 degrees.</summary>
		DegreesSigned180,
		/// <summary>0..2π radians.</summary>
		Radians0To2Pi,
		/// <summary>-π..π radians.</summary>
		RadiansSignedPi,
		/// <summary>0..1 normalized.</summary>
		Unit01,
	}
}
