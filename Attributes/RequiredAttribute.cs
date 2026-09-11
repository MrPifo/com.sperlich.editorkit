using System;
using System.Diagnostics;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Flags an object-reference field as mandatory, in a <see cref="SInspectorAttribute"/> inspector. While
	/// the value is <c>null</c>/<c>None</c> the field gets a red outline plus a small inline error message.
	/// Purely a visual nudge — it does not block Apply/Play.
	///
	/// <code>[Required] public Transform target;
	/// [Required("Needs a spawn point")] public Transform spawnPoint;</code>
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class RequiredAttribute : Attribute {

		/// <summary>Custom message; <c>null</c> = default "&lt;Field&gt; is required".</summary>
		public string Message { get; }

		public RequiredAttribute(string message = null) {
			Message = message;
		}
	}
}
