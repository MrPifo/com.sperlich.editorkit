using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Lays consecutive fields out side by side on one horizontal line in a <see cref="SInspectorAttribute"/>
	/// inspector. Put it on each field that should share the row:
	///
	/// <code>
	/// [SRow] public int marginLeft;
	/// [SRow] public int marginRight;
	/// [SRow] public int marginTop;
	/// [SRow] public int marginBottom;
	/// </code>
	///
	/// <para>Fields with the same <see cref="Group"/> key are grouped (an empty key groups every adjacent
	/// <c>[SRow]</c> field). Only scalar fields inline — bool, int, float, string, enum, color; anything
	/// wider (vectors, object references, lists, nested types) renders on its own row as usual. A run is
	/// capped at six cells per visible line and wraps beyond that.</para>
	///
	/// <para><see cref="ConditionalAttribute"/> for <c>UNITY_EDITOR</c> — no <c>#if</c> needed at call sites,
	/// stripped from player builds.</para>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class SRowAttribute : Attribute {

		/// <summary>Fields sharing this key (and adjacent in declaration order) go on the same line. Empty =
		/// group with any neighbouring <c>[SRow]</c> field.</summary>
		public string Group { get; }

		public SRowAttribute(string group = "") => Group = group ?? "";
	}
}
