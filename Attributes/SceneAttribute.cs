using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Turns a <c>string</c> or <c>int</c> field into a scene picker in a <see cref="SInspectorAttribute"/>
	/// inspector, listing the enabled scenes from Build Settings. A <c>string</c> field stores the scene
	/// name (or asset path with <see cref="UseFullPath"/>); an <c>int</c> field stores the build index.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class SceneAttribute : Attribute {

		/// <summary>For a <c>string</c> field: store the full asset path instead of just the scene name.</summary>
		public bool UseFullPath { get; }

		public SceneAttribute(bool useFullPath = false) => UseFullPath = useFullPath;
	}
}
