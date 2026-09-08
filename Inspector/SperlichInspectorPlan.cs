using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Cached, reflection-built description of one serialized type for <see cref="SperlichInspectorEngine"/>.
	/// Built once per <see cref="Type"/> and kept in a static cache until the next domain reload. Holds only
	/// what the engine cannot get from <c>SerializedProperty</c> alone: the backing <see cref="FieldInfo"/>
	/// (for attribute lookup) plus the decorator metadata (<c>[Header]</c>, <c>[Space]</c>, <c>[Tooltip]</c>,
	/// <c>[Range]</c>, <c>[TextArea]</c>/<c>[Multiline]</c>) and whether the field's type has its own
	/// <see cref="UnityEditor.PropertyDrawer"/>.
	/// </summary>
	public sealed class SperlichInspectorPlan {

		/// <summary>Per-field metadata, keyed by the serialized field name (== <c>SerializedProperty.name</c>).</summary>
		public sealed class MemberMeta {
			public FieldInfo Field;
			public string Header;
			public float SpaceBefore;
			public string Tooltip;
			public bool HasRange;
			public float RangeMin;
			public float RangeMax;
			public bool Multiline;
			public int MultilineRows;
			public bool ElementTypeHasDrawer;
			/// <summary><c>null</c> = no <c>[SRow]</c>; otherwise the (possibly empty) horizontal-group key.</summary>
			public string RowGroup;
			public bool HasMinMax;
			public float MinMaxLow;
			public float MinMaxHigh;
		}

		private static readonly Dictionary<Type, SperlichInspectorPlan> Cache = new();

		private readonly Dictionary<string, MemberMeta> members = new();

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
				}
			}
		}

		private static MemberMeta BuildMeta(FieldInfo f) {
			var meta = new MemberMeta { Field = f };

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

			meta.ElementTypeHasDrawer = PropertyDrawerRegistry.HasCustomDrawer(ElementType(f.FieldType));
			return meta;
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
