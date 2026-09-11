using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Flat dropdown listing the enabled scenes from Build Settings, for <c>[Scene]</c>. A
		/// <c>string</c> property stores the scene name (or asset path when <paramref name="useFullPath"/>);
		/// an <c>int</c> property stores the build index. The list is re-read every time the popup opens.</summary>
		public static VisualElement CreateSceneDropdown(SerializedProperty prop, bool intMode, bool useFullPath, Color accent) {
			var paths = new List<string>();

			void Rescan() {
				paths.Clear();
				foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes) {
					if (s.enabled && !string.IsNullOrEmpty(s.path)) paths.Add(s.path);
				}
			}
			Rescan();

			string NameOf(int i) => i >= 0 && i < paths.Count ? System.IO.Path.GetFileNameWithoutExtension(paths[i]) : "—";
			string LabelFor(int i) => useFullPath ? (i >= 0 && i < paths.Count ? paths[i] : "—") : NameOf(i);

			int Selected() {
				if (intMode) {
					int v = prop.intValue;
					return v >= 0 && v < paths.Count ? v : -1;
				}
				string cur = prop.stringValue;
				if (string.IsNullOrEmpty(cur)) return -1;
				for (int i = 0; i < paths.Count; i++) {
					if (paths[i] == cur || NameOf(i) == cur) return i;
				}
				return -1;
			}

			void Pick(int i) {
				if (i < 0 || i >= paths.Count) return;
				if (intMode) {
					prop.intValue = i;
				} else {
					prop.stringValue = useFullPath ? paths[i] : NameOf(i);
				}
				prop.serializedObject.ApplyModifiedProperties();
			}

			VisualElement field = BuildDropdown(() => paths.Count, LabelFor, Selected, Pick, accent);
			field.RegisterCallback<PointerDownEvent>(_ => Rescan(), TrickleDown.TrickleDown);
			return field;
		}
	}
}
