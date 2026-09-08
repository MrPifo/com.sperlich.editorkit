using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Turns a <see cref="SerializedObject"/> into a Sperlich-styled UI Toolkit tree: walks the visible
	/// serialized properties once, maps each to a Sperlich control (or a themed <c>PropertyField</c> for the
	/// types that already have full inspector parity), and applies <c>[Header]</c> / <c>[Space]</c> /
	/// <c>[Tooltip]</c> decorators. No per-frame work — everything is bound once.
	///
	/// <para>Leaf-field mapping is delegated to <see cref="SperlichFieldColumn.Property"/> (which already
	/// routes bool → pill, enum → flat dropdown, number → drag field, range → slider, and falls back to a
	/// <c>PropertyField</c> otherwise); this engine only adds the cases that helper does not cover
	/// (flags enums, multiline text, nested foldouts, collections) plus the per-row prefab-override bar.</para>
	/// </summary>
	public static class SperlichInspectorEngine {

		public const string RootClass = "sperlich-inspector";
		private static readonly Color Accent = SperlichEditorTheme.ButtonAccent;

		private static StyleSheet cachedSheet;

		/// <summary>Horizontal inset of the field rows; section-header strips cancel it to bleed full-width.</summary>
		private const int PadX = 10;

		/// <summary>Builds the whole inspector body for <paramref name="serializedObject"/> of runtime type
		/// <paramref name="targetType"/>. The caller wraps this (outer margins, scroll preservation).</summary>
		public static VisualElement Build(SerializedObject serializedObject, Type targetType) {
			var root = new VisualElement {
				style = {
					backgroundColor = SperlichEditorTheme.BgPanel,
					paddingLeft = PadX, paddingRight = 8, paddingTop = 4, paddingBottom = 10,
				}
			};
			root.AddToClassList(RootClass);
			StyleSheet sheet = ResolveStyleSheet();
			if (sheet != null) root.styleSheets.Add(sheet);

			SperlichInspectorPlan plan = SperlichInspectorPlan.For(targetType);
			BuildInto(root, serializedObject, plan);
			return root;
		}

		/// <summary>Max cells on one visible line of an <c>[SRow]</c> group (wraps beyond this).</summary>
		private const int MaxRowCells = 6;

		private readonly struct Member {
			public readonly SerializedProperty Prop;
			public readonly SperlichInspectorPlan.MemberMeta Meta;
			public Member(SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta) { Prop = prop; Meta = meta; }
		}

		private static void BuildInto(VisualElement container, SerializedObject so, SperlichInspectorPlan plan) {
			var col = new SperlichFieldColumn(150f);

			var members = new List<Member>();
			SerializedProperty it = so.GetIterator();
			bool enterChildren = true;
			while (it.NextVisible(enterChildren)) {
				enterChildren = false;
				if (it.name == "m_Script") { container.Add(BuildScriptRow(col, it.Copy())); continue; }
				members.Add(new Member(it.Copy(), plan?.Get(it.name)));
			}

			for (int i = 0; i < members.Count; i++) {
				Member m = members[i];
				string groupKey = m.Meta?.RowGroup;

				// [SRow] on an inline-able scalar -> collect the adjacent same-key run and lay it out horizontally.
				if (groupKey != null && CanInline(m.Prop)) {
					int end = i;
					while (end < members.Count
					       && members[end].Meta?.RowGroup == groupKey
					       && CanInline(members[end].Prop)) {
						end++;
					}
					EmitDecorators(container, m.Meta);
					container.Add(BuildHorizontalGroup(members, i, end));
					i = end - 1;
					continue;
				}

				EmitDecorators(container, m.Meta);
				VisualElement row = BuildRow(col, m.Prop, m.Meta);
				if (m.Meta != null && !string.IsNullOrEmpty(m.Meta.Tooltip)) row.tooltip = m.Meta.Tooltip;
				container.Add(row);
			}
		}

		private static void EmitDecorators(VisualElement container, SperlichInspectorPlan.MemberMeta meta) {
			if (meta == null) return;
			if (meta.SpaceBefore > 0f) container.Add(new VisualElement { style = { height = meta.SpaceBefore, flexShrink = 0 } });
			if (!string.IsNullOrEmpty(meta.Header)) container.Add(BuildHeader(meta.Header));
		}

		// ── horizontal [SRow] groups ─────────────────────────────────────────────────

		/// <summary>Scalar fields that make sense side by side. Anything wider stays on its own row.</summary>
		private static bool CanInline(SerializedProperty prop) {
			switch (prop.propertyType) {
				case SerializedPropertyType.Boolean:
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.Float:
				case SerializedPropertyType.Enum:
				case SerializedPropertyType.Color:
					return true;
				case SerializedPropertyType.String:
					return true;
				default:
					return false;
			}
		}

		private static VisualElement BuildHorizontalGroup(List<Member> members, int start, int end) {
			var wrap = new VisualElement { style = { marginTop = 2, marginBottom = 2, marginLeft = 3 } };

			for (int chunkStart = start; chunkStart < end; chunkStart += MaxRowCells) {
				int chunkEnd = Mathf.Min(chunkStart + MaxRowCells, end);
				var line = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
				for (int k = chunkStart; k < chunkEnd; k++) {
					line.Add(BuildInlineCell(members[k]));
				}
				wrap.Add(line);
			}
			return wrap;
		}

		private static VisualElement BuildInlineCell(Member m) {
			var cell = new VisualElement {
				style = { flexGrow = 1, flexShrink = 1, flexBasis = 0, minWidth = 58, marginRight = 6, marginBottom = 2 }
			};

			var caption = new Label(m.Prop.displayName) {
				tooltip = m.Meta?.Tooltip,
				style = {
					fontSize = 10, color = SperlichEditorTheme.TextMuted, marginBottom = 1,
					overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap,
				}
			};
			cell.Add(caption);
			cell.Add(BuildInlineControl(m));
			return cell;
		}

		private static VisualElement BuildInlineControl(Member m) {
			SerializedProperty prop = m.Prop;
			switch (prop.propertyType) {
				case SerializedPropertyType.Boolean:
					return new SperlichToggleField(prop);
				case SerializedPropertyType.Enum: {
					VisualElement dd = IsFlags(m.Meta)
						? SperlichEditorWidgets.CreateFlagsDropdown(prop, Accent)
						: SperlichEditorWidgets.CreateEnumDropdown(prop, Accent, null, m.Meta?.Field?.FieldType);
					dd.style.flexGrow = 1;
					return dd;
				}
				case SerializedPropertyType.Color: {
					var cf = new UnityEditor.UIElements.ColorField { style = { flexGrow = 1 }, showAlpha = true };
					cf.BindProperty(prop);
					SperlichFieldColumn.HideInternalLabel(cf);
					return cf;
				}
				case SerializedPropertyType.String: {
					var tf = new TextField { style = { flexGrow = 1 } };
					tf.BindProperty(prop);
					SperlichFieldColumn.HideInternalLabel(tf);
					return tf;
				}
				default:
					// int / float
					return SperlichEditorWidgets.CreateDragNumberField(prop);
			}
		}

		private static VisualElement BuildRow(SperlichFieldColumn col, SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta) {
			Type declaredType = meta?.Field?.FieldType;

			// SDictionary<,> / SHashSet<> -> Sperlich key→value / value list (before the generic-foldout branch,
			// which would otherwise expand the two backing _keys/_values lists raw).
			if (declaredType != null && declaredType.IsGenericType) {
				Type gd = declaredType.GetGenericTypeDefinition();
				if (gd == typeof(SDictionary<,>)) return BuildDictionaryRow(prop);
				if (gd == typeof(SHashSet<>)) return BuildArrayBackedList(prop.FindPropertyRelative("_items"), prop, prop.displayName, warnDuplicates: true, elemType: ElemArg(declaredType, 0), addText: "+ Wert", emptyText: "Leeres Set");
			}

			// Arrays / Lists (element type without its own drawer) -> Sperlich compact-row collection editor.
			bool isCollection = prop.isArray && prop.propertyType != SerializedPropertyType.String;
			bool elementHasDrawer = meta != null && meta.ElementTypeHasDrawer;
			if (isCollection && !elementHasDrawer) {
				return BuildArrayBackedList(prop, prop, prop.displayName, warnDuplicates: false,
					elemType: declaredType != null ? SperlichInspectorPlan.ElementType(declaredType) : null);
			}

			// Everything else that keeps a plain PropertyField: [SerializeReference], drawer types (SEvent),
			// and collections whose element type has its own drawer.
			bool hasOwnDrawer = elementHasDrawer || PropertyDrawerRegistry.HasCustomDrawerForName(prop.type);
			if (isCollection || prop.propertyType == SerializedPropertyType.ManagedReference || hasOwnDrawer) {
				return FullWidthPropertyField(prop);
			}

			// Nested serializable struct/class without a drawer -> foldout that recurses with the same engine.
			if (prop.propertyType == SerializedPropertyType.Generic && prop.hasVisibleChildren) {
				return BuildNestedFoldout(prop);
			}

			// Bool -> pill toggle (mixed-value aware). Not a bound BaseField, so it needs the manual bar.
			if (prop.propertyType == SerializedPropertyType.Boolean) {
				var toggle = new SperlichToggleField(prop);
				VisualElement boolRow = col.Row(prop.displayName, toggle);
				toggle.style.flexGrow = 0; // col.Row stretches controls; keep the toggle sized to its content
				return OverrideRow(boolRow, prop);
			}

			// Enum -> flat dropdown / flags multi-select. Also not bound BaseFields -> manual bar.
			if (prop.propertyType == SerializedPropertyType.Enum) {
				VisualElement dd = IsFlags(meta)
					? SperlichEditorWidgets.CreateFlagsDropdown(prop, Accent)
					: SperlichEditorWidgets.CreateEnumDropdown(prop, Accent, null, meta?.Field?.FieldType);
				return OverrideRow(col.Row(prop.displayName, dd), prop);
			}

			// LayerMask -> Sperlich multi-select dropdown (Unity's own field is the odd one out otherwise).
			if (prop.propertyType == SerializedPropertyType.LayerMask) {
				return OverrideRow(col.Row(prop.displayName, SperlichEditorWidgets.CreateLayerMaskDropdown(prop, Accent)), prop);
			}

			// Vector2 / Vector2Int with [SMinMax] -> dual-handle "from–to" range slider.
			if (meta != null && meta.HasMinMax
			    && (prop.propertyType == SerializedPropertyType.Vector2 || prop.propertyType == SerializedPropertyType.Vector2Int)) {
				bool intVec = prop.propertyType == SerializedPropertyType.Vector2Int;
				VisualElement mm = SperlichEditorWidgets.CreateMinMaxSlider(prop, meta.MinMaxLow, meta.MinMaxHigh, intVec, Accent);
				return OverrideRow(col.Row(prop.displayName, mm), prop);
			}

			// Vector2/3/4 (+ Int) -> one inline row of compact drag fields. Unity's PropertyField collapses
			// Vector4 into a responsive foldout in the narrow inspector; this keeps all components on one line.
			if (TryVectorComponents(prop, out string[] caps, out SerializedProperty[] parts)) {
				var cluster = SperlichEditorWidgets.CreateFieldCluster(46,
					BuildVectorCells(caps, parts));
				return col.Row(prop.displayName, cluster);
			}

			// Object reference -> bound ObjectField + a grey "×" clear button on the right.
			if (prop.propertyType == SerializedPropertyType.ObjectReference) {
				Type objType = meta?.Field?.FieldType;
				bool sceneOk = prop.serializedObject.targetObject == null
					|| !EditorUtility.IsPersistent(prop.serializedObject.targetObject);
				return col.Row(prop.displayName, SperlichEditorWidgets.CreateObjectField(prop, objType, sceneOk));
			}

			// Multiline string -> bound TextField (self-indicates overrides via the binding system).
			if (prop.propertyType == SerializedPropertyType.String && meta != null && meta.Multiline) {
				var tf = new TextField { multiline = true, style = { flexGrow = 1, whiteSpace = WhiteSpace.Normal } };
				tf.BindProperty(prop);
				SperlichFieldColumn.HideInternalLabel(tf);
				VisualElement inner = tf.Q("unity-text-input");
				if (inner != null) inner.style.minHeight = 18 * Mathf.Max(2, meta.MultilineRows);
				return col.Row(prop.displayName, tf);
			}

			// Everything else: number / range / string / color / gradient / vector / object / curve / …
			// SperlichFieldColumn.Property returns a bound BaseField or a PropertyField — both already draw
			// the prefab-override bar + Apply/Revert menu themselves, so no manual bar here (would double up).
			return col.Property(prop);
		}

		private static VisualElement OverrideRow(VisualElement row, SerializedProperty prop) {
			SperlichPrefabOverride.Attach(row, row.Q<Label>(), prop);
			return row;
		}

		/// <summary>Resolves the per-axis sub-properties of a Vector2/3/4 (or the Int variants). Returns false
		/// for anything else, so the caller falls back to a normal field.</summary>
		private static bool TryVectorComponents(SerializedProperty prop, out string[] captions, out SerializedProperty[] parts) {
			// Vector2/3/4 serialize their axes as "x"/"y"/…, the Int variants as "m_X"/"m_Y"/… — try both so
			// every vector kind ends up as the same compact drag-field row (no inconsistency between them).
			string[][] candidates;
			switch (prop.propertyType) {
				case SerializedPropertyType.Vector2:
					captions = new[] { "X", "Y" };
					candidates = new[] { new[] { "x", "y" }, new[] { "m_X", "m_Y" } };
					break;
				case SerializedPropertyType.Vector2Int:
					captions = new[] { "X", "Y" };
					candidates = new[] { new[] { "m_X", "m_Y" }, new[] { "x", "y" } };
					break;
				case SerializedPropertyType.Vector3:
					captions = new[] { "X", "Y", "Z" };
					candidates = new[] { new[] { "x", "y", "z" }, new[] { "m_X", "m_Y", "m_Z" } };
					break;
				case SerializedPropertyType.Vector3Int:
					captions = new[] { "X", "Y", "Z" };
					candidates = new[] { new[] { "m_X", "m_Y", "m_Z" }, new[] { "x", "y", "z" } };
					break;
				case SerializedPropertyType.Vector4:
					captions = new[] { "X", "Y", "Z", "W" };
					candidates = new[] { new[] { "x", "y", "z", "w" } };
					break;
				default:
					captions = null;
					parts = null;
					return false;
			}

			foreach (string[] names in candidates) {
				var found = new SerializedProperty[names.Length];
				bool ok = true;
				for (int i = 0; i < names.Length; i++) {
					found[i] = prop.FindPropertyRelative(names[i]);
					if (found[i] == null) { ok = false; break; }
				}
				if (ok) { parts = found; return true; }
			}

			captions = null;
			parts = null;
			return false;
		}

		private static VisualElement[] BuildVectorCells(string[] captions, SerializedProperty[] parts) {
			var cells = new VisualElement[captions.Length];
			for (int i = 0; i < captions.Length; i++) {
				cells[i] = SperlichEditorWidgets.CreateCompactField(captions[i], parts[i]);
			}
			return cells;
		}

		// ── collections: List<>/[] , SHashSet<> , SDictionary<,> ─────────────────────

		private static Type ElemArg(Type generic, int index) {
			Type[] args = generic.GetGenericArguments();
			return index >= 0 && index < args.Length ? args[index] : null;
		}

		private static void DeleteArrayElement(SerializedProperty arr, int i) {
			if (arr == null || i < 0 || i >= arr.arraySize) return;
			SerializedProperty el = arr.GetArrayElementAtIndex(i);
			if (el.propertyType == SerializedPropertyType.ObjectReference && el.objectReferenceValue != null) {
				el.objectReferenceValue = null; // Unity's two-step delete for object-reference arrays
			}
			arr.DeleteArrayElementAtIndex(i);
		}

		private static bool IsDuplicateAt(SerializedProperty arr, int i) {
			SerializedProperty a = arr.GetArrayElementAtIndex(i);
			for (int j = 0; j < i; j++) {
				if (SerializedProperty.DataEquals(a, arr.GetArrayElementAtIndex(j))) return true;
			}
			return false;
		}

		private static VisualElement DuplicateWarning(string msg) => new Label(msg) {
			style = { fontSize = 9, color = SperlichEditorTheme.BadgeDangerBg, marginTop = 1, marginLeft = 2, whiteSpace = WhiteSpace.Normal }
		};

		internal static VisualElement BuildArrayBackedList(SerializedProperty arr, SerializedProperty owner, string title,
			bool warnDuplicates, Type elemType, string addText = "+ Eintrag", string emptyText = "Keine Einträge") {

			if (arr == null || !arr.isArray) return FullWidthPropertyField(owner);
			SerializedObject so = arr.serializedObject;
			string persist = (so.targetObject != null ? so.targetObject.GetType().Name : "x") + "/" + owner.propertyPath;

			var (element, rebuild) = SperlichEditorWidgets.CreateCollectionList(
				title, persist,
				() => arr.arraySize,
				() => { arr.arraySize++; so.ApplyModifiedProperties(); },
				i => { DeleteArrayElement(arr, i); so.ApplyModifiedProperties(); },
				(a, b) => { arr.MoveArrayElement(a, b); so.ApplyModifiedProperties(); },
				(i, host) => {
					VisualElement ctl = SperlichEditorWidgets.CompactControl(arr.GetArrayElementAtIndex(i).Copy(), Accent, _ => elemType);
					ctl.style.flexGrow = 1;
					host.Add(ctl);
					if (warnDuplicates && IsDuplicateAt(arr, i)) host.Add(DuplicateWarning("Doppelter Wert — wird beim Serialisieren verworfen"));
				},
				Accent, emptyText, addText);

			SerializedProperty sizeProp = arr.Copy().FindPropertyRelative("Array.size") ?? arr;
			element.TrackPropertyValue(sizeProp, _ => rebuild());
			return element;
		}

		internal static VisualElement BuildDictionaryRow(SerializedProperty prop) {
			SerializedProperty keys = prop.FindPropertyRelative("_keys");
			SerializedProperty values = prop.FindPropertyRelative("_values");
			if (keys == null || values == null || !keys.isArray) return BuildNestedFoldout(prop);
			SerializedObject so = prop.serializedObject;
			string persist = (so.targetObject != null ? so.targetObject.GetType().Name : "x") + "/" + prop.propertyPath;

			var (element, rebuild) = SperlichEditorWidgets.CreateCollectionList(
				prop.displayName, persist,
				() => keys.arraySize,
				() => { keys.arraySize++; values.arraySize++; so.ApplyModifiedProperties(); },
				i => { DeleteArrayElement(keys, i); DeleteArrayElement(values, i); so.ApplyModifiedProperties(); },
				(a, b) => { keys.MoveArrayElement(a, b); values.MoveArrayElement(a, b); so.ApplyModifiedProperties(); },
				(i, host) => {
					var kv = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, minWidth = 0 } };
					VisualElement k = SperlichEditorWidgets.CompactControl(keys.GetArrayElementAtIndex(i).Copy(), Accent);
					k.style.width = Length.Percent(42);
					k.style.flexShrink = 0;
					kv.Add(k);
					kv.Add(new Label("→") { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginLeft = 4, marginRight = 4, flexShrink = 0 } });
					VisualElement v = SperlichEditorWidgets.CompactControl(values.GetArrayElementAtIndex(i).Copy(), Accent);
					v.style.flexGrow = 1;
					v.style.minWidth = 0;
					kv.Add(v);
					host.Add(kv);
					if (IsDuplicateAt(keys, i)) host.Add(DuplicateWarning("Doppelter Schlüssel — wird beim Serialisieren verworfen"));
				},
				Accent, emptyText: "Leeres Dictionary", addText: "+ Eintrag");

			SerializedProperty sizeProp = keys.Copy().FindPropertyRelative("Array.size") ?? keys;
			element.TrackPropertyValue(sizeProp, _ => rebuild());
			return element;
		}

		private static VisualElement BuildNestedFoldout(SerializedProperty prop) {
			var wrap = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			wrap.style.backgroundColor = SperlichEditorTheme.BgStepBody;
			wrap.style.marginTop = 2;
			wrap.style.marginBottom = 2;
			wrap.style.paddingLeft = 6;
			wrap.style.paddingRight = 6;
			wrap.style.paddingTop = 3;
			wrap.style.paddingBottom = 4;

			var foldout = new Foldout { text = prop.displayName, value = prop.isExpanded };
			foldout.RegisterValueChangedCallback(e => prop.isExpanded = e.newValue);
			wrap.Add(foldout);

			var body = new VisualElement();
			foldout.Add(body);

			var col = new SperlichFieldColumn(140f);
			SerializedProperty child = prop.Copy();
			SerializedProperty end = prop.GetEndProperty();
			bool enter = true;
			while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end)) {
				enter = false;
				// nested members have no plan (rare to see [Header] inside a struct) — decorators skipped
				VisualElement r = BuildRow(col, child.Copy(), null);
				body.Add(r);
			}
			return wrap;
		}

		private static VisualElement FullWidthPropertyField(SerializedProperty prop) {
			// No explicit BindProperty — the element returned from CreateInspectorGUI is bound by Unity, and
			// this is how the other Sperlich editors add PropertyFields (see SperlichFieldColumn.Raw).
			return new PropertyField(prop) { style = { marginTop = 1, marginBottom = 1 } };
		}

		private static VisualElement BuildHeader(string text) {
			// Full-bleed strip on BgStep, same look as the CreateChevronSection headers in the other editors.
			var header = new Label(text.ToUpperInvariant()) {
				style = {
					unityFontStyleAndWeight = FontStyle.Bold,
					fontSize = 11,
					color = SperlichEditorTheme.TextSecondary,
					backgroundColor = SperlichEditorTheme.BgStep,
					marginLeft = -PadX, marginRight = -8,
					marginTop = 8, marginBottom = 5,
					paddingLeft = PadX, paddingRight = 8, paddingTop = 4, paddingBottom = 4,
					borderBottomWidth = 1,
					borderBottomColor = SperlichEditorTheme.BorderSubtle,
				}
			};
			return header;
		}

		private static VisualElement BuildScriptRow(SperlichFieldColumn col, SerializedProperty scriptProp) {
			var of = new ObjectField { objectType = typeof(MonoScript), style = { flexGrow = 1 } };
			of.SetValueWithoutNotify(scriptProp.objectReferenceValue);
			of.SetEnabled(false);
			SperlichFieldColumn.HideInternalLabel(of);
			VisualElement row = col.Row("Script", of);
			row.style.opacity = 0.75f;
			row.style.marginBottom = 4;
			return row;
		}

		private static bool IsFlags(SperlichInspectorPlan.MemberMeta meta) {
			Type t = meta?.Field?.FieldType;
			return t != null && t.IsEnum && t.GetCustomAttribute<FlagsAttribute>() != null;
		}

		private static StyleSheet ResolveStyleSheet() {
			if (cachedSheet != null) return cachedSheet;
			foreach (string guid in AssetDatabase.FindAssets("SperlichInspector t:StyleSheet")) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.EndsWith("SperlichInspector.uss", StringComparison.OrdinalIgnoreCase)) {
					cachedSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
					if (cachedSheet != null) return cachedSheet;
				}
			}
			return null;
		}
	}
}
