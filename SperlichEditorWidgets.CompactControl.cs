using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>
		/// Renders a single value control for <paramref name="prop"/> in the Sperlich style but WITHOUT a
		/// label column — for [SRow] cells, list elements and dictionary key/value slots. Falls back to a
		/// themed <c>PropertyField</c> for shapes it does not special-case.
		/// </summary>
		/// <param name="resolveType">Optional: returns the declared C# type of a property (enum type for the
		/// value badge / flags detection, object type for the picker filter). May be <c>null</c>.</param>
		public static VisualElement CompactControl(SerializedProperty prop, Color? accent = null, Func<SerializedProperty, Type> resolveType = null) {
			Color acc = accent ?? SperlichEditorTheme.ButtonAccent;
			Type declared = resolveType?.Invoke(prop);

			// A type with its own PropertyDrawer (SEvent, …) keeps that drawer instead of being torn apart.
			if (PropertyDrawerRegistry.HasCustomDrawerForName(prop.type)) {
				var pfd = new PropertyField(prop, " ");
				SperlichFieldColumn.HideInternalLabel(pfd);
				pfd.style.flexGrow = 1;
				return pfd;
			}

			switch (prop.propertyType) {
				case SerializedPropertyType.Boolean:
					return new SperlichToggleField(prop) { style = { flexGrow = 0 } };

				case SerializedPropertyType.Integer:
				case SerializedPropertyType.Float:
					return CreateDragNumberField(prop);

				case SerializedPropertyType.String: {
					var tf = new TextField { style = { flexGrow = 1, minWidth = 0 } };
					tf.BindProperty(prop);
					SperlichFieldColumn.HideInternalLabel(tf);
					return tf;
				}

				case SerializedPropertyType.Enum: {
					bool flags = declared != null && declared.IsEnum && declared.GetCustomAttribute<FlagsAttribute>() != null;
					VisualElement dd = flags ? CreateFlagsDropdown(prop, acc) : CreateEnumDropdown(prop, acc, null, declared);
					dd.style.flexGrow = 1;
					return dd;
				}

				case SerializedPropertyType.Color: {
					var cf = new ColorField { showAlpha = true, style = { flexGrow = 1, minWidth = 0 } };
					cf.BindProperty(prop);
					SperlichFieldColumn.HideInternalLabel(cf);
					return cf;
				}

				case SerializedPropertyType.Gradient: {
					var gf = new GradientField { style = { flexGrow = 1, minWidth = 0 } };
					gf.BindProperty(prop);
					SperlichFieldColumn.HideInternalLabel(gf);
					return gf;
				}

				case SerializedPropertyType.ObjectReference:
					return CreateObjectField(prop, declared, true);

				case SerializedPropertyType.LayerMask:
					return CreateLayerMaskDropdown(prop, acc);

				case SerializedPropertyType.Vector2:
				case SerializedPropertyType.Vector3:
				case SerializedPropertyType.Vector4:
				case SerializedPropertyType.Vector2Int:
				case SerializedPropertyType.Vector3Int:
					return CreateCompactVectorField(prop);

				case SerializedPropertyType.Generic when prop.hasVisibleChildren:
					return CompactGeneric(prop, acc, resolveType);
			}

			var pf = new PropertyField(prop, " ");
			SperlichFieldColumn.HideInternalLabel(pf);
			pf.style.flexGrow = 1;
			return pf;
		}

		private static VisualElement CompactGeneric(SerializedProperty prop, Color acc, Func<SerializedProperty, Type> resolveType) {
			var fold = new Foldout { text = prop.displayName, value = prop.isExpanded, style = { flexGrow = 1 } };
			fold.RegisterValueChangedCallback(e => prop.isExpanded = e.newValue);

			var body = new VisualElement { style = { marginLeft = 2, marginTop = 1 } };
			fold.Add(body);

			SerializedProperty child = prop.Copy();
			SerializedProperty end = prop.GetEndProperty();
			bool enter = true;
			while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end)) {
				enter = false;
				var line = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 1 } };
				line.Add(new Label(child.displayName) {
					style = { width = 92, flexShrink = 0, fontSize = 10, color = SperlichEditorTheme.TextMuted, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap }
				});
				VisualElement ctl = CompactControl(child.Copy(), acc, resolveType);
				ctl.style.flexGrow = 1;
				line.Add(ctl);
				body.Add(line);
			}
			return fold;
		}

		/// <summary>Vector2/3/4 (+ Int) as one inline row of compact drag fields (X Y [Z] [W]).</summary>
		public static VisualElement CreateCompactVectorField(SerializedProperty prop) {
			string[] caps;
			string[][] candidates;
			switch (prop.propertyType) {
				case SerializedPropertyType.Vector2:
					caps = new[] { "X", "Y" }; candidates = new[] { new[] { "x", "y" }, new[] { "m_X", "m_Y" } }; break;
				case SerializedPropertyType.Vector2Int:
					caps = new[] { "X", "Y" }; candidates = new[] { new[] { "m_X", "m_Y" }, new[] { "x", "y" } }; break;
				case SerializedPropertyType.Vector3:
					caps = new[] { "X", "Y", "Z" }; candidates = new[] { new[] { "x", "y", "z" }, new[] { "m_X", "m_Y", "m_Z" } }; break;
				case SerializedPropertyType.Vector3Int:
					caps = new[] { "X", "Y", "Z" }; candidates = new[] { new[] { "m_X", "m_Y", "m_Z" }, new[] { "x", "y", "z" } }; break;
				case SerializedPropertyType.Vector4:
					caps = new[] { "X", "Y", "Z", "W" }; candidates = new[] { new[] { "x", "y", "z", "w" } }; break;
				default:
					return CompactControl(prop);
			}

			SerializedProperty[] parts = null;
			foreach (string[] names in candidates) {
				var found = new SerializedProperty[names.Length];
				bool ok = true;
				for (int i = 0; i < names.Length; i++) {
					found[i] = prop.FindPropertyRelative(names[i]);
					if (found[i] == null) { ok = false; break; }
				}
				if (ok) { parts = found; break; }
			}
			if (parts == null) {
				var pf = new PropertyField(prop, " ");
				SperlichFieldColumn.HideInternalLabel(pf);
				return pf;
			}

			var cells = new VisualElement[caps.Length];
			for (int i = 0; i < caps.Length; i++) cells[i] = CreateCompactField(caps[i], parts[i]);
			return CreateFieldCluster(46, cells);
		}
	}
}
