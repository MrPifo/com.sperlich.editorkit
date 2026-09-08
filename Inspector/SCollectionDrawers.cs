using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Property drawers so <see cref="SDictionary{TKey,TValue}"/> / <see cref="SHashSet{T}"/> also render in
	/// the Sperlich collection style outside a <see cref="SInspectorAttribute"/> inspector (inside one the
	/// engine draws them directly). They reuse the engine's collection builders.
	/// </summary>
	[CustomPropertyDrawer(typeof(SDictionary<,>), true)]
	public sealed class SDictionaryDrawer : PropertyDrawer {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			var root = new VisualElement();
			root.AddToClassList(SperlichInspectorEngine.RootClass);
			root.Add(SperlichInspectorEngine.BuildDictionaryRow(property.Copy()));
			return root;
		}
	}

	[CustomPropertyDrawer(typeof(SHashSet<>), true)]
	public sealed class SHashSetDrawer : PropertyDrawer {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			var root = new VisualElement();
			root.AddToClassList(SperlichInspectorEngine.RootClass);

			Type ft = fieldInfo?.FieldType;
			Type elem = ft != null && ft.IsGenericType ? ft.GetGenericArguments()[0] : null;

			root.Add(SperlichInspectorEngine.BuildArrayBackedList(
				property.FindPropertyRelative("_items"), property.Copy(), property.displayName,
				warnDuplicates: true, elemType: elem, addText: "+ Wert", emptyText: "Leeres Set"));
			return root;
		}
	}
}
