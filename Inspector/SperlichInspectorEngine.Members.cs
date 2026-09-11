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
			public string After;
			public string Before;
			public Func<VisualElement> Build;
			public bool Done;
		}

		private const BindingFlags InstanceMembers =
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		private static List<PendingEmit> BuildPendingEmits(SperlichInspectorPlan plan, List<Member> members, SerializedObject so) {
			var list = new List<PendingEmit>();
			if (plan == null) return list;

			foreach (SperlichInspectorPlan.MethodButtonMeta mb in plan.MethodButtons) {
				SperlichInspectorPlan.MethodButtonMeta captured = mb;
				list.Add(new PendingEmit {
					Token = captured.MetadataToken, After = captured.After, Before = captured.Before,
					Build = () => BuildMethodButton(captured, so),
				});
			}

			foreach (SperlichInspectorPlan.ButtonGroupMeta bg in plan.ButtonGroups) {
				SperlichInspectorPlan.ButtonGroupMeta captured = bg;
				list.Add(new PendingEmit {
					Token = captured.MetadataToken,
					Build = () => BuildButtonGroupBar(captured, so),
				});
			}

			var serializedNames = new HashSet<string>();
			foreach (Member m in members) serializedNames.Add(m.Name);
			foreach (SperlichInspectorPlan.ShowMemberMeta sm in plan.ShowMembers) {
				if (sm.IsField && serializedNames.Contains(sm.Name)) continue; // serialized fields already show
				SperlichInspectorPlan.ShowMemberMeta captured = sm;
				list.Add(new PendingEmit {
					Token = captured.MetadataToken,
					Build = () => BuildLiveDisplayRow(captured, so),
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
			foreach (PendingEmit pe in pending) {
				if (pe.Done) continue;
				VisualElement el = pe.Build();
				if (el != null) container.Add(el);
				pe.Done = true;
			}
		}

		private static VisualElement BuildMethodButton(SperlichInspectorPlan.MethodButtonMeta mb, SerializedObject so) {
			MethodInfo m = mb.Method;
			return SperlichEditorWidgets.MakeButton(mb.Label, mb.Size, mb.Height, mb.Anchor,
				() => InvokeOnTargets(so, m), isAccent: false, icon: mb.Icon);
		}

		private static VisualElement BuildButtonGroupBar(SperlichInspectorPlan.ButtonGroupMeta bg, SerializedObject so) {
			var items = new List<(string label, string icon, Action click)>();
			foreach ((MethodInfo method, string label, string icon) in bg.Items) {
				MethodInfo m = method;
				items.Add((label, icon, () => InvokeOnTargets(so, m)));
			}
			return SperlichEditorWidgets.CreateMethodButtonGroup(items, Accent);
		}

		private static VisualElement BuildLiveDisplayRow(SperlichInspectorPlan.ShowMemberMeta sm, SerializedObject so) {
			var col = new SperlichFieldColumn(150f);
			var value = new Label(ReadShowValue(sm, so)) {
				style = { color = SperlichEditorTheme.TextSecondary, fontSize = 12, whiteSpace = WhiteSpace.Normal, flexGrow = 1 },
			};
			value.SetEnabled(false);
			VisualElement row = col.Row(sm.Label, value);
			row.schedule.Execute(() => {
				string v = ReadShowValue(sm, so);
				if (v != value.text) value.text = v;
			}).Every(sm.PollMs);
			return row;
		}

		private static string ReadShowValue(SperlichInspectorPlan.ShowMemberMeta sm, SerializedObject so) {
			UnityEngine.Object[] targets = ResolveTargets(so);
			string first = null;
			foreach (UnityEngine.Object t in targets) {
				if (t == null) continue;
				object raw;
				try {
					raw = sm.IsField ? ((FieldInfo)sm.Member).GetValue(t) : ((PropertyInfo)sm.Member).GetValue(t);
				} catch (Exception e) {
					return "⚠ " + (e.InnerException ?? e).GetType().Name;
				}
				string s = FormatDisplayValue(raw);
				if (first == null) first = s;
				else if (first != s) return "—";
			}
			return first ?? "—";
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
		private static void InvokeOnTargets(SerializedObject so, MethodInfo method) {
			if (method == null) return;
			UnityEngine.Object[] targets = ResolveTargets(so);
			if (targets.Length == 0) return;
			Undo.RecordObjects(targets, method.Name);
			foreach (UnityEngine.Object t in targets) {
				if (t == null) continue;
				try { method.Invoke(t, null); }
				catch (Exception e) { Debug.LogException(e.InnerException ?? e); }
			}
			so.Update();
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
