using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary><c>[TabGroup]</c> emit for <see cref="SperlichInspectorEngine"/>: fields sharing a group key
	/// (not necessarily contiguous) are pulled out of the normal in-order emission and rendered as one tab
	/// bar, built at the position of the first of them.</summary>
	public static partial class SperlichInspectorEngine {

		private sealed class TabGroupSpec {
			public readonly List<string> TabOrder = new();
			public readonly Dictionary<string, List<int>> IndicesByTab = new();
			public int AnchorIndex = int.MaxValue;
		}

		/// <summary>Every member index carrying a <c>[TabGroup]</c> -> its group key, plus one
		/// <see cref="TabGroupSpec"/> per group key (tab order + member indices per tab, first-seen order).</summary>
		private static (Dictionary<int, string> indexGroup, Dictionary<string, TabGroupSpec> specs) ComputeTabGroups(List<Member> members) {
			var indexGroup = new Dictionary<int, string>();
			var specs = new Dictionary<string, TabGroupSpec>();

			for (int i = 0; i < members.Count; i++) {
				string key = members[i].Meta?.TabGroupKey;
				string tab = members[i].Meta?.TabGroupTab;
				if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(tab)) continue;

				indexGroup[i] = key;
				if (!specs.TryGetValue(key, out TabGroupSpec spec)) { spec = new TabGroupSpec(); specs[key] = spec; }
				if (!spec.IndicesByTab.TryGetValue(tab, out List<int> list)) { list = new List<int>(); spec.IndicesByTab[tab] = list; spec.TabOrder.Add(tab); }
				list.Add(i);
				if (i < spec.AnchorIndex) spec.AnchorIndex = i;
			}
			return (indexGroup, specs);
		}

		/// <summary>Builds the tab bar + all tab bodies for one group. Bodies are built eagerly (not lazily on
		/// first select) and toggled via <c>display</c> — simplest way to keep every field's own live bindings
		/// (TrackPropertyValue, schedulers) alive regardless of which tab is showing.</summary>
		private static VisualElement BuildTabGroupBlock(string groupKey, TabGroupSpec spec, List<Member> members, SperlichFieldColumn col, SerializedObject so, SperlichInspectorPlan plan) {
			string typeName = so.targetObject != null ? so.targetObject.GetType().Name : "x";
			string persistKey = typeName + "/tabgroup/" + groupKey;
			int selected = Mathf.Clamp(EditorPrefs.GetInt(persistKey, 0), 0, Mathf.Max(0, spec.TabOrder.Count - 1));

			var root = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			root.style.backgroundColor = SperlichEditorTheme.BgStepBody;
			root.style.marginTop = 2;
			root.style.marginBottom = 4;
			root.style.paddingTop = 0;
			root.style.paddingBottom = 6;
			root.style.paddingLeft = 0;
			root.style.paddingRight = 0;
			root.style.overflow = Overflow.Visible;

			var bar = new VisualElement {
				style = { flexDirection = FlexDirection.Row, borderBottomWidth = 1, borderBottomColor = SperlichEditorTheme.BorderSubtle }
			};
			root.Add(bar);

			var bodies = new List<VisualElement>();
			for (int t = 0; t < spec.TabOrder.Count; t++) {
				string tabName = spec.TabOrder[t];
				var body = new VisualElement { style = { paddingLeft = 8, paddingRight = 8, paddingTop = 6, display = t == selected ? DisplayStyle.Flex : DisplayStyle.None } };
				foreach (int idx in spec.IndicesByTab[tabName]) {
					EmitMemberAt(body, col, members, idx, so, plan, tightHeader: true);
				}
				bodies.Add(body);
				root.Add(body);
			}

			var tabButtons = new List<(VisualElement underline, Label lbl)>();

			void Select(int t) {
				selected = t;
				EditorPrefs.SetInt(persistKey, t);
				for (int k = 0; k < bodies.Count; k++) bodies[k].style.display = k == t ? DisplayStyle.Flex : DisplayStyle.None;
				for (int k = 0; k < tabButtons.Count; k++) {
					bool active = k == t;
					tabButtons[k].underline.style.backgroundColor = active ? Accent : Color.clear;
					tabButtons[k].lbl.style.color = active ? SperlichEditorTheme.TextPrimary : SperlichEditorTheme.TextMuted;
				}
			}

			for (int t = 0; t < spec.TabOrder.Count; t++) {
				int captured = t;
				var seg = new VisualElement { pickingMode = PickingMode.Position, style = { paddingLeft = 10, paddingRight = 10, paddingTop = 6, paddingBottom = 6 } };
				SperlichEditorWidgets.SetHoverCursor(seg, MouseCursor.Link);
				var lbl = new Label(spec.TabOrder[t]) { pickingMode = PickingMode.Ignore, style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
				seg.Add(lbl);
				var underline = new VisualElement { pickingMode = PickingMode.Ignore, style = { height = 2, marginTop = 2 } };
				var segWrap = new VisualElement();
				segWrap.Add(seg);
				segWrap.Add(underline);
				seg.RegisterCallback<ClickEvent>(_ => Select(captured));
				bar.Add(segWrap);
				tabButtons.Add((underline, lbl));
			}
			Select(selected);
			return root;
		}
	}
}
