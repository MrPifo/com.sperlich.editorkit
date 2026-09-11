using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Reproduces Unity's prefab-override affordances for a row whose control is a hand-built Sperlich
	/// widget (drag-number field, pill toggle, flat dropdown, object field, collection card, …) rather than
	/// a native <c>PropertyField</c> — those custom widgets don't get the binding system's blue bar / bold
	/// label / Apply-Revert menu automatically. Kept as one call so every Sperlich row behaves like a
	/// native one under prefab editing.
	/// </summary>
	public static class SperlichPrefabOverride {

		private static readonly Color BarColor = new Color(0.06f, 0.5f, 0.75f);

		/// <summary>Marks a row that owns its own Apply/Revert menu. An outer row's menu handler bails when
		/// the right-click landed inside a nested scope, so a per-element revert can't also offer to
		/// revert the whole collection.</summary>
		private const string OverrideScopeClass = "sperlich-override-scope";

		public static void Attach(VisualElement row, Label label, SerializedProperty property, int barLeft = -6) {
			SerializedProperty prop = property.Copy();
			SerializedObject so = prop.serializedObject;

			// Absolute-positioned bar in the left gutter: no effect on the row's own layout, so these rows
			// stay aligned with the PropertyField-fallback rows that get Unity's own bar.
			row.style.position = Position.Relative;

			var bar = new VisualElement {
				pickingMode = PickingMode.Ignore,
				style = {
					position = Position.Absolute, left = barLeft, top = 1, bottom = 1, width = 2,
					backgroundColor = BarColor, display = DisplayStyle.None,
				}
			};
			// Appended last (not SendToBack): it sits entirely in the left gutter and ignores picking, so
			// paint order is irrelevant — and keeping it last means it never shifts the row's real child
			// indices, which row post-processors rely on.
			row.Add(bar);

			bool IsOverridden() {
				try { so.UpdateIfRequiredOrScript(); } catch { return false; /* object destroyed */ }
				if (prop.prefabOverride && !prop.isDefaultOverride) return true;
				// Container properties (managed reference, nested struct, custom-drawer type like SEvent):
				// the override flag usually sits on a changed leaf, not the container — scan the subtree.
				if (prop.hasChildren && (prop.propertyType == SerializedPropertyType.ManagedReference
				                         || prop.propertyType == SerializedPropertyType.Generic)) {
					try {
						SerializedProperty p = prop.Copy();
						SerializedProperty end = prop.GetEndProperty();
						int guard = 0;
						bool enter = true;
						while (p.NextVisible(enter) && !SerializedProperty.EqualContents(p, end) && guard++ < 512) {
							enter = true;
							if (p.prefabOverride && !p.isDefaultOverride) return true;
						}
					} catch { /* iteration raced a structural change */ }
				}
				return false;
			}

			void Refresh() {
				bool overridden = IsOverridden();
				bar.style.display = overridden ? DisplayStyle.Flex : DisplayStyle.None;
				if (label != null) label.style.unityFontStyleAndWeight = overridden ? FontStyle.Bold : FontStyle.Normal;
			}

			// A PropertyField (directly, or wrapped one level deep for the override bar) re-syncs itself from
			// the SerializedObject and would lose its managed-reference subtree on an explicit re-Bind — so
			// only re-bind hand-built control rows.
			bool hostsPropertyField = row is PropertyField
				|| (row.childCount > 0 && row[0] is PropertyField);

			void AfterChange() {
				try {
					so.Update();                       // pull the reverted / applied value back into the SO
					if (!hostsPropertyField) row.Bind(so);
				} catch { /* object destroyed */ }
				Refresh();
			}

			// Populate for ANY context menu raised on this row or a descendant (an ObjectField / collection
			// card opens its own menu and swallows the trigger event — but the populate event still trickles
			// down through the row, so we prepend our items here in the capture phase).
			row.RegisterCallback<ContextualMenuPopulateEvent>(evt => {
				if (!IsOverridden()) return;

				// A nested row with its own Apply/Revert scope owns this right-click — don't also offer the
				// whole-collection / whole-object revert on top of the per-element one.
				if (evt.target is VisualElement clicked && clicked != row) {
					for (VisualElement s = clicked; s != null && s != row; s = s.hierarchy.parent) {
						if (s.ClassListContains(OverrideScopeClass)) return;
					}
				}

				UnityEngine.Object target = so.targetObject;
				string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
				string prefabName = string.IsNullOrEmpty(prefabPath) ? "Prefab" : System.IO.Path.GetFileNameWithoutExtension(prefabPath);

				evt.menu.AppendAction($"Apply to Prefab '{prefabName}'", _ => {
					if (string.IsNullOrEmpty(prefabPath)) return;
					PrefabUtility.ApplyPropertyOverride(prop, prefabPath, InteractionMode.UserAction);
					AfterChange();
				}, DropdownMenuAction.AlwaysEnabled);

				evt.menu.AppendAction("Revert", _ => {
					PrefabUtility.RevertPropertyOverride(prop, InteractionMode.UserAction);
					AfterChange();
				}, DropdownMenuAction.AlwaysEnabled);

				evt.menu.AppendSeparator();
			}, TrickleDown.TrickleDown);

			// A bare manipulator so rows whose control has no context menu of its own (drag-number field,
			// pill toggle, …) still show one on right-click, which then fires the populate callback above.
			row.AddManipulator(new ContextualMenuManipulator(_ => { }));

			row.AddToClassList(OverrideScopeClass);
			row.RegisterCallback<AttachToPanelEvent>(_ => Refresh());
			row.TrackPropertyValue(prop, _ => Refresh());
			// Safety net for changes that don't fire TrackPropertyValue (external Revert, Undo, Apply from
			// the component header). schedule pauses automatically while the inspector is closed.
			row.schedule.Execute(Refresh).Every(400);
			Refresh();
		}
	}
}
