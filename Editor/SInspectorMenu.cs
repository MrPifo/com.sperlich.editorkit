using UnityEditor;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Global editor toggles for the <see cref="SInspectorAttribute"/> auto-inspector. Currently just the
	/// "show the Script reference row" switch, OR-combined with the per-type
	/// <c>[SInspector(showScript: true)]</c> flag.
	/// </summary>
	internal static class SInspectorMenu {

		private const string ShowScriptKey = "Sperlich.SInspector/ShowScriptField";
		private const string ShowScriptMenu = "Tools/Sperlich/SInspector/Show Script Field";

		/// <summary>When <c>true</c> the "Script" row is shown on every <c>[SInspector]</c> type.</summary>
		public static bool ShowScriptField {
			get => EditorPrefs.GetBool(ShowScriptKey, false);
			set => EditorPrefs.SetBool(ShowScriptKey, value);
		}

		[MenuItem(ShowScriptMenu, priority = 100)]
		private static void ToggleShowScript() => ShowScriptField = !ShowScriptField;

		[MenuItem(ShowScriptMenu, validate = true)]
		private static bool ToggleShowScriptValidate() {
			Menu.SetChecked(ShowScriptMenu, ShowScriptField);
			return true;
		}
	}
}
