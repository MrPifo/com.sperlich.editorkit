using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Reproduces Unity's prefab-override affordances for a row whose control is not a standard bound
	/// <c>PropertyField</c> (those get it from the binding system already): a blue bar in the left gutter,
	/// a bold label, and an Apply / Revert context menu. Kept as a one-call helper so every Sperlich row
	/// looks and behaves like a native one under prefab editing.
	/// </summary>
	public static class SperlichPrefabOverride {

		private static readonly Color BarColor = new Color(0.06f, 0.5f, 0.75f);

		public static void Attach(VisualElement row, Label label, SerializedProperty property) {
			SerializedProperty prop = property.Copy();

			// Absolute-positioned bar in the left gutter: no effect on the row's own layout, so bool/enum rows
			// stay aligned with the number/color rows that don't call this helper.
			row.style.position = Position.Relative;

			var bar = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = {
					position = Position.Absolute, left = -6, top = 1, bottom = 1, width = 2,
					backgroundColor = BarColor, display = DisplayStyle.None,
				}
			};
			row.Add(bar);
			bar.SendToBack();

			void Refresh() {
				bool overridden = prop.prefabOverride && !prop.isDefaultOverride;
				bar.style.display = overridden ? DisplayStyle.Flex : DisplayStyle.None;
				if (label != null) label.style.unityFontStyleAndWeight = overridden ? FontStyle.Bold : FontStyle.Normal;
			}

			row.AddManipulator(new ContextualMenuManipulator(evt => {
				if (!prop.prefabOverride) return;
				Object target = prop.serializedObject.targetObject;
				string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
				string prefabName = string.IsNullOrEmpty(prefabPath) ? "Prefab" : System.IO.Path.GetFileNameWithoutExtension(prefabPath);

				evt.menu.AppendAction($"Apply to Prefab '{prefabName}'", _ => {
					if (!string.IsNullOrEmpty(prefabPath)) {
						PrefabUtility.ApplyPropertyOverride(prop, prefabPath, InteractionMode.UserAction);
					}
				});
				evt.menu.AppendAction("Revert", _ => {
					PrefabUtility.RevertPropertyOverride(prop, InteractionMode.UserAction);
				});
			}));

			row.RegisterCallback<AttachToPanelEvent>(_ => Refresh());
			row.TrackPropertyValue(prop, _ => Refresh());
			Refresh();
		}
	}
}
