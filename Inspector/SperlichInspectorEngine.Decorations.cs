using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>Per-row post-processing for <see cref="SperlichInspectorEngine"/>: <c>[SReadOnly]</c>,
	/// <c>[TintColor]</c>, <c>[SuffixLabel]</c>, <c>[Unit]</c>, <c>[InlineButton]</c>,
	/// <c>[OnValueChanged]</c>, plus the <c>[EnumToggleButtons]</c> and <c>[ProgressBar]</c> controls that
	/// <see cref="BuildRow"/> hands off to.</summary>
	public static partial class SperlichInspectorEngine {

		/// <summary>Runs after <see cref="BuildRow"/>. May return a wrapper element (e.g. field row + a
		/// <c>[Unit]</c> conversion companion row), otherwise the row itself.</summary>
		private static VisualElement PostProcessRow(VisualElement row, SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta, SerializedObject so) {
			if (meta == null) return row;

			if (meta.ReadOnly) SperlichEditorWidgets.MarkReadOnly(row);

			if (meta.Tags != null && meta.Tags.Count > 0) {
				Label rowLabel = row.Q<Label>();
				if (rowLabel != null && rowLabel.parent != null) {
					int insertAt = rowLabel.parent.IndexOf(rowLabel) + 1;
					foreach ((string label, Color color) in meta.Tags) {
						rowLabel.parent.Insert(insertAt++, SperlichEditorWidgets.CreateTagPill(label, color));
					}
				}
			}

			Color? resolvedTint = null;
			if (!string.IsNullOrEmpty(meta.TintColorHtml) && ColorUtility.TryParseHtmlString(meta.TintColorHtml, out Color htmlTint)) {
				resolvedTint = htmlTint;
			} else if (meta.TintColorEnum != TintColor.None) {
				resolvedTint = SperlichEditorWidgets.TintColorToColor(meta.TintColorEnum);
			}
			if (resolvedTint.HasValue) {
				Color tint = resolvedTint.Value;
				Label lbl = row.Q<Label>();
				if (lbl != null) lbl.style.color = tint;
				if (meta.TintBackground) {
					// Colour the field's actual input surface, not the row's control wrapper (that only
					// showed a thin border because the input paints its own dark bg on top).
					VisualElement input = row.Q("unity-text-input")
						?? row.Q(className: "unity-base-field__input")
						?? row.Q(SperlichFieldColumn.ControlCellName)
						?? (row.childCount > 1 ? row[1] : null);
					if (input != null) input.style.backgroundColor = Color.Lerp(SperlichEditorTheme.BgDark, tint, 0.22f);
				}
			}

			bool hasExplicitSuffix = !string.IsNullOrEmpty(meta.SuffixText);
			if (hasExplicitSuffix) {
				if (meta.SuffixText.StartsWith("$")) {
					string member = meta.SuffixText.Substring(1);
					SperlichEditorWidgets.AttachSuffixLabel(row,
						() => TryReadStringMember(so, member, out string s) ? s : string.Empty, meta.SuffixOverlay, 250);
				} else {
					SperlichEditorWidgets.AttachSuffixLabel(row, () => meta.SuffixText, meta.SuffixOverlay);
				}
			}

			VisualElement result = row;

			if (meta.HasUnit) {
				string suffix = !string.IsNullOrEmpty(meta.UnitCustom)
					? meta.UnitCustom
					: (meta.Unit != UnitOfMeasure.None ? UnitConversion.Symbol(meta.Unit) : null);
				if (!hasExplicitSuffix && !string.IsNullOrEmpty(suffix)) {
					SperlichEditorWidgets.AttachSuffixLabel(row, () => suffix, false);
				}
				if (meta.Unit != UnitOfMeasure.None && meta.UnitDisplayAs != UnitOfMeasure.None) {
					result = WrapWithUnitCompanion(row, prop, meta);
				}
			}

			if (meta.InlineButtons != null && meta.InlineButtons.Count > 0) {
				var btns = new List<(string label, string icon, Action click)>();
				foreach ((string method, string label, string icon) in meta.InlineButtons) {
					string mName = method;
					btns.Add((label ?? ObjectNames.NicifyVariableName(method), icon, () => InvokeInstanceMethodOnTargets(so, mName)));
				}
				SperlichEditorWidgets.AttachInlineButtons(row, btns);
			}

			if (meta.OnValueChangedMethods != null && meta.OnValueChangedMethods.Count > 0 && meta.Field != null) {
				AttachOnValueChanged(row, prop, meta, so);
			}

			if (meta.Required && prop.propertyType == SerializedPropertyType.ObjectReference) {
				result = AttachRequiredCheck(result, prop, meta.RequiredMessage);
			}

			if (meta.EnableCondition != null) {
				ApplyEnableCondition(result, meta.EnableCondition, so);
			}

			return result;
		}

		/// <summary>Red left-edge flag + inline error message while the object reference is <c>null</c>
		/// (<c>[Required]</c>). The flag sits further out than the prefab-override bar (<c>left:-6</c>) so
		/// both can show at once without overlapping.</summary>
		private static VisualElement AttachRequiredCheck(VisualElement row, SerializedProperty prop, string customMessage) {
			var wrap = new VisualElement { style = { position = Position.Relative } };
			wrap.Add(row);

			var flag = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = { position = Position.Absolute, left = -9, top = 1, bottom = 1, width = 2, backgroundColor = SperlichEditorTheme.BadgeDangerBg, display = DisplayStyle.None }
			};
			wrap.Add(flag);

			string msg = customMessage ?? $"{prop.displayName} is required.";
			VisualElement box = SperlichEditorWidgets.CreateMessageBox(msg, SperlichEditorWidgets.MessageKind.Error);
			box.style.marginTop = 1;
			box.style.display = DisplayStyle.None;
			wrap.Add(box);

			void Refresh() {
				bool missing = prop.objectReferenceValue == null;
				flag.style.display = missing ? DisplayStyle.Flex : DisplayStyle.None;
				box.style.display = missing ? DisplayStyle.Flex : DisplayStyle.None;
			}
			Refresh();
			wrap.TrackPropertyValue(prop, _ => Refresh());
			wrap.schedule.Execute(Refresh).Every(400);
			return wrap;
		}

		// ── [EnableIf] / [DisableIf] ─────────────────────────────────────────────────

		/// <summary>Enables/disables <paramref name="element"/> from a condition on another member. Same
		/// condition grammar and event-vs-poll strategy as <see cref="ApplyVisibilityCondition"/>.</summary>
		private static void ApplyEnableCondition(VisualElement element, SperlichInspectorPlan.MemberMeta.VisCondition cond, SerializedObject so) {
			SerializedProperty driver = so.FindProperty(cond.Member);

			void Update() {
				bool match = EvaluateCondition(cond, driver, so);
				bool enabled = cond.Hide ? !match : match; // Hide=true -> [DisableIf]: disable on match
				element.SetEnabled(enabled);
			}

			Update();
			if (driver != null) element.TrackPropertyValue(driver, _ => Update());
			else element.schedule.Execute(Update).Every(200);
		}

		private static VisualElement WrapWithUnitCompanion(VisualElement row, SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta) {
			var wrap = new VisualElement();
			wrap.Add(row);

			bool sameDim = UnitConversion.SameDimension(meta.Unit, meta.UnitDisplayAs);
			var conv = new Label { style = { color = SperlichEditorTheme.TextMuted, fontSize = 11, flexGrow = 1 } };
			conv.SetEnabled(false);

			var col = new SperlichFieldColumn(150f);
			VisualElement compRow = col.Row(prop.displayName + " (" + UnitConversion.Symbol(meta.UnitDisplayAs) + ")", conv);
			compRow.style.opacity = 0.85f;

			void Refresh() {
				float v = prop.propertyType == SerializedPropertyType.Integer ? prop.intValue : prop.floatValue;
				conv.text = UnitConversion.TryConvert(v, meta.Unit, meta.UnitDisplayAs, out float r) ? r.ToString("0.###") : "—";
			}
			Refresh();
			compRow.TrackPropertyValue(prop, _ => Refresh());
			wrap.Add(compRow);

			if (!sameDim) {
				wrap.Add(SperlichEditorWidgets.CreateMessageBox(
					$"[Unit] dimension mismatch: {meta.Unit} ↔ {meta.UnitDisplayAs} — no conversion.",
					SperlichEditorWidgets.MessageKind.Warning));
			}
			return wrap;
		}

		private static void AttachOnValueChanged(VisualElement row, SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta, SerializedObject so) {
			FieldInfo fi = meta.Field;
			UnityEngine.Object[] targets = ResolveTargets(so);
			var prev = new object[targets.Length];
			for (int i = 0; i < targets.Length; i++) prev[i] = targets[i] != null ? SafeGet(fi, targets[i]) : null;

			row.TrackPropertyValue(prop, _ => {
				for (int i = 0; i < targets.Length; i++) {
					if (targets[i] == null) continue;
					object oldV = prev[i];
					object newV = SafeGet(fi, targets[i]);
					prev[i] = newV;
					foreach (string mName in meta.OnValueChangedMethods) {
						InvokeChangeCallback(targets[i], mName, fi.FieldType, oldV, newV);
					}
				}
				so.Update();
			});
		}

		private static object SafeGet(FieldInfo fi, object target) {
			try { return fi.GetValue(target); } catch { return null; }
		}

		private static void InvokeChangeCallback(UnityEngine.Object target, string name, Type valueType, object oldV, object newV) {
			for (Type t = target.GetType(); t != null && t != typeof(UnityEngine.Object); t = t.BaseType) {
				foreach (MethodInfo m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
					if (m.Name != name) continue;
					ParameterInfo[] ps = m.GetParameters();
					try {
						if (ps.Length == 0) { m.Invoke(target, null); return; }
						if (ps.Length == 1 && (ps[0].ParameterType == typeof(object) || ps[0].ParameterType.IsAssignableFrom(valueType))) {
							m.Invoke(target, new[] { newV });
							return;
						}
						if (ps.Length == 2) { m.Invoke(target, new[] { oldV, newV }); return; }
					} catch (Exception e) {
						Debug.LogException(e.InnerException ?? e);
						return;
					}
				}
			}
			Debug.LogWarning($"[SInspector] {target.GetType().Name}: [OnValueChanged] method '{name}' not found (expected (), (T) or (T, T)).");
		}

		// ── [ShowIf] / [HideIf] ────────────────────────────────────────────────────

		/// <summary>Toggles <paramref name="element"/>'s visibility from a condition on another member.
		/// Event-driven when the driver is a serialized field, polled (~200 ms) otherwise.</summary>
		private static void ApplyVisibilityCondition(VisualElement element, SperlichInspectorPlan.MemberMeta.VisCondition cond, SerializedObject so) {
			SerializedProperty driver = so.FindProperty(cond.Member);

			void Update() {
				bool match = EvaluateCondition(cond, driver, so);
				bool visible = cond.Hide ? !match : match;
				element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			}

			Update();
			if (driver != null) element.TrackPropertyValue(driver, _ => Update());
			else element.schedule.Execute(Update).Every(200);
		}

		private static bool EvaluateCondition(SperlichInspectorPlan.MemberMeta.VisCondition cond, SerializedProperty driver, SerializedObject so) {
			object current;
			if (driver != null) {
				current = ReadDriverValue(driver);
			} else if (!TryReadRawMember(so.targetObject, cond.Member, out current)) {
				return false; // unknown driver -> never matches
			}

			object[] wanted = cond.Values;
			if (wanted == null || wanted.Length == 0) return IsTruthy(current);

			foreach (object want in wanted) {
				bool wantIsFlags = want is Enum && want.GetType().GetCustomAttribute<FlagsAttribute>() != null;
				if (wantIsFlags) {
					if (TryToLong(current, out long cur) && TryToLong(want, out long bits)) {
						if (bits == 0 ? cur == 0 : (cur & bits) == bits) return true;
					}
				} else if (ValuesEqual(current, want)) {
					return true;
				}
			}
			return false;
		}

		private static object ReadDriverValue(SerializedProperty p) {
			switch (p.propertyType) {
				case SerializedPropertyType.Boolean: return p.boolValue;
				case SerializedPropertyType.Integer: return p.intValue;
				case SerializedPropertyType.Float: return p.floatValue;
				case SerializedPropertyType.String: return p.stringValue;
				case SerializedPropertyType.Enum: return p.intValue; // raw underlying value (matches Convert.ToInt64 on a boxed enum)
				case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
				default: return null;
			}
		}

		private static bool IsTruthy(object v) {
			switch (v) {
				case null: return false;
				case bool b: return b;
				case string s: return !string.IsNullOrEmpty(s);
				case UnityEngine.Object uo: return uo != null;
			}
			return TryToDouble(v, out double d) ? Mathf.Abs((float)d) > 0.0001f : true;
		}

		private static bool ValuesEqual(object a, object b) {
			if (a == null || b == null) return a == null && b == null;
			if (a is string || b is string) return string.Equals(Convert.ToString(a), Convert.ToString(b));
			if (TryToDouble(a, out double da) && TryToDouble(b, out double db)) return Math.Abs(da - db) < 0.0001;
			return a.Equals(b);
		}

		private static bool TryToDouble(object v, out double result) {
			try {
				if (v is Enum) { result = Convert.ToInt64(v); return true; }
				result = Convert.ToDouble(v);
				return true;
			} catch { result = 0; return false; }
		}

		private static bool TryToLong(object v, out long result) {
			try { result = Convert.ToInt64(v); return true; }
			catch { result = 0; return false; }
		}

		// ── controls handed off from BuildRow ───────────────────────────────────────

		private static VisualElement BuildEnumToggleButtons(SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta) {
			Type enumType = meta?.Field?.FieldType;
			Color accent = ResolveAccent(meta);
			if (enumType != null && enumType.IsEnum && enumType.GetCustomAttribute<FlagsAttribute>() != null) {
				string[] caps = FlagCaptions(enumType);
				if (caps.Length > 0) return SperlichEditorWidgets.CreateFlagButtons(prop, caps, null, accent);
				return SperlichEditorWidgets.CreateFlagsDropdown(prop, accent);
			}
			string[] labels = prop.enumDisplayNames ?? Array.Empty<string>();

			if (enumType != null && enumType.IsEnum) {
				string[] names = Enum.GetNames(enumType);
				Color[] perSegment = null;
				for (int i = 0; i < names.Length; i++) {
					var field = enumType.GetField(names[i]);
					if (field != null) {
						var ac = field.GetCustomAttribute<AccentColorAttribute>();
						var tc = field.GetCustomAttribute<TintColorAttribute>();
						if (ac != null || tc != null) {
							perSegment ??= new Color[names.Length];
							Color? c = SperlichEditorWidgets.ResolveColor(ac?.ColorHex ?? tc?.Color, ac?.Tint ?? tc?.Tint ?? TintColor.None);
							perSegment[i] = c ?? accent;
						}
					}
				}
				if (perSegment != null) {
					for (int i = 0; i < perSegment.Length; i++) {
						if (perSegment[i] == default) perSegment[i] = accent;
					}
					return SperlichEditorWidgets.CreateSegmentedControl(prop, labels, perSegment, buttonsPerRow: meta?.EnumToggleButtonsPerRow ?? 0);
				}
			}

			return SperlichEditorWidgets.CreateSegmentedControl(prop, labels, accent, buttonsPerRow: meta?.EnumToggleButtonsPerRow ?? 0);
		}

		/// <summary>For a <c>[Flags]</c> enum: an array where index <c>i</c> is the display name of the member
		/// whose value is <c>1 &lt;&lt; i</c> (empty string for a gap). Sized to the highest bit used.</summary>
		private static string[] FlagCaptions(Type enumType) {
			string[] names = Enum.GetNames(enumType);
			Array values = Enum.GetValues(enumType);
			var byBit = new Dictionary<int, string>();
			int maxBit = -1;
			for (int i = 0; i < names.Length; i++) {
				long v = Convert.ToInt64(values.GetValue(i));
				if (v <= 0 || (v & (v - 1)) != 0) continue; // skip 0 / combos / non-single-bit
				int bit = 0;
				long vv = v;
				while ((vv & 1) == 0) { vv >>= 1; bit++; }
				byBit[bit] = ObjectNames.NicifyVariableName(names[i]);
				if (bit > maxBit) maxBit = bit;
			}
			if (maxBit < 0) return Array.Empty<string>();
			var caps = new string[maxBit + 1];
			for (int b = 0; b <= maxBit; b++) caps[b] = byBit.TryGetValue(b, out string n) ? n : string.Empty;
			return caps;
		}

		private static VisualElement BuildProgressBarControl(SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta) {
			Color fill = SperlichEditorWidgets.ResolveColor(meta.PbColor, meta.PbTint) ?? Accent;

			float Read() => prop.propertyType == SerializedPropertyType.Integer ? prop.intValue : prop.floatValue;
			var (rootEl, setValue, setRange, _) = SperlichEditorWidgets.CreateProgressBar(
				Read(), meta.PbMin, meta.PbMax, fill, meta.PbHeight <= 0 ? 18 : meta.PbHeight, meta.PbSegmented, meta.PbShowValue, meta.PbPercent);

			rootEl.TrackPropertyValue(prop, p => setValue(p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue));

			if (!string.IsNullOrEmpty(meta.PbMinMember) || !string.IsNullOrEmpty(meta.PbMaxMember)) {
				SerializedObject so = prop.serializedObject;
				rootEl.schedule.Execute(() => {
					float lo = meta.PbMin, hi = meta.PbMax;
					if (!string.IsNullOrEmpty(meta.PbMinMember) && TryReadNumericMember(so, meta.PbMinMember, out float mlo)) lo = mlo;
					if (!string.IsNullOrEmpty(meta.PbMaxMember) && TryReadNumericMember(so, meta.PbMaxMember, out float mhi)) hi = mhi;
					setRange(lo, hi);
				}).Every(250);
			}
			return rootEl;
		}
	}
}
