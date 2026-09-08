using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Blanket fallback inspector for every <see cref="MonoBehaviour"/> / <see cref="ScriptableObject"/>.
	/// Because it is registered <c>isFallback</c>, it only ever runs when a type has no other custom editor
	/// (so <c>SText</c>, <c>FlexContainer</c>, <c>Transform</c>, … are untouched).
	///
	/// <para>If the inspected type carries <see cref="SInspectorAttribute"/> the body is drawn by
	/// <see cref="SperlichInspectorEngine"/>; otherwise it renders the plain default inspector, i.e. the
	/// fallback editor is invisible.</para>
	/// </summary>
	internal static class SInspectorGate {

		public static bool IsEnabledFor(Type type) =>
			type != null && Attribute.IsDefined(type, typeof(SInspectorAttribute), inherit: true);

		public static VisualElement Build(Editor editor) {
			Type type = editor.target != null ? editor.target.GetType() : null;

			VisualElement root;
			if (IsEnabledFor(type)) {
				root = SperlichInspectorEngine.Build(editor.serializedObject, type);
				// Pull out to the inspector edges so the panel background + section strips bleed full-width,
				// exactly like the other Sperlich editors (SText, FlexContainer …).
				root.style.marginLeft = -15;
				root.style.marginRight = -4;
			} else {
				root = new VisualElement();
				InspectorElement.FillDefaultInspector(root, editor.serializedObject, editor);
			}

			SperlichInspectorScroll.Preserve(root, editor.target);
			return root;
		}
	}

	[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
	[CanEditMultipleObjects]
	public class SInspectorMonoBehaviourEditor : Editor {
		public override VisualElement CreateInspectorGUI() => SInspectorGate.Build(this);
	}

	[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
	[CanEditMultipleObjects]
	public class SInspectorScriptableObjectEditor : Editor {
		public override VisualElement CreateInspectorGUI() => SInspectorGate.Build(this);
	}
}
