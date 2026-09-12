using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary><c>[SButton]</c> / <c>[ButtonGroup]</c> / <c>[ShowProperty]</c> / <c>[ShowField]</c> emit for
	/// <see cref="SperlichInspectorEngine"/>: members that have no <c>SerializedProperty</c> and are woven
	/// into the field list by declaration order or pinned next to a named field.</summary>
	public static partial class SperlichInspectorEngine {

		private sealed class PendingEmit {
			public int Token;
			public string Name;
			public string After;
			public string Before;
			public string RowGroup;
			public BoxAttribute Box;
			public Func<VisualElement> Build;
			public bool Done;
		}

		private const BindingFlags InstanceMembers =
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		private static List<PendingEmit> BuildPendingEmits(SperlichInspectorPlan plan, List<Member> members, SerializedObject so, Func<object[]> targetResolver = null) {
			var list = new List<PendingEmit>();
			if (plan == null) return list;

			foreach (SperlichInspectorPlan.MethodButtonMeta mb in plan.MethodButtons) {
				SperlichInspectorPlan.MethodButtonMeta captured = mb;
				list.Add(new PendingEmit {
					Token = captured.MetadataToken, Name = captured.Method.Name, After = captured.After, Before = captured.Before,
					Build = () => BuildMethodButton(captured, so, targetResolver),
				});
			}

			foreach (SperlichInspectorPlan.ButtonGroupMeta bg in plan.ButtonGroups) {
				SperlichInspectorPlan.ButtonGroupMeta captured = bg;
				list.Add(new PendingEmit {
					Token = captured.MetadataToken, Name = captured.Group,
					Build = () => BuildButtonGroupBar(captured, so, targetResolver),
				});
			}

			var serializedNames = new HashSet<string>();
			foreach (Member m in members) serializedNames.Add(m.Name);
			foreach (SperlichInspectorPlan.ShowMemberMeta sm in plan.ShowMembers) {
				if (sm.IsField && serializedNames.Contains(sm.Name)) continue; // serialized fields already show
				SperlichInspectorPlan.ShowMemberMeta captured = sm;
				list.Add(new PendingEmit {
					Token = captured.MetadataToken,
					Name = captured.Name,
					RowGroup = captured.RowGroup,
					Box = captured.Box,
					Build = () => !string.IsNullOrEmpty(captured.RowGroup)
						? BuildLiveDisplayCell(captured, so, targetResolver)
						: BuildLiveDisplayRow(captured, so, targetResolver),
				});
			}

			list.Sort((a, b) => (a.Token & 0xFFFFFF).CompareTo(b.Token & 0xFFFFFF));
			return list;
		}

		/// <summary>Emits every not-yet-emitted pending item pinned <paramref name="before"/> or
		/// <paramref name="after"/> the named field into <paramref name="target"/>.</summary>
		private static void FlushPinned(List<PendingEmit> pending, VisualElement target, SerializedObject so, string before = null, string after = null) {
			foreach (PendingEmit pe in pending) {
				if (pe.Done) continue;
				bool hit = (before != null && pe.Before == before) || (after != null && pe.After == after);
				if (!hit) continue;
				VisualElement el = pe.Build();
				if (el != null) target.Add(el);
				pe.Done = true;
			}
		}

		/// <summary>Emits the leftover pending items (declaration order, unpinned or unresolved pins) at the
		/// end of the inspector body.</summary>
		private static void FlushUnpinned(List<PendingEmit> pending, VisualElement container, SerializedObject so) {
			VisualElement currentTarget = container;
			int i = 0;
			while (i < pending.Count) {
				PendingEmit pe = pending[i];
				if (pe.Done) { i++; continue; }

				if (pe.Box != null) {
					currentTarget = BuildBoxContainer(container, so, pe.Box, pe.Name ?? "box");
				}

				if (!string.IsNullOrEmpty(pe.RowGroup)) {
					int end = i;
					while (end < pending.Count && !pending[end].Done && pending[end].RowGroup == pe.RowGroup) {
						end++;
					}
					var rowWrap = new VisualElement {
						style = {
							flexDirection = FlexDirection.Row,
							flexWrap = Wrap.Wrap,
							marginTop = 2,
							marginBottom = 2,
							marginLeft = 2,
							marginRight = 2
						}
					};
					for (int k = i; k < end; k++) {
						VisualElement el = pending[k].Build();
						if (el != null) rowWrap.Add(el);
						pending[k].Done = true;
					}
					currentTarget.Add(rowWrap);
					i = end;
					continue;
				}

				VisualElement single = pe.Build();
				if (single != null) currentTarget.Add(single);
				pe.Done = true;
				i++;
			}
		}

		private static VisualElement BuildMethodButton(SperlichInspectorPlan.MethodButtonMeta mb, SerializedObject so, Func<object[]> targetResolver = null) {
			MethodInfo m = mb.Method;
			return SperlichEditorWidgets.MakeButton(mb.Label, mb.Size, mb.Height, mb.Anchor,
				() => InvokeOnTargets(so, m, targetResolver), isAccent: false, icon: mb.Icon);
		}

		private static VisualElement BuildButtonGroupBar(SperlichInspectorPlan.ButtonGroupMeta bg, SerializedObject so, Func<object[]> targetResolver = null) {
			var items = new List<(string label, string icon, Action click)>();
			foreach ((MethodInfo method, string label, string icon) in bg.Items) {
				MethodInfo m = method;
				items.Add((label, icon, () => InvokeOnTargets(so, m, targetResolver)));
			}
			return SperlichEditorWidgets.CreateMethodButtonGroup(items, Accent);
		}

		private static VisualElement BuildLiveDisplayCell(SperlichInspectorPlan.ShowMemberMeta sm, SerializedObject so, Func<object[]> targetResolver) {
			var cell = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.SpaceBetween,
					alignItems = Align.Center,
					flexGrow = 1,
					flexShrink = 1,
					flexBasis = 0,
					minWidth = 58,
					marginRight = 6,
					marginLeft = 2,
					height = 20
				}
			};

			var caption = new Label(sm.Label) {
				style = {
					fontSize = 11,
					color = SperlichEditorTheme.TextSecondary,
					marginRight = 6
				}
			};
			cell.Add(caption);

			VisualElement valueEl = CreateLiveValueElement(sm, so, targetResolver);
			cell.Add(valueEl);
			return cell;
		}

		private static VisualElement BuildLiveDisplayRow(SperlichInspectorPlan.ShowMemberMeta sm, SerializedObject so, Func<object[]> targetResolver) {
			var col = new SperlichFieldColumn(150f);
			VisualElement valueEl = CreateLiveValueElement(sm, so, targetResolver);
			return col.Row(sm.Label, valueEl);
		}

		private static VisualElement CreateLiveValueElement(SperlichInspectorPlan.ShowMemberMeta sm, SerializedObject so, Func<object[]> targetResolver) {
			Color? customColor = SperlichEditorWidgets.ResolveColor(sm.ColorHex, sm.Tint);

			if (sm.Badge) {
				var badge = new Label("—") {
					style = {
						fontSize = 9,
						unityFontStyleAndWeight = FontStyle.Bold,
						paddingLeft = 6,
						paddingRight = 6,
						paddingTop = 1,
						paddingBottom = 1,
						borderTopLeftRadius = 3,
						borderTopRightRadius = 3,
						borderBottomLeftRadius = 3,
						borderBottomRightRadius = 3,
						backgroundColor = new Color(1f, 1f, 1f, 0.08f),
						color = SperlichEditorTheme.TextMuted,
						alignSelf = Align.Center
					}
				};

				void UpdateBadge() {
					object raw = ReadRawShowValue(sm, so, targetResolver);
					if (raw is bool b) {
						if (b) {
							badge.text = "TRUE";
							badge.style.color = new Color(0.45f, 0.85f, 0.25f);
							badge.style.backgroundColor = new Color(0.45f, 0.85f, 0.25f, 0.18f);
						} else {
							badge.text = "FALSE";
							badge.style.color = new Color(0.95f, 0.32f, 0.28f);
							badge.style.backgroundColor = new Color(0.95f, 0.32f, 0.28f, 0.18f);
						}
					} else if (raw != null) {
						badge.text = raw.ToString().ToUpperInvariant();
						if (customColor.HasValue) {
							badge.style.color = customColor.Value;
							badge.style.backgroundColor = new Color(customColor.Value.r, customColor.Value.g, customColor.Value.b, 0.18f);
						}
					} else {
						badge.text = "—";
					}
				}

				badge.schedule.Execute(UpdateBadge).Every(sm.PollMs);
				UpdateBadge();
				return badge;
			}

			var valLabel = new Label("—") {
				style = {
					color = customColor ?? SperlichEditorTheme.TextSecondary,
					fontSize = 11,
					whiteSpace = WhiteSpace.NoWrap,
				}
			};
			valLabel.SetEnabled(false);

			void UpdateLabel() {
				string v = ReadFormattedShowValue(sm, so, targetResolver);
				if (v != valLabel.text) valLabel.text = v;
			}

			valLabel.schedule.Execute(UpdateLabel).Every(sm.PollMs);
			UpdateLabel();
			return valLabel;
		}

		private static object ReadRawShowValue(SperlichInspectorPlan.ShowMemberMeta sm, SerializedObject so, Func<object[]> targetResolver) {
			object[] targets = targetResolver != null ? targetResolver() : ResolveTargets(so);
			if (targets == null || targets.Length == 0) return null;
			object first = null;
			foreach (object t in targets) {
				if (t == null) continue;
				try {
					object raw = sm.IsField ? ((FieldInfo)sm.Member).GetValue(t) : ((PropertyInfo)sm.Member).GetValue(t);
					if (first == null) first = raw;
					else if (!Equals(first, raw)) return null;
				} catch {
					return null;
				}
			}
			return first;
		}

		private static string ReadFormattedShowValue(SperlichInspectorPlan.ShowMemberMeta sm, SerializedObject so, Func<object[]> targetResolver) {
			object raw = ReadRawShowValue(sm, so, targetResolver);
			if (raw == null) return "—";

			string s;
			if (raw is float f) {
				if (sm.Suffix == "ms") s = f.ToString("0.000");
				else if (sm.Suffix == "m") s = f.ToString("0.00");
				else s = f.ToString("0.###");
			} else if (raw is double d) {
				if (sm.Suffix == "ms") s = d.ToString("0.000");
				else if (sm.Suffix == "m") s = d.ToString("0.00");
				else s = d.ToString("0.###");
			} else if (raw is Vector2 v2) {
				s = $"{v2.x:0.#}|{v2.y:0.#}";
			} else if (raw is Vector3 v3) {
				s = $"{v3.x:0.#}|{v3.z:0.#}";
			} else {
				s = FormatDisplayValue(raw);
			}

			if (!string.IsNullOrEmpty(sm.Suffix)) {
				s += " " + sm.Suffix;
			}
			return s;
		}

		private static string FormatDisplayValue(object v) {
			switch (v) {
				case null: return "null";
				case UnityEngine.Object uo: return uo != null ? uo.name : "None";
				case float f: return f.ToString("0.###");
				case double d: return d.ToString("0.###");
				case Vector2 v2: return v2.ToString("0.##");
				case Vector3 v3: return v3.ToString("0.##");
				case Vector4 v4: return v4.ToString("0.##");
				default: return v.ToString();
			}
		}

		// ── invocation helpers ──────────────────────────────────────────────────────

		private static UnityEngine.Object[] ResolveTargets(SerializedObject so) {
			if (so == null) return Array.Empty<UnityEngine.Object>();
			UnityEngine.Object[] many = so.targetObjects;
			if (many != null && many.Length > 0) return many;
			return so.targetObject != null ? new[] { so.targetObject } : Array.Empty<UnityEngine.Object>();
		}

		/// <summary>Invokes a resolved <see cref="MethodInfo"/> on every selected target, wrapped in an Undo
		/// step, then refreshes the serialized object.</summary>
		private static void InvokeOnTargets(SerializedObject so, MethodInfo method, Func<object[]> targetResolver = null) {
			if (method == null) return;
			UnityEngine.Object[] undoRoots = ResolveTargets(so);
			object[] targets = targetResolver != null ? targetResolver() : undoRoots;
			if (targets == null || targets.Length == 0) return;
			if (undoRoots != null && undoRoots.Length > 0) Undo.RecordObjects(undoRoots, method.Name);
			foreach (object t in targets) {
				if (t == null) continue;
				try { method.Invoke(t, null); }
				catch (Exception e) { Debug.LogException(e.InnerException ?? e); }
			}
			so?.Update();
		}

		/// <summary>Resolves a parameterless method by name and invokes it on every target (used by
		/// <c>[InlineButton]</c>). Uses a manual scan — the <c>GetMethod(name, flags, binder, types, mods)</c>
		/// overload is unreliable on Mono.</summary>
		private static void InvokeInstanceMethodOnTargets(SerializedObject so, string methodName) {
			UnityEngine.Object[] targets = ResolveTargets(so);
			if (targets.Length == 0) return;
			Undo.RecordObjects(targets, methodName);
			foreach (UnityEngine.Object t in targets) {
				if (t == null) continue;
				MethodInfo m = FindParameterlessMethod(t.GetType(), methodName);
				if (m == null) {
					Debug.LogWarning($"[SInspector] {t.GetType().Name}: [InlineButton] method '{methodName}' not found or takes parameters.");
					continue;
				}
				try { m.Invoke(t, null); }
				catch (Exception e) { Debug.LogException(e.InnerException ?? e); }
			}
			so.Update();
		}

		private static MethodInfo FindParameterlessMethod(Type type, string name) {
			foreach (MethodInfo m in type.GetMethods(InstanceMembers)) {
				if (m.Name == name && m.GetParameters().Length == 0) return m;
			}
			return null;
		}

		private static bool TryReadRawMember(object target, string name, out object raw) {
			raw = null;
			if (target == null) return false;
			Type t = target.GetType();

			FieldInfo f = t.GetField(name, InstanceMembers);
			if (f != null) {
				try { raw = f.GetValue(target); return true; } catch { return false; }
			}
			PropertyInfo p = t.GetProperty(name, InstanceMembers);
			if (p != null && p.GetMethod != null && p.GetIndexParameters().Length == 0) {
				try { raw = p.GetValue(target); return true; } catch { return false; }
			}
			MethodInfo m = FindParameterlessMethod(t, name);
			if (m != null && m.ReturnType != typeof(void)) {
				try { raw = m.Invoke(target, null); return true; } catch { return false; }
			}
			return false;
		}

		private static bool TryReadNumericMember(SerializedObject so, string name, out float value) {
			value = 0f;
			UnityEngine.Object target = so != null ? so.targetObject : null;
			if (!TryReadRawMember(target, name, out object raw) || raw == null) return false;
			try { value = Convert.ToSingle(raw); return true; } catch { return false; }
		}

		private static bool TryReadStringMember(SerializedObject so, string name, out string value) {
			value = null;
			UnityEngine.Object target = so != null ? so.targetObject : null;
			if (!TryReadRawMember(target, name, out object raw)) return false;
			value = raw?.ToString() ?? string.Empty;
			return true;
		}
	}
}
