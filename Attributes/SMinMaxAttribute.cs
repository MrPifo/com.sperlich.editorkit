using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// On a <c>Vector2</c> / <c>Vector2Int</c> field in a <see cref="SInspectorAttribute"/> inspector:
	/// renders a dual-handle "from–to" range slider bounded to <see cref="Min"/>..<see cref="Max"/>, where
	/// <c>x</c> is the low end and <c>y</c> the high end (always kept <c>x ≤ y</c> and inside the bounds).
	///
	/// <code>
	/// [SMinMax(0f, 10f)] public Vector2 spawnRange = new(2f, 8f);
	/// </code>
	///
	/// <para>Without this attribute a <c>Vector2</c> renders as the normal X/Y row.
	/// <see cref="ConditionalAttribute"/> for <c>UNITY_EDITOR</c> — no <c>#if</c> needed at call sites.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class SMinMaxAttribute : Attribute {

		public float Min { get; }
		public float Max { get; }

		public SMinMaxAttribute(float min, float max) {
			Min = min;
			Max = max;
		}
	}
}
