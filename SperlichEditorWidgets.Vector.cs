using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Per-axis component names for the vector <see cref="SerializedPropertyType"/>s, in display
		/// order. Empty for anything else.</summary>
		private static string[] VectorComponents(SerializedPropertyType type) => type switch {
			SerializedPropertyType.Vector2 or SerializedPropertyType.Vector2Int => new[] { "x", "y" },
			SerializedPropertyType.Vector3 or SerializedPropertyType.Vector3Int => new[] { "x", "y", "z" },
			SerializedPropertyType.Vector4 => new[] { "x", "y", "z", "w" },
			_ => System.Array.Empty<string>(),
		};

		/// <summary>
		/// Compact per-axis drag-number row for a Vector2/3/4/2Int/3Int property — each axis gets the same
		/// <see cref="CreateDragNumberField"/> grip-and-scrub control the rest of a Sperlich inspector uses,
		/// with a small axis letter in front, instead of Unity's default compound Vector field (which has no
		/// Sperlich drag grip and doesn't line up with the shared label column).
		/// </summary>
		public static VisualElement CreateVectorField(SerializedProperty prop) {
			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
			string[] comps = VectorComponents(prop.propertyType);

			for (int i = 0; i < comps.Length; i++) {
				SerializedProperty sub = prop.FindPropertyRelative(comps[i]);
				if (sub == null) continue;

				var cell = new VisualElement {
					style = {
						flexDirection = FlexDirection.Row, alignItems = Align.Center,
						flexGrow = 1, flexBasis = 0, minWidth = 0,
						marginRight = i < comps.Length - 1 ? 4 : 0,
					}
				};
				cell.Add(new Label(comps[i].ToUpperInvariant()) {
					pickingMode = PickingMode.Ignore,
					style = {
						fontSize = 9, width = 8, flexShrink = 0, marginRight = 2,
						color = SperlichEditorTheme.TextMuted, unityTextAlign = TextAnchor.MiddleCenter,
					}
				});

				VisualElement field = CreateDragNumberField(sub);
				field.style.flexGrow = 1;
				field.style.minWidth = 0;
				cell.Add(field);
				row.Add(cell);
			}
			return row;
		}
	}
}
