using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Renders a <c>float</c> field's <c>[valueMin, valueMax]</c> range as an editable 0-100% slider + typed
	/// field, in a <see cref="SInspectorAttribute"/> inspector. The serialized value stays in its normal
	/// domain (0..1 by default) — only the displayed/edited number is remapped.
	///
	/// <code>[Percent] public float volume; // stored 0..1, shown/edited as 0-100 %
	/// [Percent(0f, 20f)] public float damageFalloff; // stored 0..20, shown as 0-100 %</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class PercentAttribute : Attribute {

		public float ValueMin { get; }
		public float ValueMax { get; }

		public PercentAttribute(float valueMin = 0f, float valueMax = 1f) {
			ValueMin = valueMin;
			ValueMax = valueMax;
		}
	}
}
