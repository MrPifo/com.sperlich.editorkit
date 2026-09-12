using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Cached, reflection-built description of one serialized type for <see cref="SperlichInspectorEngine"/>.
	/// Built once per <see cref="Type"/> and kept in a static cache until the next domain reload. Holds only
	/// what the engine cannot get from <c>SerializedProperty</c> alone: the backing <see cref="FieldInfo"/>
	/// (for attribute lookup), the decorator / layout metadata for every field, plus the method-button,
	/// live-display and button-group members that have no <c>SerializedProperty</c> at all.
	/// </summary>
	public sealed class SperlichInspectorPlan {

		/// <summary>Per-field metadata, keyed by the serialized field name (== <c>SerializedProperty.name</c>).</summary>
		public sealed class MemberMeta {
			public FieldInfo Field;
			public int MetadataToken;

			public string Header;
			public float SpaceBefore;
			public string Tooltip;
			public bool HasRange;
			public float RangeMin;
			public float RangeMax;
			public bool Multiline;
			public int MultilineRows;
			public bool ElementTypeHasDrawer;
			/// <summary>Field carries <c>[SerializeReference]</c> — render it (and its elements, if a list)
			/// with a native, explicitly-bound <c>PropertyField</c>.</summary>
			public bool IsSerializeReference;
			/// <summary><c>null</c> = no <c>[SRow]</c>; otherwise the (possibly empty) horizontal-group key.</summary>
			public string RowGroup;
			public bool HasMinMax;
			public float MinMaxLow;
			public float MinMaxHigh;

			// ── extension attributes ────────────────────────────────────────────────
			public bool ReadOnly;

			public bool EnumToggleButtons;
			public string EnumToggleLabel;
			public int EnumToggleButtonsPerRow;

			/// <summary>Literal suffix text, or <c>$member</c> for a polled string member. <c>null</c> = none.</summary>
			public string SuffixText;
			public bool SuffixOverlay;

			public bool HasUnit;
			public UnitOfMeasure Unit;
			public UnitOfMeasure UnitDisplayAs;
			public string UnitCustom;

			public bool HasProgressBar;
			public float PbMin;
			public float PbMax;
			public string PbColor;
			public TintColor PbTint;
			public int PbHeight;
			public string PbMinMember;
			public string PbMaxMember;
			public bool PbSegmented;
			public bool PbShowValue;
			public bool PbPercent;

			/// <summary>(<c>method</c>, <c>label</c>, <c>icon</c>) tuples for <c>[InlineButton]</c>; <c>null</c> = none.</summary>
			public List<(string method, string label, string icon)> InlineButtons;

			public bool IsScene;
			public bool SceneUseFullPath;

			public string TintColorHtml;
			public TintColor TintColorEnum;
			public bool TintBackground;

			/// <summary>From <c>[AccentColor]</c> — overrides the control's accent (slider fill, toggle on-
			/// colour, knob arc, enum dropdown, collection header). <c>null</c>/<see cref="Sperlich.EditorKit.TintColor.None"/>
			/// = theme accent.</summary>
			public string AccentColorHex;
			public TintColor AccentColorTint;

			/// <summary>(<c>label</c>, <c>colorHtml</c>, <c>tint</c>, <c>style</c>, <c>align</c>, <c>placement</c>) tuples for stacked
			/// <c>[HLine]</c>s; <c>null</c> = none.</summary>
			public List<(string label, string colorHtml, TintColor tint, LineStyle style, HLineAlign align, HLinePlacement placement)> HLines;

			/// <summary>Callback method names from <c>[OnValueChanged]</c>; <c>null</c> = none.</summary>
			public List<string> OnValueChangedMethods;

			/// <summary>Set on the field that STARTS a <c>[Box]</c> run.</summary>
			public BoxAttribute Box;
			/// <summary>Set on the field carrying <c>[EndGroup]</c> — closes an open box before this field.</summary>
			public bool EndGroup;

			/// <summary><c>[ShowIf]</c> / <c>[HideIf]</c> condition; <c>null</c> = field always visible.</summary>
			public VisCondition Visibility;

			/// <summary><c>[EnableIf]</c> / <c>[DisableIf]</c> condition; <c>null</c> = field always enabled.
			/// Reuses <see cref="VisCondition"/> — <c>Hide</c> means "disable on match" (<c>[DisableIf]</c>).</summary>
			public VisCondition EnableCondition;

			/// <summary>Parsed <c>[ShowIf]</c> / <c>[HideIf]</c> / <c>[EnableIf]</c> / <c>[DisableIf]</c> data.</summary>
			public sealed class VisCondition {
				public string Member;
				public object[] Values;
				/// <summary><c>true</c> for <c>[HideIf]</c> / <c>[DisableIf]</c> — the match result is inverted.</summary>
				public bool Hide;
			}

			/// <summary>(<c>label</c>, <c>color</c>) tuples for stacked <c>[Tag]</c>s; <c>null</c> = none.</summary>
			/// <summary>(<c>label</c>, <c>color</c>) tuples for stacked <c>[Tag]</c>s; the colour is resolved
			/// here already (hex &gt; <see cref="Sperlich.EditorKit.TintColor"/> &gt; <see cref="TagColor"/>
			/// preset) so downstream code never needs to know which source it came from.</summary>
			public List<(string label, Color color)> Tags;

			public bool Required;
			public string RequiredMessage;

			/// <summary>(<c>message</c>, <c>type</c>, <c>visibleIf</c>) tuples for stacked <c>[InfoBox]</c>es;
			/// <c>null</c> = none. <c>visibleIf</c> is <c>null</c> when the box is always shown.</summary>
			public List<(string message, InfoBoxType type, string visibleIf)> InfoBoxes;

			public bool HasPercent;
			public float PercentMin;
			public float PercentMax;

			public bool HasKnob;
			public float KnobMin;
			public float KnobMax;
			public float KnobDiameter;

			public bool HasStepper;
			public float StepperStep;
			public float StepperMin;
			public float StepperMax;

			/// <summary><c>[TabGroup]</c> tab name; <c>null</c> = not in a tab group.</summary>
			public string TabGroupTab;
			/// <summary><c>[TabGroup]</c> group key (several independent tab bars can coexist on one type).</summary>
			public string TabGroupKey;

			public bool IsExpandable;
			public bool ExpandableDefaultOpen;
		}

		/// <summary>A parameterless method rendered as a stand-alone button (<c>[SButton]</c>).</summary>
		public sealed class MethodButtonMeta {
			public MethodInfo Method;
			public string Label;
			public ButtonSize Size;
			public ButtonSize Height;
			public ButtonAnchor Anchor;
			public string After;
			public string Before;
			public string Icon;
			public int MetadataToken;
		}

		/// <summary>A set of parameterless methods sharing a <c>[ButtonGroup]</c> key — one segmented bar.</summary>
		public sealed class ButtonGroupMeta {
			public string Group;
			public readonly List<(MethodInfo method, string label, string icon)> Items = new();
			/// <summary>Declaration token of the first method — decides where the bar is placed.</summary>
			public int MetadataToken;
		}

		/// <summary>A read-only live display of a property (<c>[ShowProperty]</c>) or non-serialized field
		/// (<c>[ShowField]</c>).</summary>
		public sealed class ShowMemberMeta {
			public MemberInfo Member;
			public bool IsField;
			public string Name;
			public string Label;
			public int PollMs;
			public int MetadataToken;
			public string Suffix;
			public bool Badge;
			public TintColor Tint;
			public string ColorHex;
			public string RowGroup;
			public BoxAttribute Box;
			public List<(string label, string colorHtml, TintColor tint, LineStyle style, HLineAlign align, HLinePlacement placement)> HLines;
		}

		private static readonly Dictionary<Type, SperlichInspectorPlan> Cache = new();

		private readonly Dictionary<string, MemberMeta> members = new();

		public readonly List<MethodButtonMeta> MethodButtons = new();
		public readonly List<ButtonGroupMeta> ButtonGroups = new();
		public readonly List<ShowMemberMeta> ShowMembers = new();

		/// <summary>Metadata for the serialized field <paramref name="name"/>, or <c>null</c> when it has no
		/// reflectable backing field (e.g. Unity-internal <c>m_Script</c>).</summary>
		public MemberMeta Get(string name) => members.TryGetValue(name, out MemberMeta m) ? m : null;

		public static SperlichInspectorPlan For(Type type) {
			if (Cache.TryGetValue(type, out SperlichInspectorPlan cached)) return cached;
			var plan = new SperlichInspectorPlan();
			plan.Build(type);
			Cache[type] = plan;
			return plan;
		}

		private void Build(Type type) {
			// Walk the whole hierarchy so inherited [SerializeField] fields are covered. Base-first so a
			// shadowed name resolves to the most-derived declaration (matches Unity's serialization).
			var chain = new List<Type>();
			for (Type t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(ScriptableObject) && t != typeof(object); t = t.BaseType) {
				chain.Add(t);
			}
			chain.Reverse();

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			foreach (Type t in chain) {
				foreach (FieldInfo f in t.GetFields(flags)) {
					members[f.Name] = BuildMeta(f);
					CollectShowField(f);
				}
				foreach (MethodInfo m in t.GetMethods(flags)) {
					CollectMethod(m);
				}
				foreach (PropertyInfo p in t.GetProperties(flags)) {
					CollectShowProperty(p);
				}
			}
		}

		private static MemberMeta BuildMeta(FieldInfo f) {
			var meta = new MemberMeta { Field = f, MetadataToken = f.MetadataToken };

			if (f.GetCustomAttribute<HeaderAttribute>() is { } header) meta.Header = header.header;

			float space = 0f;
			foreach (SpaceAttribute s in f.GetCustomAttributes<SpaceAttribute>(true)) space += s.height <= 0f ? 8f : s.height;
			meta.SpaceBefore = space;

			if (f.GetCustomAttribute<TooltipAttribute>() is { } tip) meta.Tooltip = tip.tooltip;

			if (f.GetCustomAttribute<RangeAttribute>() is { } range) {
				meta.HasRange = true;
				meta.RangeMin = range.min;
				meta.RangeMax = range.max;
			}

			if (f.GetCustomAttribute<TextAreaAttribute>() is { } area) {
				meta.Multiline = true;
				meta.MultilineRows = Mathf.Max(2, area.minLines);
			} else if (f.GetCustomAttribute<MultilineAttribute>() is { } multi) {
				meta.Multiline = true;
				meta.MultilineRows = Mathf.Max(2, multi.lines);
			}

			if (f.GetCustomAttribute<SRowAttribute>() is { } srow) meta.RowGroup = srow.Group ?? string.Empty;

			if (f.GetCustomAttribute<SMinMaxAttribute>() is { } mm) {
				meta.HasMinMax = true;
				meta.MinMaxLow = Mathf.Min(mm.Min, mm.Max);
				meta.MinMaxHigh = Mathf.Max(mm.Min, mm.Max);
			}

			if (f.GetCustomAttribute<SReadOnlyAttribute>() != null) meta.ReadOnly = true;

			if (f.GetCustomAttribute<EnumToggleButtonsAttribute>() is { } etb) {
				meta.EnumToggleButtons = true;
				meta.EnumToggleLabel = etb.Label;
				meta.EnumToggleButtonsPerRow = etb.ButtonsPerRow;
			}

			if (f.GetCustomAttribute<SuffixLabelAttribute>() is { } sfx) {
				meta.SuffixText = sfx.Label;
				meta.SuffixOverlay = sfx.Overlay;
			}

			if (f.GetCustomAttribute<UnitAttribute>() is { } unit) {
				meta.HasUnit = true;
				meta.Unit = unit.Unit;
				meta.UnitDisplayAs = unit.DisplayAs;
				meta.UnitCustom = unit.Custom;
			}

			if (f.GetCustomAttribute<ProgressBarAttribute>() is { } pb) {
				meta.HasProgressBar = true;
				meta.PbMin = pb.Min;
				meta.PbMax = pb.Max;
				meta.PbColor = pb.Color;
				meta.PbTint = pb.Tint;
				meta.PbHeight = pb.Height;
				meta.PbMinMember = pb.MinMember;
				meta.PbMaxMember = pb.MaxMember;
				meta.PbSegmented = pb.Segmented;
				meta.PbShowValue = pb.ShowValue;
				meta.PbPercent = pb.Percent;
			}

			foreach (InlineButtonAttribute ib in f.GetCustomAttributes<InlineButtonAttribute>(true)) {
				(meta.InlineButtons ??= new()).Add((ib.Method, ib.Label, ib.Icon));
			}

			if (f.GetCustomAttribute<SceneAttribute>() is { } scn) {
				meta.IsScene = true;
				meta.SceneUseFullPath = scn.UseFullPath;
			}

			if (f.GetCustomAttribute<TintColorAttribute>() is { } tint) {
				meta.TintColorHtml = tint.Color;
				meta.TintColorEnum = tint.Tint;
				meta.TintBackground = tint.Background;
			}

			if (f.GetCustomAttribute<AccentColorAttribute>() is { } accent) {
				meta.AccentColorHex = accent.ColorHex;
				meta.AccentColorTint = accent.Tint;
			}

			foreach (HLineAttribute hl in f.GetCustomAttributes<HLineAttribute>(true)) {
				(meta.HLines ??= new()).Add((hl.Label, hl.Color, hl.Tint, hl.Style, hl.Align, hl.Placement));
			}

			foreach (OnValueChangedAttribute ov in f.GetCustomAttributes<OnValueChangedAttribute>(true)) {
				(meta.OnValueChangedMethods ??= new()).Add(ov.Method);
			}

			if (f.GetCustomAttribute<BoxAttribute>() is { } box) meta.Box = box;
			if (f.GetCustomAttribute<EndGroupAttribute>() != null) meta.EndGroup = true;

			if (f.GetCustomAttribute<ShowIfAttribute>() is { } showIf) {
				meta.Visibility = new MemberMeta.VisCondition { Member = showIf.Member, Values = showIf.Values, Hide = false };
			} else if (f.GetCustomAttribute<HideIfAttribute>() is { } hideIf) {
				meta.Visibility = new MemberMeta.VisCondition { Member = hideIf.Member, Values = hideIf.Values, Hide = true };
			}

			if (f.GetCustomAttribute<EnableIfAttribute>() is { } enableIf) {
				meta.EnableCondition = new MemberMeta.VisCondition { Member = enableIf.Member, Values = enableIf.Values, Hide = false };
			} else if (f.GetCustomAttribute<DisableIfAttribute>() is { } disableIf) {
				meta.EnableCondition = new MemberMeta.VisCondition { Member = disableIf.Member, Values = disableIf.Values, Hide = true };
			}

			foreach (TagAttribute tag in f.GetCustomAttributes<TagAttribute>(true)) {
				Color resolved = SperlichEditorWidgets.ResolveColor(tag.ColorHex, tag.Tint) ?? SperlichEditorWidgets.TagColorToColor(tag.Color);
				(meta.Tags ??= new()).Add((tag.Label, resolved));
			}

			if (f.GetCustomAttribute<RequiredAttribute>() is { } req) {
				meta.Required = true;
				meta.RequiredMessage = req.Message;
			}

			foreach (InfoBoxAttribute info in f.GetCustomAttributes<InfoBoxAttribute>(true)) {
				(meta.InfoBoxes ??= new()).Add((info.Message, info.Type, info.VisibleIf));
			}

			if (f.GetCustomAttribute<PercentAttribute>() is { } pct) {
				meta.HasPercent = true;
				meta.PercentMin = pct.ValueMin;
				meta.PercentMax = pct.ValueMax;
			}

			if (f.GetCustomAttribute<KnobAttribute>() is { } knob) {
				meta.HasKnob = true;
				meta.KnobMin = knob.Min;
				meta.KnobMax = knob.Max;
				meta.KnobDiameter = knob.Diameter;
			}

			if (f.GetCustomAttribute<StepperAttribute>() is { } stepper) {
				meta.HasStepper = true;
				meta.StepperStep = stepper.Step;
				meta.StepperMin = stepper.Min;
				meta.StepperMax = stepper.Max;
			}

			if (f.GetCustomAttribute<TabGroupAttribute>() is { } tabGroup) {
				meta.TabGroupTab = tabGroup.Tab;
				meta.TabGroupKey = tabGroup.Group;
			}

			if (f.GetCustomAttribute<ExpandableAttribute>() is { } expandable) {
				meta.IsExpandable = true;
				meta.ExpandableDefaultOpen = expandable.DefaultExpanded;
			}

			if (f.GetCustomAttribute<SerializeReference>() != null) meta.IsSerializeReference = true;

			meta.ElementTypeHasDrawer = PropertyDrawerRegistry.HasCustomDrawer(ElementType(f.FieldType));
			return meta;
		}

		private void CollectShowField(FieldInfo f) {
			var sf = f.GetCustomAttribute<ShowFieldAttribute>();
			if (sf == null) return;
			ShowMembers.Add(new ShowMemberMeta {
				Member = f, IsField = true, Name = f.Name,
				Label = sf.Label ?? ObjectNames.NicifyVariableName(f.Name),
				PollMs = Mathf.Max(50, sf.PollMs), MetadataToken = f.MetadataToken,
			});
		}

		private void CollectShowProperty(PropertyInfo p) {
			var sp = p.GetCustomAttribute<ShowPropertyAttribute>();
			if (sp == null) return;
			if (p.GetIndexParameters().Length != 0 || p.GetMethod == null) return;
			var row = p.GetCustomAttribute<SRowAttribute>();
			var box = p.GetCustomAttribute<BoxAttribute>();
			var tint = p.GetCustomAttribute<TintColorAttribute>();
			var accent = p.GetCustomAttribute<AccentColorAttribute>();

			List<(string, string, TintColor, LineStyle, HLineAlign, HLinePlacement)> hlines = null;
			foreach (HLineAttribute hl in p.GetCustomAttributes<HLineAttribute>(true)) {
				(hlines ??= new()).Add((hl.Label, hl.Color, hl.Tint, hl.Style, hl.Align, hl.Placement));
			}

			ShowMembers.Add(new ShowMemberMeta {
				Member = p, IsField = false, Name = p.Name,
				Label = sp.Label ?? ObjectNames.NicifyVariableName(p.Name),
				PollMs = Mathf.Max(50, sp.PollMs), MetadataToken = p.MetadataToken,
				Suffix = sp.Suffix,
				Badge = sp.Badge,
				Tint = sp.Tint != TintColor.None ? sp.Tint : (tint?.Tint ?? TintColor.None),
				ColorHex = sp.ColorHex ?? accent?.ColorHex ?? tint?.Color,
				RowGroup = row?.Group,
				Box = box,
				HLines = hlines,
			});
		}

		private void CollectMethod(MethodInfo m) {
			var sb = m.GetCustomAttribute<SButtonAttribute>();
			var bg = m.GetCustomAttribute<ButtonGroupAttribute>();
			if (sb == null && bg == null) return;

			if (m.GetParameters().Length != 0) {
				Debug.LogWarning($"[SInspector] {m.DeclaringType?.Name}.{m.Name}: [SButton]/[ButtonGroup] only supports parameterless methods — skipped.");
				return;
			}

			if (sb != null) {
				MethodButtons.Add(new MethodButtonMeta {
					Method = m,
					Label = sb.Label ?? ObjectNames.NicifyVariableName(m.Name),
					Size = sb.Size, Height = sb.Height, Anchor = sb.Anchor,
					After = sb.After, Before = sb.Before, Icon = sb.Icon,
					MetadataToken = m.MetadataToken,
				});
			}

			if (bg != null) {
				ButtonGroupMeta grp = ButtonGroups.Find(g => g.Group == bg.Group);
				if (grp == null) {
					grp = new ButtonGroupMeta { Group = bg.Group, MetadataToken = m.MetadataToken };
					ButtonGroups.Add(grp);
				}
				grp.Items.Add((m, bg.Label ?? ObjectNames.NicifyVariableName(m.Name), bg.Icon));
			}
		}

		/// <summary>For a <c>List&lt;T&gt;</c> / <c>T[]</c> field this is <c>T</c>, otherwise the field type
		/// itself — the type a per-element drawer would be registered against.</summary>
		public static Type ElementType(Type fieldType) {
			if (fieldType.IsArray) return fieldType.GetElementType();
			if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) {
				return fieldType.GetGenericArguments()[0];
			}
			return fieldType;
		}
	}
}
