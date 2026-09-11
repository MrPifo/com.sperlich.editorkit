using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary><c>[Box]</c> run detection and container construction for <see cref="SperlichInspectorEngine"/>.</summary>
	public static partial class SperlichInspectorEngine {

		/// <summary>Pre-scan: maps the index of a field that STARTS a <c>[Box]</c> to the exclusive end index
		/// of its run plus the box attribute and the first field's name (used for the persist key). A run
		/// ends at the next <c>[Box]</c>, the next <c>[Header]</c>, an <c>[EndGroup]</c>, or the last field.
		/// Boxes do not nest.</summary>
		private static Dictionary<int, (int end, BoxAttribute box, string firstName)> ComputeBoxSpans(List<Member> members) {
			var spans = new Dictionary<int, (int, BoxAttribute, string)>();

			for (int i = 0; i < members.Count; i++) {
				BoxAttribute box = members[i].Meta?.Box;
				if (box == null) continue;

				int end = members.Count;
				for (int j = i + 1; j < members.Count; j++) {
					SperlichInspectorPlan.MemberMeta mj = members[j].Meta;
					if (mj == null) continue;
					if (mj.Box != null || !string.IsNullOrEmpty(mj.Header) || mj.EndGroup) { end = j; break; }
				}

				spans[i] = (end, box, members[i].Name);
				i = end - 1; // no nesting — resume scanning at the run's end
			}

			return spans;
		}

		/// <summary>Creates the box shell, adds it to <paramref name="parent"/> and returns the body element
		/// the following field rows should be routed into.</summary>
		private static VisualElement BuildBoxContainer(VisualElement parent, SerializedObject so, BoxAttribute box, string firstFieldName) {
			string title = box.Label;
			VisualElement body;

			if (box.Collapsable) {
				string typeName = so.targetObject != null ? so.targetObject.GetType().Name : "x";
				string persistKey = typeName + "/box/" + firstFieldName;
				var (header, chevronBody, _) = SperlichEditorWidgets.CreateChevronSection(
					string.IsNullOrEmpty(title) ? "Box" : title,
					box.Expanded, SperlichEditorTheme.BgStep, SperlichEditorTheme.BgStepBody, persistKey);

				var shell = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
				shell.style.marginTop = 3;
				shell.style.marginBottom = 3;
				shell.Add(header);
				chevronBody.style.paddingLeft = 6;
				chevronBody.style.paddingRight = 6;
				chevronBody.style.paddingTop = 0; // BuildInto re-adds 3 unless the first child is a bleed [Header]
				chevronBody.style.paddingBottom = 4;
				shell.Add(chevronBody);
				parent.Add(shell);
				body = chevronBody;
			} else {
				var boxEl = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
				boxEl.style.backgroundColor = SperlichEditorTheme.BgStepBody;
				boxEl.style.marginTop = 3;
				boxEl.style.marginBottom = 3;

				if (!string.IsNullOrEmpty(title)) {
					// Full-bleed BgStep header strip, same colour split as the chevron / [Header] strips.
					boxEl.Add(new Label(title.ToUpperInvariant()) {
						style = {
							unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11,
							color = SperlichEditorTheme.TextSecondary,
							backgroundColor = SperlichEditorTheme.BgStep,
							paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4,
							borderBottomWidth = 1, borderBottomColor = SperlichEditorTheme.BorderSubtle,
						}
					});
					var contentBody = new VisualElement {
						style = { paddingLeft = 6, paddingRight = 6, paddingTop = 0, paddingBottom = 4 }
					};
					boxEl.Add(contentBody);
					parent.Add(boxEl);
					body = contentBody;
				} else {
					boxEl.style.paddingLeft = 6;
					boxEl.style.paddingRight = 6;
					boxEl.style.paddingTop = 3;
					boxEl.style.paddingBottom = 4;
					parent.Add(boxEl);
					body = boxEl;
				}
			}

			if (box.ReadOnly) body.SetEnabled(false);
			return body;
		}
	}
}
