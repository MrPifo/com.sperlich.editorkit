using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>
		/// Flat Sperlich multi-select dropdown for a <c>LayerMask</c> <see cref="SerializedProperty"/>
		/// (int-backed): the named layers plus <c>Nothing</c> / <c>Everything</c>, same look as
		/// <see cref="CreateFlagsDropdown"/>. Layer entries are sorted alphabetically (Sperlich convention —
		/// only real C# enums keep declaration order); the true bit index is preserved for the mask maths.
		/// </summary>
		public static VisualElement CreateLayerMaskDropdown(SerializedProperty maskProp, Color? accent = null) {
			var layers = new List<(int index, string name)>();
			for (int i = 0; i < 32; i++) {
				string n = LayerMask.LayerToName(i);
				if (!string.IsNullOrEmpty(n)) layers.Add((i, n));
			}
			layers.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

			int AllMask() {
				int m = 0;
				foreach ((int index, string _) in layers) m |= 1 << index;
				return m;
			}

			int TotalOptions() => layers.Count + 2; // 0 = Nothing, 1 = Everything, 2.. = layers

			string OptionLabel(int i) => i switch {
				0 => "Nothing",
				1 => "Everything",
				_ => layers[i - 2].name,
			};

			string OptionBadge(int i) => i < 2 ? null : layers[i - 2].index.ToString();

			bool OptionSelected(int i) {
				int mask = maskProp.intValue;
				if (i == 0) return mask == 0;
				if (i == 1) { int all = AllMask(); return all != 0 && (mask & all) == all; }
				return (mask & (1 << layers[i - 2].index)) != 0;
			}

			void ToggleOption(int i) {
				int mask = maskProp.intValue;
				if (i == 0) {
					mask = 0;
				} else if (i == 1) {
					int all = AllMask();
					mask = (mask & all) == all ? 0 : all;
				} else {
					int bit = 1 << layers[i - 2].index;
					mask = (mask & bit) != 0 ? mask & ~bit : mask | bit;
				}
				maskProp.intValue = mask;
				maskProp.serializedObject.ApplyModifiedProperties();
			}

			string Header() {
				if (maskProp.hasMultipleDifferentValues) return "—";
				int mask = maskProp.intValue;
				if (mask == 0) return "Nothing";
				int all = AllMask();
				if (all != 0 && (mask & all) == all && (mask & ~all) == 0) return "Everything";
				var active = new List<string>();
				foreach ((int index, string name) in layers) {
					if ((mask & (1 << index)) != 0) active.Add(name);
				}
				if (active.Count == 0) return "Mixed …";
				return active.Count == 1 ? active[0] : string.Join(", ", active);
			}

			VisualElement dd = BuildMultiSelectDropdown(TotalOptions, OptionLabel, OptionSelected, ToggleOption, Header, accent, OptionBadge);
			dd.TrackPropertyValue(maskProp, _ => {
				Label lbl = dd.Q<Label>();
				if (lbl != null) lbl.text = Header();
			});
			return dd;
		}
	}
}
