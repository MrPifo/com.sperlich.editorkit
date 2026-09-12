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
	///
	/// <para>Split across partials: <c>.Groups</c> ([Box] runs), <c>.Members</c> ([SButton] / [ButtonGroup]
	/// / [ShowProperty] / [ShowField]) and <c>.Decorations</c> ([HLine] / [SReadOnly] / [SuffixLabel] /
	/// [Unit] / [InlineButton] / [TintColor] / [OnValueChanged] / [ProgressBar]).</para>
	/// </summary>
	public static partial class SperlichInspectorEngine {

		public const string RootClass = "sperlich-inspector";
		private static readonly Color Accent = SperlichEditorTheme.ButtonAccent;

		/// <summary>Resolves <c>[AccentColor]</c> for a field, falling back to the theme <see cref="Accent"/>.</summary>
		private static Color ResolveAccent(SperlichInspectorPlan.MemberMeta meta) =>
			SperlichEditorWidgets.ResolveColor(meta?.AccentColorHex, meta?.AccentColorTint ?? TintColor.None) ?? Accent;

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
			public readonly string Name;
			public Member(SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta) {
				Prop = prop; Meta = meta; Name = prop.name;
			}
		}

		private static void BuildInto(VisualElement container, SerializedObject so, SperlichInspectorPlan plan) {
			var col = new SperlichFieldColumn(150f);
			bool showScript = ResolveShowScript(so);

			var members = new List<Member>();
			SerializedProperty it = so.GetIterator();
			bool enterChildren = true;
			while (it.NextVisible(enterChildren)) {
				enterChildren = false;
				if (it.name == "m_Script") {
					if (showScript) container.Add(BuildScriptRow(col, it.Copy()));
					continue;
				}
				members.Add(new Member(it.Copy(), plan?.Get(it.name)));
			}

			BuildPlanMembers(container, col, so, plan, members, null);
		}

		private static void BuildPlanMembers(VisualElement container, SperlichFieldColumn col, SerializedObject so, SperlichInspectorPlan plan, List<Member> members, Func<object[]> targetResolver) {
			List<PendingEmit> pending = BuildPendingEmits(plan, members, so, targetResolver);
			Dictionary<int, (int end, BoxAttribute box, string firstName)> boxSpans = ComputeBoxSpans(members);
			(Dictionary<int, string> tabIndexGroup, Dictionary<string, TabGroupSpec> tabSpecs) = ComputeTabGroups(members);
			var tabGroupBuilt = new HashSet<string>();

			VisualElement target = container;
			int activeBoxEnd = -1;

			for (int i = 0; i < members.Count; i++) {
				if (activeBoxEnd == i) { target = container; activeBoxEnd = -1; }

				// [TabGroup]: pulled out of normal in-order emission (members can be scattered across the
				// class) — the whole tab bar is built once, at the first member of the group.
				if (tabIndexGroup.TryGetValue(i, out string tgKey)) {
					if (tabGroupBuilt.Add(tgKey)) {
						target.Add(BuildTabGroupBlock(tgKey, tabSpecs[tgKey], members, col, so, plan));
					}
					continue;
				}

				// First member overall: no leading [Space]/[Header] gap at the very top of the inspector.
				bool tightHeader = i == 0;
				if (boxSpans.TryGetValue(i, out (int end, BoxAttribute box, string firstName) span)) {
					target = BuildBoxContainer(container, so, span.box, span.firstName);
					activeBoxEnd = span.end;
					// A [Header] as the box's first child butts flush against the chevron strip; a plain
					// first row keeps a little breathing room at the top of the body.
					bool leadHeader = members[i].Meta != null && !string.IsNullOrEmpty(members[i].Meta.Header);
					tightHeader = leadHeader;
					if (!leadHeader) target.style.paddingTop = 3;
				}

				FlushPinned(pending, target, so, before: members[i].Name);

				int next = EmitMemberAt(target, col, members, i, so, plan, tightHeader);
				for (int k = i; k < next; k++) FlushPinned(pending, target, so, after: members[k].Name);
				i = next - 1;
			}

			FlushUnpinned(pending, container, so);
		}

		/// <summary>Emits the member (or the whole <c>[SRow]</c> run) starting at <paramref name="i"/> into
		/// <paramref name="parent"/> and returns the index of the next member to process.</summary>
		private static int EmitMemberAt(VisualElement parent, SperlichFieldColumn col, List<Member> members, int i, SerializedObject so, SperlichInspectorPlan plan, bool tightHeader = false) {
			Member m = members[i];
			string groupKey = m.Meta?.RowGroup;

			// [ShowIf] / [HideIf]: route the member's decorators + row into a wrapper we can toggle as a unit.
			SperlichInspectorPlan.MemberMeta.VisCondition cond = m.Meta?.Visibility;
			VisualElement sink = cond != null ? new VisualElement { style = { flexShrink = 0 } } : parent;
			int next;

			// [SRow] on an inline-able scalar -> collect the adjacent same-key run and lay it out horizontally.
			if (groupKey != null && CanInline(m.Prop)) {
				int end = i;
				while (end < members.Count
				       && members[end].Meta?.RowGroup == groupKey
				       && CanInline(members[end].Prop)) {
					end++;
				}
				EmitDecorators(sink, m.Meta, so, tightHeader);
				sink.Add(BuildHorizontalGroup(members, i, end));
				next = end;
			} else {
				EmitDecorators(sink, m.Meta, so, tightHeader);
				VisualElement row = BuildRow(col, m.Prop, m.Meta);
				if (m.Meta != null && !string.IsNullOrEmpty(m.Meta.Tooltip)) row.tooltip = m.Meta.Tooltip;
				row = PostProcessRow(row, m.Prop, m.Meta, so);
				sink.Add(row);
				next = i + 1;
			}

			if (cond != null) {
				ApplyVisibilityCondition(sink, cond, so);
				parent.Add(sink);
			}
			return next;
		}

		/// <summary>Per-type <c>[SInspector(showScript:)]</c> OR the global Tools-menu toggle.</summary>
		private static bool ResolveShowScript(SerializedObject so) {
			if (SInspectorMenu.ShowScriptField) return true;
			Type t = so?.targetObject != null ? so.targetObject.GetType() : null;
			if (t == null) return false;
			var attr = (SInspectorAttribute)Attribute.GetCustomAttribute(t, typeof(SInspectorAttribute), true);
			return attr != null && attr.ShowScript;
		}

		private static void EmitDecorators(VisualElement container, SperlichInspectorPlan.MemberMeta meta, SerializedObject so, bool tightHeader = false) {
			if (meta == null) return;
			if (!tightHeader && meta.SpaceBefore > 0f) container.Add(new VisualElement { style = { height = meta.SpaceBefore, flexShrink = 0 } });
			if (meta.HLines != null) {
				foreach ((string label, string colorHtml, TintColor tint, LineStyle style, HLineAlign align, HLinePlacement placement) in meta.HLines) {
					Color c = SperlichEditorWidgets.ResolveColor(colorHtml, tint) ?? SperlichEditorTheme.BorderStrong;
					container.Add(SperlichEditorWidgets.CreateSeparatorLine(label, c, style, align, placement));
				}
			}
			if (!string.IsNullOrEmpty(meta.Header)) container.Add(BuildHeader(meta.Header, tightHeader));

			if (meta.InfoBoxes != null) {
				foreach ((string message, InfoBoxType type, string visibleIf) in meta.InfoBoxes) {
					SperlichEditorWidgets.MessageKind kind = type switch {
						InfoBoxType.Warning => SperlichEditorWidgets.MessageKind.Warning,
						InfoBoxType.Error => SperlichEditorWidgets.MessageKind.Error,
						_ => SperlichEditorWidgets.MessageKind.Info,
					};
					VisualElement box = SperlichEditorWidgets.CreateMessageBox(message, kind);
					if (!string.IsNullOrEmpty(visibleIf) && so != null) {
						var cond = new SperlichInspectorPlan.MemberMeta.VisCondition { Member = visibleIf, Values = Array.Empty<object>(), Hide = false };
						ApplyVisibilityCondition(box, cond, so);
					}
					container.Add(box);
				}
			}
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
			// Per-cell prefab-override affordance ([SRow] members had none). Slim gutter offset so the bar
			// sits in the inter-cell gap rather than under the previous cell.
			SperlichPrefabOverride.Attach(cell, caption, m.Prop, barLeft: -3);
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
					if (m.Meta != null && m.Meta.HasRange) {
						bool intMode = prop.propertyType == SerializedPropertyType.Integer;
						VisualElement slider = SperlichEditorWidgets.CreateRangeSlider(prop, m.Meta.RangeMin, m.Meta.RangeMax, intMode, ResolveAccent(m.Meta));
						slider.style.flexGrow = 1;
						return slider;
					}
					return SperlichEditorWidgets.CreateDragNumberField(prop);
			}
		}

		private static VisualElement BuildRow(SperlichFieldColumn col, SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta) {
			Type declaredType = meta?.Field?.FieldType;

			// [EnumToggleButtons] -> segmented (plain) / toggle bar ([Flags]). Before the generic enum branch.
			if (meta != null && meta.EnumToggleButtons && prop.propertyType == SerializedPropertyType.Enum) {
				VisualElement toggles = BuildEnumToggleButtons(prop, meta);
				return OverrideRow(col.Row(meta.EnumToggleLabel ?? prop.displayName, toggles), prop);
			}

			// [ProgressBar] -> read-only fill bar tracking the value.
			if (meta != null && meta.HasProgressBar
			    && (prop.propertyType == SerializedPropertyType.Integer || prop.propertyType == SerializedPropertyType.Float)) {
				return OverrideRow(col.Row(prop.displayName, BuildProgressBarControl(prop, meta)), prop);
			}

			// [Percent] -> editable 0-100% field mapped onto the field's own [min,max].
			if (meta != null && meta.HasPercent && prop.propertyType == SerializedPropertyType.Float) {
				return OverrideRow(col.Row(prop.displayName, SperlichEditorWidgets.CreatePercentField(prop, meta.PercentMin, meta.PercentMax, ResolveAccent(meta))), prop);
			}

			// [Knob] -> rotary dial (custom range, or a KnobRange angle preset).
			if (meta != null && meta.HasKnob
			    && (prop.propertyType == SerializedPropertyType.Integer || prop.propertyType == SerializedPropertyType.Float)) {
				return OverrideRow(col.Row(prop.displayName, SperlichEditorWidgets.CreateKnobField(prop, meta.KnobMin, meta.KnobMax, meta.KnobDiameter, ResolveAccent(meta))), prop);
			}

			// [Stepper] -> [-][value][+].
			if (meta != null && meta.HasStepper
			    && (prop.propertyType == SerializedPropertyType.Integer || prop.propertyType == SerializedPropertyType.Float)) {
				return OverrideRow(col.Row(prop.displayName, SperlichEditorWidgets.CreateStepperField(prop, meta.StepperStep, meta.StepperMin, meta.StepperMax, Accent)), prop);
			}

			// [Expandable] -> object field with the assigned asset's own inspector foldable inline below it.
			if (meta != null && meta.IsExpandable && prop.propertyType == SerializedPropertyType.ObjectReference) {
				return OverrideRow(SperlichEditorWidgets.CreateExpandableField(prop, meta.Field?.FieldType, meta.ExpandableDefaultOpen, prop.displayName, Accent), prop);
			}

			// [Scene] -> Build-Settings scene dropdown (string name / int build index).
			if (meta != null && meta.IsScene
			    && (prop.propertyType == SerializedPropertyType.String || prop.propertyType == SerializedPropertyType.Integer)) {
				bool intMode = prop.propertyType == SerializedPropertyType.Integer;
				VisualElement sd = SperlichEditorWidgets.CreateSceneDropdown(prop, intMode, meta.SceneUseFullPath, Accent);
				return OverrideRow(col.Row(prop.displayName, sd), prop);
			}

			// Single [SerializeReference] field -> native, explicitly-bound PropertyField (only it renders the
			// polymorphic type picker + sub-fields), wrapped so it gets the prefab-override bar / Apply-Revert
			// menu (the managed-reference PropertyField doesn't surface those itself).
			if (prop.propertyType == SerializedPropertyType.ManagedReference) {
				return OverrideRow(FullWidthPropertyField(prop), prop);
			}

			// [SerializeReference] list -> Sperlich collection card; each element is a bound PropertyField
			// (via CompactControl) so it keeps the type picker AND gets a per-element override bar / menu.
			if (prop.isArray && meta != null && meta.IsSerializeReference) {
				return OverrideRow(BuildArrayBackedList(prop, prop, prop.displayName, warnDuplicates: false,
					elemType: declaredType != null ? SperlichInspectorPlan.ElementType(declaredType) : null,
					polymorphic: true, accent: ResolveAccent(meta)), prop);
			}

			// SDictionary<,> / SHashSet<> -> Sperlich key→value / value list (before the generic-foldout branch,
			// which would otherwise expand the two backing _keys/_values lists raw).
			if (declaredType != null && declaredType.IsGenericType) {
				Type gd = declaredType.GetGenericTypeDefinition();
				if (gd == typeof(SDictionary<,>)) return OverrideRow(BuildDictionaryRow(prop, ResolveAccent(meta)), prop);
				if (gd == typeof(SHashSet<>)) return OverrideRow(BuildArrayBackedList(prop.FindPropertyRelative("_items"), prop, prop.displayName, warnDuplicates: true, elemType: ElemArg(declaredType, 0), addText: "+ Add", emptyText: "Empty set", accent: ResolveAccent(meta)), prop);
			}

			// Arrays / Lists (element type without its own drawer) -> Sperlich compact-row collection editor.
			bool isCollection = prop.isArray && prop.propertyType != SerializedPropertyType.String;
			bool elementHasDrawer = meta != null && meta.ElementTypeHasDrawer;
			if (isCollection && !elementHasDrawer) {
				return OverrideRow(BuildArrayBackedList(prop, prop, prop.displayName, warnDuplicates: false,
					elemType: declaredType != null ? SperlichInspectorPlan.ElementType(declaredType) : null,
					accent: ResolveAccent(meta)), prop);
			}

			// Collections whose element type has its own drawer (List<SEvent>, …) -> native list drawer.
			bool hasOwnDrawer = elementHasDrawer || PropertyDrawerRegistry.HasCustomDrawerForName(prop.type);
			if (isCollection) {
				return FullWidthPropertyField(prop);
			}

			// Single field with its own PropertyDrawer (SEvent, …). The drawer doesn't surface the
			// prefab-override bar, so add ours.
			if (hasOwnDrawer) {
				return OverrideRow(FullWidthPropertyField(prop), prop);
			}

			// Nested serializable struct/class without a drawer -> foldout that recurses with the same engine.
			// Wrapped so the foldout itself carries the override bar when any descendant field is changed
			// (its child rows still get their own per-field bar + Apply/Revert; the outer one bails when a
			// right-click lands inside a child scope).
			if (prop.propertyType == SerializedPropertyType.Generic && prop.hasVisibleChildren) {
				return OverrideRow(BuildNestedFoldout(prop), prop);
			}

			// Bool -> pill toggle (mixed-value aware). Not a bound BaseField, so it needs the manual bar.
			if (prop.propertyType == SerializedPropertyType.Boolean) {
				Color? boolAccent = SperlichEditorWidgets.ResolveColor(meta?.AccentColorHex, meta?.AccentColorTint ?? TintColor.None);
				var toggle = new SperlichToggleField(prop, boolAccent);
				VisualElement boolRow = col.Row(prop.displayName, toggle);
				toggle.style.flexGrow = 0; // col.Row stretches controls; keep the toggle sized to its content
				return OverrideRow(boolRow, prop);
			}

			// Enum -> flat dropdown / flags multi-select. Also not bound BaseFields -> manual bar.
			if (prop.propertyType == SerializedPropertyType.Enum) {
				VisualElement dd = IsFlags(meta)
					? SperlichEditorWidgets.CreateFlagsDropdown(prop, ResolveAccent(meta))
					: SperlichEditorWidgets.CreateEnumDropdown(prop, ResolveAccent(meta), null, meta?.Field?.FieldType);
				return OverrideRow(col.Row(prop.displayName, dd), prop);
			}

			// LayerMask -> Sperlich multi-select dropdown (Unity's own field is the odd one out otherwise).
			if (prop.propertyType == SerializedPropertyType.LayerMask) {
				return OverrideRow(col.Row(prop.displayName, SperlichEditorWidgets.CreateLayerMaskDropdown(prop, ResolveAccent(meta))), prop);
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
				return OverrideRow(col.Row(prop.displayName, cluster), prop);
			}

			// Object reference -> bound ObjectField + a grey "×" clear button on the right.
			if (prop.propertyType == SerializedPropertyType.ObjectReference) {
				Type objType = meta?.Field?.FieldType;
				bool sceneOk = prop.serializedObject.targetObject == null
					|| !EditorUtility.IsPersistent(prop.serializedObject.targetObject);
				return OverrideRow(col.Row(prop.displayName, SperlichEditorWidgets.CreateObjectField(prop, objType, sceneOk)), prop);
			}

			// Multiline string -> bound TextField in a Sperlich row.
			if (prop.propertyType == SerializedPropertyType.String && meta != null && meta.Multiline) {
				var tf = new TextField { multiline = true, style = { flexGrow = 1, whiteSpace = WhiteSpace.Normal } };
				tf.BindProperty(prop);
				SperlichFieldColumn.HideInternalLabel(tf);
				VisualElement inner = tf.Q("unity-text-input");
				if (inner != null) inner.style.minHeight = 18 * Mathf.Max(2, meta.MultilineRows);
				return OverrideRow(col.Row(prop.displayName, tf), prop);
			}

			// Plain string -> bound TextField in a Sperlich row. Same external 150px label column as every
			// other row (the PropertyField fallback below indents its own internal label differently, which
			// is what made string rows sit a few px off from number rows).
			if (prop.propertyType == SerializedPropertyType.String) {
				var tf = new TextField { style = { flexGrow = 1 } };
				tf.BindProperty(prop);
				SperlichFieldColumn.HideInternalLabel(tf);
				return OverrideRow(col.Row(prop.displayName, tf), prop);
			}

			// Plain number / [Range] -> hand-built Sperlich drag field or slider. These are not native
			// PropertyFields, so they need the manual prefab-override bar too (this was the "numbers show
			// no blue bar" gap).
			if (prop.propertyType == SerializedPropertyType.Integer || prop.propertyType == SerializedPropertyType.Float) {
				if (meta != null && meta.HasRange) {
					bool intMode = prop.propertyType == SerializedPropertyType.Integer;
					VisualElement slider = SperlichEditorWidgets.CreateRangeSlider(prop, meta.RangeMin, meta.RangeMax, intMode, ResolveAccent(meta));
					return OverrideRow(col.Row(prop.displayName, slider), prop);
				}
				if (SperlichEditorWidgets.TryGetRange(prop, out float rMin, out float rMax)) {
					bool intMode = prop.propertyType == SerializedPropertyType.Integer;
					VisualElement slider = SperlichEditorWidgets.CreateRangeSlider(prop, rMin, rMax, intMode, ResolveAccent(meta));
					return OverrideRow(col.Row(prop.displayName, slider), prop);
				}
				return OverrideRow(col.Row(prop.displayName, SperlichEditorWidgets.CreateDragNumberField(prop)), prop);
			}

			// Color -> bound ColorField in a Sperlich row.
			if (prop.propertyType == SerializedPropertyType.Color) {
				var cf = new UnityEditor.UIElements.ColorField { style = { flexGrow = 1 }, showAlpha = true };
				cf.BindProperty(prop);
				SperlichFieldColumn.HideInternalLabel(cf);
				return OverrideRow(col.Row(prop.displayName, cf), prop);
			}

			// Gradient -> bound GradientField in a Sperlich row.
			if (prop.propertyType == SerializedPropertyType.Gradient) {
				var gf = new UnityEditor.UIElements.GradientField { style = { flexGrow = 1 } };
				gf.BindProperty(prop);
				SperlichFieldColumn.HideInternalLabel(gf);
				return OverrideRow(col.Row(prop.displayName, gf), prop);
			}

			// Everything else: curve / rect / bounds / quaternion / hash128 / …
			// SperlichFieldColumn.Property returns a bound BaseField or a native PropertyField for these,
			// which draw Unity's own prefab-override bar + Apply/Revert menu.
			return col.Property(prop);
		}

		private static VisualElement OverrideRow(VisualElement row, SerializedProperty prop) {
			// A PropertyField owns and rebuilds its own child list (on rebind / re-attach, and heavily so for
			// custom UITK drawers like SEvent's). A bar added straight into it is silently cleared on the next
			// rebuild — the Apply/Revert menu still works but the blue indicator vanishes. Wrap it in a plain
			// container the field can't touch and hang the override affordances off that.
			if (row is PropertyField) {
				var wrap = new VisualElement { style = { position = Position.Relative, marginTop = 1, marginBottom = 1 } };
				row.style.marginTop = 0;
				row.style.marginBottom = 0;
				wrap.Add(row);
				// No single "row label" to bold on a drawer tree — the gutter bar carries the indicator.
				SperlichPrefabOverride.Attach(wrap, null, prop);
				return wrap;
			}
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

		/// <summary>Seeds a freshly added collection element with a value not already used by the earlier
		/// elements — so an <c>SHashSet</c> / <c>SDictionary</c> entry survives the
		/// <c>ISerializationCallbackReceiver</c> de-dup that runs on every serialize.</summary>
		private static void SeedUniqueElement(SerializedProperty arr, int idx) {
			try {
				if (arr == null || idx < 0 || idx >= arr.arraySize) return;
				SerializedProperty e = arr.GetArrayElementAtIndex(idx);
				switch (e.propertyType) {
					case SerializedPropertyType.Integer: {
						long max = long.MinValue;
						for (int j = 0; j < idx; j++) max = Math.Max(max, arr.GetArrayElementAtIndex(j).longValue);
						e.longValue = idx == 0 ? 0 : max + 1;
						break;
					}
					case SerializedPropertyType.Float: {
						float max = float.MinValue;
						for (int j = 0; j < idx; j++) max = Mathf.Max(max, arr.GetArrayElementAtIndex(j).floatValue);
						e.floatValue = idx == 0 ? 0f : max + 1f;
						break;
					}
					case SerializedPropertyType.String: {
						var used = new HashSet<string>();
						for (int j = 0; j < idx; j++) used.Add(arr.GetArrayElementAtIndex(j).stringValue);
						string s = "new";
						int n = 1;
						while (used.Contains(s)) s = "new" + (++n);
						e.stringValue = s;
						break;
					}
					case SerializedPropertyType.Enum: {
						int count = e.enumNames?.Length ?? 0;
						var used = new HashSet<int>();
						for (int j = 0; j < idx; j++) used.Add(arr.GetArrayElementAtIndex(j).enumValueIndex);
						for (int c = 0; c < count; c++) if (!used.Contains(c)) { e.enumValueIndex = c; break; }
						break;
					}
				}
			} catch { /* best effort */ }
		}

		/// <summary>Type picker for a <c>[SerializeReference]</c> list's "+" button: lists every instantiable
		/// type assignable to <paramref name="baseType"/>, and on pick appends one element and assigns a fresh
		/// instance to its <c>managedReferenceValue</c>.</summary>
		private static void ShowManagedReferenceTypeMenu(string arrPath, Type baseType, SerializedObject so) {
			var types = new List<Type>();
			if (!baseType.IsAbstract && !baseType.IsInterface && baseType.GetConstructor(Type.EmptyTypes) != null) {
				types.Add(baseType);
			}
			foreach (Type t in TypeCache.GetTypesDerivedFrom(baseType)) {
				if (t.IsAbstract || t.IsInterface || t.IsGenericTypeDefinition) continue;
				if (typeof(UnityEngine.Object).IsAssignableFrom(t)) continue; // managed refs can't hold UnityEngine.Objects
				if (t.GetConstructor(Type.EmptyTypes) == null) continue;
				types.Add(t);
			}
			types.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

			var menu = new GenericMenu();
			if (types.Count == 0) {
				menu.AddDisabledItem(new GUIContent($"No instantiable type for {baseType.Name}"));
				menu.ShowAsContext();
				return;
			}
			foreach (Type t in types) {
				Type captured = t;
				string ns = string.IsNullOrEmpty(t.Namespace) ? string.Empty : t.Namespace + "/";
				menu.AddItem(new GUIContent(ns + ObjectNames.NicifyVariableName(t.Name)), false, () => {
					SerializedProperty a = so.FindProperty(arrPath);
					if (a == null) return;
					int idx = a.arraySize;
					a.arraySize = idx + 1;
					SerializedProperty e = a.GetArrayElementAtIndex(idx);
					try { e.managedReferenceValue = Activator.CreateInstance(captured); }
					catch (Exception ex) { Debug.LogException(ex); }
					so.ApplyModifiedProperties();
				});
			}
			menu.ShowAsContext();
		}

		internal static VisualElement BuildArrayBackedList(SerializedProperty arr, SerializedProperty owner, string title,
			bool warnDuplicates, Type elemType, string addText = "+ Add", string emptyText = "No entries", bool polymorphic = false,
			Color? accent = null) {
			Color headerAccent = accent ?? Accent;

			if (arr == null || !arr.isArray) return FullWidthPropertyField(owner);
			SerializedObject so = arr.serializedObject;
			string arrPath = arr.propertyPath;
			// Re-resolve every time: SHashSet / SDictionary rewrite their backing list on each serialize
			// (ISerializationCallbackReceiver), which invalidates a cached SerializedProperty -> NRE.
			SerializedProperty Arr() => so.FindProperty(arrPath);
			string persist = (so.targetObject != null ? so.targetObject.GetType().Name : "x") + "/" + owner.propertyPath;

			// [SerializeReference] list: "+" can't just grow the array (the element would be a null managed
			// reference with no way to pick a concrete type in the compact row). Open a type menu instead.
			Action onAdd = polymorphic && elemType != null
				? () => ShowManagedReferenceTypeMenu(arrPath, elemType, so)
				: () => {
					SerializedProperty a = Arr();
					if (a == null) return;
					a.arraySize++;
					if (warnDuplicates) SeedUniqueElement(a, a.arraySize - 1);
					so.ApplyModifiedProperties();
				};

			var (element, rebuild) = SperlichEditorWidgets.CreateCollectionList(
				title, persist,
				() => { SerializedProperty a = Arr(); return a != null ? a.arraySize : 0; },
				onAdd,
				i => { SerializedProperty a = Arr(); if (a == null) return; DeleteArrayElement(a, i); so.ApplyModifiedProperties(); },
				(x, y) => { SerializedProperty a = Arr(); if (a == null) return; a.MoveArrayElement(x, y); so.ApplyModifiedProperties(); },
				(i, host) => {
					SerializedProperty a = Arr();
					if (a == null || i < 0 || i >= a.arraySize) return;
					SerializedProperty elem = a.GetArrayElementAtIndex(i).Copy();
					VisualElement ctl = SperlichEditorWidgets.CompactControl(elem, headerAccent, _ => elemType);
					ctl.style.flexGrow = 1;
					host.Add(ctl);
					SperlichPrefabOverride.Attach(host, null, elem);
					if (warnDuplicates && IsDuplicateAt(a, i)) host.Add(DuplicateWarning("Duplicate value — dropped on serialize"));
				},
				headerAccent, emptyText, addText);

			// Rebuild policy: a change to the array *size* (add / remove / undo) rebuilds the rows at once.
			// A change to an element *value* must NOT rebuild mid-edit — that would tear down the field the
			// user is dragging / typing in (drag-scrub dies on the first frame). Instead we debounce: once the
			// value edits stop for ~250 ms, one rebuild refreshes the duplicate-row warnings.
			SerializedProperty a0 = Arr();
			int lastCount = a0 != null ? a0.arraySize : 0;
			double lastEditAt = -1;
			element.TrackPropertyValue(owner.Copy(), _ => lastEditAt = EditorApplication.timeSinceStartup);
			element.schedule.Execute(() => {
				SerializedProperty a = Arr();
				int n = a != null ? a.arraySize : 0;
				if (n != lastCount) { lastCount = n; lastEditAt = -1; rebuild(); return; }
				if (lastEditAt > 0 && EditorApplication.timeSinceStartup - lastEditAt > 0.25) { lastEditAt = -1; rebuild(); }
			}).Every(100);
			return element;
		}

		internal static VisualElement BuildDictionaryRow(SerializedProperty prop, Color? accent = null) {
			Color headerAccent = accent ?? Accent;
			SerializedProperty keys0 = prop.FindPropertyRelative("_keys");
			SerializedProperty values0 = prop.FindPropertyRelative("_values");
			if (keys0 == null || values0 == null || !keys0.isArray) return BuildNestedFoldout(prop);
			SerializedObject so = prop.serializedObject;
			string keysPath = keys0.propertyPath;
			string valuesPath = values0.propertyPath;
			SerializedProperty Keys() => so.FindProperty(keysPath);
			SerializedProperty Values() => so.FindProperty(valuesPath);
			string persist = (so.targetObject != null ? so.targetObject.GetType().Name : "x") + "/" + prop.propertyPath;

			var (element, rebuild) = SperlichEditorWidgets.CreateCollectionList(
				prop.displayName, persist,
				() => { SerializedProperty k = Keys(); return k != null ? k.arraySize : 0; },
				() => {
					SerializedProperty k = Keys(), v = Values();
					if (k == null || v == null) return;
					k.arraySize++;
					v.arraySize++;
					SeedUniqueElement(k, k.arraySize - 1);
					so.ApplyModifiedProperties();
				},
				i => { SerializedProperty k = Keys(), v = Values(); if (k == null || v == null) return; DeleteArrayElement(k, i); DeleteArrayElement(v, i); so.ApplyModifiedProperties(); },
				(x, y) => { SerializedProperty k = Keys(), v = Values(); if (k == null || v == null) return; k.MoveArrayElement(x, y); v.MoveArrayElement(x, y); so.ApplyModifiedProperties(); },
				(i, host) => {
					SerializedProperty k = Keys(), v = Values();
					if (k == null || v == null || i < 0 || i >= k.arraySize || i >= v.arraySize) return;
					var kv = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, minWidth = 0 } };
					VisualElement kCtl = SperlichEditorWidgets.CompactControl(k.GetArrayElementAtIndex(i).Copy(), headerAccent);
					kCtl.style.width = Length.Percent(42);
					kCtl.style.flexShrink = 0;
					kv.Add(kCtl);
					kv.Add(new Label("→") { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginLeft = 4, marginRight = 4, flexShrink = 0 } });
					VisualElement vCtl = SperlichEditorWidgets.CompactControl(v.GetArrayElementAtIndex(i).Copy(), headerAccent);
					vCtl.style.flexGrow = 1;
					vCtl.style.minWidth = 0;
					kv.Add(vCtl);
					host.Add(kv);
					if (IsDuplicateAt(k, i)) host.Add(DuplicateWarning("Duplicate key — dropped on serialize"));
				},
				headerAccent, emptyText: "Empty dictionary", addText: "+ Add");

			// Same debounce as BuildArrayBackedList: structural (key-count) changes rebuild now, value edits
			// (a key/value being typed) settle for ~250 ms before one rebuild refreshes the dup-key warnings.
			// Rebuilding mid-edit would drop the row being changed and dispose its bound field.
			SerializedProperty k0 = Keys();
			int lastCount = k0 != null ? k0.arraySize : 0;
			double lastEditAt = -1;
			element.TrackPropertyValue(prop.Copy(), _ => lastEditAt = EditorApplication.timeSinceStartup);
			element.schedule.Execute(() => {
				SerializedProperty k = Keys();
				int n = k != null ? k.arraySize : 0;
				if (n != lastCount) { lastCount = n; lastEditAt = -1; rebuild(); return; }
				if (lastEditAt > 0 && EditorApplication.timeSinceStartup - lastEditAt > 0.25) { lastEditAt = -1; rebuild(); }
			}).Every(100);
			return element;
		}

		private static VisualElement BuildNestedFoldout(SerializedProperty prop) {
			var wrap = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			wrap.style.backgroundColor = SperlichEditorTheme.BgStepBody;
			wrap.style.marginTop = 2;
			wrap.style.marginBottom = 2;
			wrap.style.paddingLeft = 3;
			wrap.style.paddingRight = 3;
			wrap.style.paddingTop = 3;
			wrap.style.paddingBottom = 4;

			var foldout = new Foldout { text = prop.displayName, value = prop.isExpanded };
			foldout.RegisterValueChangedCallback(e => prop.isExpanded = e.newValue);
			wrap.Add(foldout);

			var body = new VisualElement();
			foldout.Add(body);

			var col = new SperlichFieldColumn(140f);
			Type nestedType = ResolvePropertyType(prop.serializedObject, prop.propertyPath);
			SperlichInspectorPlan nestedPlan = nestedType != null ? SperlichInspectorPlan.For(nestedType) : null;

			var nestedMembers = new List<Member>();
			SerializedProperty child = prop.Copy();
			SerializedProperty end = prop.GetEndProperty();
			bool enter = true;
			while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end)) {
				enter = false;
				nestedMembers.Add(new Member(child.Copy(), nestedPlan?.Get(child.name)));
			}

			Func<object[]> targetResolver = () => ResolvePropertyTargets(prop.serializedObject, prop.propertyPath);
			BuildPlanMembers(body, col, prop.serializedObject, nestedPlan, nestedMembers, targetResolver);
			return wrap;
		}

		public static object[] ResolvePropertyTargets(SerializedObject so, string propertyPath) {
			if (so == null || string.IsNullOrEmpty(propertyPath)) return Array.Empty<object>();
			UnityEngine.Object[] roots = ResolveTargets(so);
			var result = new List<object>(roots.Length);
			string[] tokens = propertyPath.Split('.');

			foreach (UnityEngine.Object root in roots) {
				if (root == null) continue;
				object current = root;
				for (int i = 0; i < tokens.Length && current != null; i++) {
					string token = tokens[i];
					if (token == "Array") {
						if (i + 1 < tokens.Length && tokens[i + 1].StartsWith("data[") && tokens[i + 1].EndsWith("]")) {
							string idxStr = tokens[i + 1].Substring(5, tokens[i + 1].Length - 6);
							if (int.TryParse(idxStr, out int idx) && current is System.Collections.IList list && idx >= 0 && idx < list.Count) {
								current = list[idx];
							} else {
								current = null;
							}
							i++;
							continue;
						}
					}
					FieldInfo fi = null;
					for (Type t = current.GetType(); t != null && t != typeof(object); t = t.BaseType) {
						fi = t.GetField(token, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (fi != null) break;
					}
					current = fi?.GetValue(current);
				}
				if (current != null) result.Add(current);
			}
			return result.ToArray();
		}

		public static Type ResolvePropertyType(SerializedObject so, string propertyPath) {
			if (so == null || string.IsNullOrEmpty(propertyPath)) return null;
			UnityEngine.Object root = so.targetObject;
			if (root == null) return null;
			Type currentType = root.GetType();
			string[] tokens = propertyPath.Split('.');

			for (int i = 0; i < tokens.Length && currentType != null; i++) {
				string token = tokens[i];
				if (token == "Array") {
					if (i + 1 < tokens.Length && tokens[i + 1].StartsWith("data[")) {
						if (currentType.IsArray) currentType = currentType.GetElementType();
						else if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(List<>)) currentType = currentType.GetGenericArguments()[0];
						i++;
						continue;
					}
				}
				FieldInfo fi = null;
				for (Type t = currentType; t != null && t != typeof(object); t = t.BaseType) {
					fi = t.GetField(token, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (fi != null) break;
				}
				currentType = fi?.FieldType;
			}
			return currentType;
		}

		private static VisualElement FullWidthPropertyField(SerializedProperty prop) {
			// Explicit BindProperty: an auto-bound PropertyField loses its [SerializeReference] managed
			// sub-tree on a rebind (Apply / Revert / reselect) and never shows the prefab-override bar for
			// managed references. Binding it directly to the property path fixes both.
			var pf = new PropertyField(prop) { style = { marginTop = 1, marginBottom = 1 } };
			pf.BindProperty(prop);
			return pf;
		}

		private static VisualElement BuildHeader(string text, bool tightTop = false) {
			// Full-bleed strip on BgStep, same look as the CreateChevronSection headers in the other editors.
			// tightTop: sits flush against a preceding strip (e.g. as the first child of a [Box] body).
			var header = new Label(text.ToUpperInvariant()) {
				style = {
					unityFontStyleAndWeight = FontStyle.Bold,
					fontSize = 11,
					color = SperlichEditorTheme.TextSecondary,
					backgroundColor = SperlichEditorTheme.BgStep,
					marginLeft = -PadX, marginRight = -8,
					marginTop = tightTop ? 0 : 8, marginBottom = 5,
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
