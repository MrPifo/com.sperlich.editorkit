using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>Public entry points for custom editors that build their own layout but want the SInspector
	/// field rows (attributes, conditions, prefab overrides, multi-object editing) for nested data.</summary>
	public static partial class SperlichInspectorEngine {

		/// <summary>Adds the SInspector stylesheet and focus accent to a custom editor root.</summary>
		public static void ApplyTheme(VisualElement root) {
			if (root == null) return;
			root.AddToClassList(RootClass);
			StyleSheet sheet = ResolveStyleSheet();
			if (sheet != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
			SperlichEditorWidgets.EnableFocusAccent(root);
		}

		/// <summary>Builds one row per visible child field of <paramref name="parent"/> (a nested class, struct or
		/// <c>[SerializeReference]</c> object), with the same attribute support as a full SInspector.
		/// <paramref name="include"/> filters by field name; <c>null</c> builds every field.</summary>
		public static VisualElement BuildChildRows(SerializedProperty parent, SperlichFieldColumn col = null, Func<string, bool> include = null) {
			var container = new VisualElement();
			if (parent == null) return container;

			SerializedObject so = parent.serializedObject;
			Type parentType = ResolveChildOwnerType(parent);
			SperlichInspectorPlan plan = parentType != null ? SperlichInspectorPlan.For(parentType) : null;

			var members = new List<Member>();
			SerializedProperty child = parent.Copy();
			SerializedProperty end = parent.GetEndProperty();
			bool enter = true;
			while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end)) {
				enter = false;
				if (include != null && !include(child.name)) continue;
				members.Add(new Member(child.Copy(), plan?.Get(child.name)));
			}

			string parentPath = parent.propertyPath;
			Func<object[]> targetResolver = () => ResolvePropertyTargets(so, parentPath);
			BuildPlanMembers(container, col ?? new SperlichFieldColumn(140f), so, plan, members, targetResolver);
			return container;
		}

		private static Type ResolveChildOwnerType(SerializedProperty parent) {
			if (parent.propertyType == SerializedPropertyType.ManagedReference) {
				return parent.managedReferenceValue?.GetType();
			}
			return ResolvePropertyType(parent.serializedObject, parent.propertyPath);
		}
	}
}
