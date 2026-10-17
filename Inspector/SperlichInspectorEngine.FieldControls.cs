using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichInspectorEngine {

		static readonly Dictionary<Type, Func<SerializedProperty, FieldInfo, VisualElement>> FieldControls = new();

		/// <summary>Registers a control for every field of <paramref name="valueType"/> (e.g. a custom value struct).
		/// The engine puts the control in the normal label column and adds the prefab-override bar, so the row looks
		/// like any built-in one. Register from an <c>[InitializeOnLoad]</c> class. A later call replaces the factory.</summary>
		public static void RegisterFieldControl(Type valueType, Func<SerializedProperty, FieldInfo, VisualElement> factory) {
			if (valueType == null || factory == null) return;
			FieldControls[valueType] = factory;
		}

		private static bool TryBuildFieldControl(SerializedProperty prop, SperlichInspectorPlan.MemberMeta meta, out VisualElement control) {
			control = null;
			Type type = meta?.Field?.FieldType;
			if (type == null || !FieldControls.TryGetValue(type, out var factory)) return false;
			control = factory(prop, meta.Field);
			return control != null;
		}
	}
}
