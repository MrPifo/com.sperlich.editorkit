using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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

		/// <summary>Pre-scan for <c>[SubBox]</c>: like <see cref="ComputeBoxSpans"/>, but a run also ends at an
		/// <c>[EndSubBox]</c>. It always ends no later than the surrounding <c>[Box]</c> (which ends on
		/// <c>[Box]</c> / <c>[Header]</c> / <c>[EndGroup]</c>). Sub-boxes do not nest.</summary>
		private static Dictionary<int, (int end, SubBoxAttribute box, string firstName)> ComputeSubBoxSpans(List<Member> members) {
			var spans = new Dictionary<int, (int, SubBoxAttribute, string)>();

			for (int i = 0; i < members.Count; i++) {
				SubBoxAttribute sub = members[i].Meta?.SubBox;
				if (sub == null) continue;

				int end = members.Count;
				for (int j = i + 1; j < members.Count; j++) {
					SperlichInspectorPlan.MemberMeta mj = members[j].Meta;
					if (mj == null) continue;
					if (mj.SubBox != null || mj.EndSubBox || mj.Box != null || !string.IsNullOrEmpty(mj.Header) || mj.EndGroup) { end = j; break; }
				}

				spans[i] = (end, sub, members[i].Name);
				i = end - 1;
			}

			return spans;
		}

		/// <summary>Creates the box shell, adds it to <paramref name="parent"/> and returns the body element
		/// the following field rows should be routed into.</summary>
		private static VisualElement BuildBoxContainer(VisualElement parent, SerializedObject so, BoxAttribute box, string firstFieldName) {
			string title = box.Label;
			VisualElement body;
			Color? sidebarColor = null;
			if (!string.IsNullOrEmpty(box.SidebarColor) && ColorUtility.TryParseHtmlString(box.SidebarColor, out Color sc)) {
				sidebarColor = sc;
			} else if (box.SidebarTint != TintColor.None) {
				sidebarColor = SperlichEditorWidgets.TintColorToColor(box.SidebarTint);
			} else if (box.SidebarR >= 0f) {
				sidebarColor = new Color(box.SidebarR, box.SidebarG, box.SidebarB, box.SidebarA);
			}
			if (!string.IsNullOrEmpty(box.SidebarMember) && TryReadColorMember(so.targetObject, box.SidebarMember, out Color dynamicSidebar)) {
				sidebarColor = dynamicSidebar;
			}

			if (box.Collapsable) {
				string typeName = so.targetObject != null ? so.targetObject.GetType().Name : "x";
				string persistKey = typeName + "/box/" + firstFieldName;
				var (header, chevronBody, _) = SperlichEditorWidgets.CreateChevronSection(
					string.IsNullOrEmpty(title) ? "Box" : title,
					box.Expanded, SperlichEditorTheme.BgStep, SperlichEditorTheme.BgStepBody, persistKey, sidebarColor);

				var shell = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
				shell.style.marginTop = 3;
				shell.style.marginBottom = 3;
				shell.Add(header);
				chevronBody.style.paddingLeft = 3;
				chevronBody.style.paddingRight = 3;
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
					var titleRow = new VisualElement {
						style = {
							flexDirection = FlexDirection.Row, alignItems = Align.Stretch,
							backgroundColor = SperlichEditorTheme.BgStep,
							borderBottomWidth = 1, borderBottomColor = SperlichEditorTheme.BorderSubtle,
						}
					};
					if (sidebarColor.HasValue) titleRow.Add(SperlichEditorWidgets.CreateColorSidebar(sidebarColor.Value));
					titleRow.Add(new Label(title.ToUpperInvariant()) {
						style = {
							unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11,
							color = SperlichEditorTheme.TextSecondary,
							paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4,
						}
					});
					boxEl.Add(titleRow);
					var contentBody = new VisualElement {
						style = { paddingLeft = 3, paddingRight = 3, paddingTop = 0, paddingBottom = 4 }
					};
					boxEl.Add(contentBody);
					parent.Add(boxEl);
					body = contentBody;
				} else {
					boxEl.style.paddingLeft = 3;
					boxEl.style.paddingRight = 3;
					boxEl.style.paddingTop = 3;
					boxEl.style.paddingBottom = 4;
					if (sidebarColor.HasValue) {
						parent.Add(SperlichEditorWidgets.CreateSidebarGroup(sidebarColor.Value, boxEl));
					} else {
						parent.Add(boxEl);
					}
					body = boxEl;
				}
			}

			SperlichPrefabOverride.MarkGroupBody(body);
			if (box.ReadOnly) body.SetEnabled(false);
			return body;
		}

		/// <summary>Builds a <c>[SubBox]</c> as a fieldset: thin outline with the label sitting on the top border.
		/// Returns the content element the field rows should be routed into.</summary>
		private static VisualElement BuildSubBoxContainer(VisualElement parent, SerializedObject so, SubBoxAttribute sub, string firstFieldName) {
			if (sub.Flat) return BuildFlatSubBoxContainer(parent, so, sub, firstFieldName);
			Color? accent = null;
			if (!string.IsNullOrEmpty(sub.SidebarColor) && ColorUtility.TryParseHtmlString(sub.SidebarColor, out Color sc)) {
				accent = sc;
			} else if (sub.SidebarTint != TintColor.None) {
				accent = SperlichEditorWidgets.TintColorToColor(sub.SidebarTint);
			}

			bool hasTitle = !string.IsNullOrEmpty(sub.Label);
			const float legendHeight = 14f;

			var holder = new VisualElement {
				style = { marginTop = 4, marginBottom = 4, marginLeft = 2, marginRight = 2, backgroundColor = SperlichEditorTheme.BgStepBody }
			};

			var frame = new VisualElement {
				style = {
					marginTop = hasTitle ? legendHeight / 2f : 0,
					paddingTop = hasTitle ? legendHeight / 2f + 2 : 3, paddingBottom = 3, paddingLeft = 3, paddingRight = 3,
					borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
					borderTopColor = SperlichEditorTheme.BorderStrong, borderBottomColor = SperlichEditorTheme.BorderStrong,
					borderLeftColor = SperlichEditorTheme.BorderStrong, borderRightColor = SperlichEditorTheme.BorderStrong,
					borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
				}
			};
			var content = new VisualElement();
			SperlichPrefabOverride.MarkGroupBody(content);
			frame.Add(content);
			holder.Add(frame);

			if (hasTitle) {
				var legend = new VisualElement {
					style = {
						position = Position.Absolute, top = 0, left = 8, height = legendHeight,
						flexDirection = FlexDirection.Row, alignItems = Align.Center,
						backgroundColor = SperlichEditorTheme.BgStepBody, paddingLeft = 6, paddingRight = 6,
					}
				};

				var text = new Label(sub.Label.ToUpperInvariant()) {
					pickingMode = PickingMode.Ignore,
					style = { fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextMuted, marginLeft = 0, marginRight = 0, marginTop = 0, marginBottom = 0, paddingLeft = 0, paddingRight = 0 }
				};

				if (sub.Collapsable) {
					string typeName = so.targetObject != null ? so.targetObject.GetType().Name : "x";
					string prefKey = "Sperlich.Section/" + typeName + "/subbox/" + firstFieldName + "/" + sub.Label;
					bool expanded = EditorPrefs.GetBool(prefKey, sub.Expanded);

					var arrow = new Label(expanded ? "▾" : "▸") {
						pickingMode = PickingMode.Ignore,
						style = { fontSize = 10, color = accent ?? SperlichEditorTheme.TextMuted, marginRight = 4, marginLeft = 0, marginTop = 0, marginBottom = 0, paddingLeft = 0, paddingRight = 0 }
					};
					legend.pickingMode = PickingMode.Position;
					SperlichEditorWidgets.SetHoverCursor(legend, MouseCursor.Link);
					legend.Add(arrow);
					legend.Add(text);

					void Apply() {
						content.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
						frame.style.paddingBottom = expanded ? 3 : 0;
						arrow.text = expanded ? "▾" : "▸";
					}
					Apply();
					legend.RegisterCallback<ClickEvent>(_ => {
						expanded = !expanded;
						EditorPrefs.SetBool(prefKey, expanded);
						Apply();
					});
				} else {
					if (accent.HasValue) {
						legend.Add(new VisualElement {
							pickingMode = PickingMode.Ignore,
							style = {
								width = 6, height = 6, marginRight = 5, flexShrink = 0, backgroundColor = accent.Value,
								borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
							}
						});
					}
					legend.Add(text);
				}
				holder.Add(legend);
			}

			HideWhenEmpty(holder, content);
			parent.Add(holder);
			return content;
		}

		/// <summary><c>[SubBox(flat: true)]</c>: a flat header strip (chevron when collapsable, optional
		/// right-aligned summary) followed by indented rows, no outline.</summary>
		private static VisualElement BuildFlatSubBoxContainer(VisualElement parent, SerializedObject so, SubBoxAttribute sub, string firstFieldName) {
			var holder = new VisualElement { style = { marginTop = 3, marginBottom = 1 } };
			var header = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row, alignItems = Align.Center, height = 20,
					paddingLeft = 6, paddingRight = 6, backgroundColor = SperlichEditorTheme.BgStep,
					borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
				}
			};
			var arrow = new Label("▾") {
				pickingMode = PickingMode.Ignore,
				style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, width = 12, marginLeft = 0, paddingLeft = 0 }
			};
			var title = new Label(sub.Label ?? string.Empty) {
				pickingMode = PickingMode.Ignore,
				style = { fontSize = 11, color = SperlichEditorTheme.TextSecondary, marginLeft = 0, paddingLeft = 0 }
			};
			var summary = new Label {
				pickingMode = PickingMode.Ignore,
				style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginLeft = StyleKeyword.Auto, unityTextAlign = TextAnchor.MiddleRight }
			};
			if (sub.Collapsable) header.Add(arrow);
			header.Add(title);
			header.Add(summary);

			var content = new VisualElement { style = { paddingLeft = 10, paddingTop = 2 } };
			SperlichPrefabOverride.MarkGroupBody(content);
			holder.Add(header);
			holder.Add(content);

			if (sub.Collapsable) {
				string typeName = so.targetObject != null ? so.targetObject.GetType().Name : "x";
				string prefKey = "Sperlich.Section/" + typeName + "/subbox/" + firstFieldName + "/" + sub.Label;
				bool expanded = EditorPrefs.GetBool(prefKey, sub.Expanded);
				void Apply() {
					content.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
					arrow.text = expanded ? "▾" : "▸";
				}
				Apply();
				SperlichEditorWidgets.SetHoverCursor(header, MouseCursor.Link);
				header.RegisterCallback<ClickEvent>(_ => {
					expanded = !expanded;
					EditorPrefs.SetBool(prefKey, expanded);
					Apply();
				});
			}

			if (!string.IsNullOrEmpty(sub.Summary)) {
				void UpdateSummary() => summary.text = TryReadStringMember(so, sub.Summary, out string s) ? s : string.Empty;
				UpdateSummary();
				summary.schedule.Execute(UpdateSummary).Every(200);
			}

			HideWhenEmpty(holder, content);
			parent.Add(holder);
			return content;
		}

		/// <summary>Collapses <paramref name="holder"/> when every row in <paramref name="content"/> is hidden by
		/// <c>[ShowIf]</c>/<c>[HideIf]</c>, instead of leaving an empty frame.</summary>
		private static void HideWhenEmpty(VisualElement holder, VisualElement content) {
			holder.schedule.Execute(() => {
				bool anyVisible = false;
				foreach (VisualElement child in content.Children()) {
					if (child.style.display != DisplayStyle.None) { anyVisible = true; break; }
				}
				holder.style.display = anyVisible ? DisplayStyle.Flex : DisplayStyle.None;
			}).Every(200);
		}
	}

	/// <summary><c>[InspectorAccent]</c> and <c>[Box(sidebarMember: ...)]</c>: colours read from members at
	/// build time. Colours are baked into the controls, so the inspector rebuilds when one of them changes.</summary>
	public static partial class SperlichInspectorEngine {

		private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<VisualElement, object> AccentByRoot = new();

		/// <summary>Accent of the inspector <paramref name="element"/> lives in (incl. <c>[InspectorAccent]</c>).
		/// For custom <c>PropertyDrawer</c>s: they are bound after the inspector is built, so they must ask here
		/// (e.g. on <c>AttachToPanelEvent</c>) instead of reading <see cref="SperlichEditorTheme.ButtonAccent"/>.</summary>
		public static Color ResolveAccent(VisualElement element) {
			for (VisualElement e = element; e != null; e = e.parent) {
				if (AccentByRoot.TryGetValue(e, out object accent)) return (Color)accent;
			}
			return SperlichEditorTheme.ButtonAccent;
		}

		private static void BuildWithDynamicStyle(VisualElement root, SerializedObject so, SperlichInspectorPlan plan, Type targetType) {
			string accentMember = targetType.GetCustomAttribute<InspectorAccentAttribute>(true)?.Member;
			List<string> colorMembers = CollectDynamicColorMembers(targetType, accentMember);
			if (colorMembers.Count == 0) {
				BuildInto(root, so, plan);
				return;
			}

			string builtKey = null;
			void Rebuild() {
				builtKey = ColorKey(so.targetObject, colorMembers);
				root.Clear();
				SperlichEditorTheme.AccentOverride = accentMember != null && TryReadColorMember(so.targetObject, accentMember, out Color accent) ? accent : null;
				AccentByRoot.Remove(root);
				AccentByRoot.Add(root, SperlichEditorTheme.ButtonAccent);
				try {
					BuildInto(root, so, plan);
				} finally {
					SperlichEditorTheme.AccentOverride = null;
				}
			}

			Rebuild();
			root.schedule.Execute(() => {
				if (so.targetObject == null) return;
				if (ColorKey(so.targetObject, colorMembers) != builtKey) Rebuild();
			}).Every(200);
		}

		private static List<string> CollectDynamicColorMembers(Type type, string accentMember) {
			var members = new List<string>();
			if (!string.IsNullOrEmpty(accentMember)) members.Add(accentMember);
			for (Type t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(ScriptableObject); t = t.BaseType) {
				foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
					string sidebar = f.GetCustomAttribute<BoxAttribute>()?.SidebarMember;
					if (!string.IsNullOrEmpty(sidebar) && !members.Contains(sidebar)) members.Add(sidebar);
				}
			}
			return members;
		}

		private static string ColorKey(object target, List<string> members) {
			var sb = new StringBuilder();
			foreach (string m in members) {
				sb.Append(TryReadColorMember(target, m, out Color c) ? ColorUtility.ToHtmlStringRGBA(c) : "-").Append('|');
			}
			return sb.ToString();
		}

		/// <summary>Reads a member returning <c>Color</c>, <see cref="TintColor"/> or an HTML colour string.</summary>
		private static bool TryReadColorMember(object target, string name, out Color color) {
			color = default;
			if (!TryReadRawMember(target, name, out object raw)) return false;
			switch (raw) {
				case Color c: color = c; return true;
				case TintColor tint when tint != TintColor.None: color = SperlichEditorWidgets.TintColorToColor(tint); return true;
				case string html when ColorUtility.TryParseHtmlString(html, out Color parsed): color = parsed; return true;
			}
			return false;
		}
	}
}
