using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Answers one question for <see cref="SperlichInspectorEngine"/>: does a given field type have its own
	/// <see cref="PropertyDrawer"/> (via <c>[CustomPropertyDrawer]</c>)? If yes, the engine just emits a
	/// <c>PropertyField</c> so that drawer runs (e.g. <c>SEventPropertyDrawer</c>) instead of trying to
	/// rebuild the type from its child properties.
	///
	/// <para>Built once from <see cref="TypeCache"/> and cached for the lifetime of the domain. The
	/// <c>[CustomPropertyDrawer]</c> attribute keeps its target type / "use for children" flag in internal
	/// fields, so they are read by reflection — the same approach NaughtyAttributes / Tri-Inspector use.</para>
	/// </summary>
	public static class PropertyDrawerRegistry {

		private static readonly FieldInfo TypeField =
			typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo UseForChildrenField =
			typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);

		// Exact type -> has a PropertyDrawer.
		private static readonly HashSet<Type> ExactTypes = new();
		// Base type -> a PropertyDrawer that also applies to subclasses ("use for children").
		private static readonly List<Type> ChildDrawerBases = new();
		// Simple type names (incl. "use for children" subclasses) — the only handle a SerializedProperty of a
		// nested Generic gives us (SerializedProperty.type).
		private static readonly HashSet<string> DrawerTypeNames = new();

		private static bool built;

		private static void EnsureBuilt() {
			if (built) return;
			built = true;

			if (TypeField == null) {
				// Internal field names changed in this Unity version — degrade gracefully (engine then just
				// recurses into every serializable type; a themed PropertyField is still used as the leaf).
				Debug.LogWarning("[Sperlich.EditorKit] PropertyDrawerRegistry: could not reflect CustomPropertyDrawer internals — custom drawers may render un-themed.");
				return;
			}

			foreach (Type drawerType in TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>()) {
				// DecoratorDrawer (e.g. Header/Space) is not a value drawer — ignore it here.
				if (!typeof(PropertyDrawer).IsAssignableFrom(drawerType)) continue;

				foreach (CustomPropertyDrawer attr in drawerType.GetCustomAttributes<CustomPropertyDrawer>(false)) {
					if (TypeField.GetValue(attr) is not Type target) continue;
					ExactTypes.Add(target);
					DrawerTypeNames.Add(target.Name);
					bool useForChildren = UseForChildrenField != null && UseForChildrenField.GetValue(attr) is true;
					if (useForChildren) {
						ChildDrawerBases.Add(target);
						foreach (Type sub in TypeCache.GetTypesDerivedFrom(target)) DrawerTypeNames.Add(sub.Name);
					}
				}
			}
		}

		/// <summary>True when Unity has a <see cref="PropertyDrawer"/> registered for <paramref name="type"/>
		/// (directly, for an open generic of it, or via a "use for children" drawer on a base type).</summary>
		public static bool HasCustomDrawer(Type type) {
			if (type == null) return false;
			EnsureBuilt();

			if (ExactTypes.Contains(type)) return true;
			if (type.IsGenericType && ExactTypes.Contains(type.GetGenericTypeDefinition())) return true;

			for (int i = 0; i < ChildDrawerBases.Count; i++) {
				Type baseType = ChildDrawerBases[i];
				if (baseType.IsAssignableFrom(type)) return true;
				if (baseType.IsGenericTypeDefinition && IsSubclassOfRawGeneric(baseType, type)) return true;
			}
			return false;
		}

		/// <summary>Whether a <see cref="PropertyDrawer"/> is registered for a type of this simple name — the
		/// only identity a nested <c>Generic</c> <see cref="SerializedProperty"/> exposes
		/// (<c>SerializedProperty.type</c>).</summary>
		public static bool HasCustomDrawerForName(string simpleTypeName) {
			if (string.IsNullOrEmpty(simpleTypeName)) return false;
			EnsureBuilt();
			return DrawerTypeNames.Contains(simpleTypeName);
		}

		private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
			for (Type t = toCheck; t != null && t != typeof(object); t = t.BaseType) {
				Type cur = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
				if (cur == generic) return true;
			}
			return false;
		}
	}
}
