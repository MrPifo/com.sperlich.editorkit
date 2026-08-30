using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Hält die Scroll-Position des Inspector-Fensters über einen Rebuild von <c>CreateInspectorGUI</c>
	/// hinweg fest. Unity baut UI-Toolkit-Custom-Inspektoren bei Undo/Redo (und beim Ändern von
	/// Listen-Längen) komplett neu auf und setzt dabei die umschließende <see cref="ScrollView"/> auf 0
	/// zurück — der Inspector "springt nach oben". Diese Klasse merkt sich die letzte Position pro Ziel
	/// und stellt sie nach dem nächsten Aufbau wieder her.
	/// </summary>
	public static class SperlichInspectorScroll {

		private static readonly Dictionary<int, Vector2> Saved = new();

		/// <summary>In <c>CreateInspectorGUI</c> mit dem zurückgegebenen Root und dem <c>target</c> aufrufen.</summary>
		public static void Preserve(VisualElement root, Object target) {
			if (root == null || target == null) return;
			int id = target.GetInstanceID();

			root.RegisterCallback<AttachToPanelEvent>(_ => {
				ScrollView sv = FindAncestorScrollView(root);
				if (sv == null) return;

				bool restoring = false;

				// Nutzer-Scroll mitschreiben (nur so lange nicht gerade wiederhergestellt wird). Handler ist
				// an die Lebensdauer DIESES Aufbaus gebunden — beim nächsten Rebuild sauber wieder abgemeldet.
				Action<float> onScroll = _ => { if (!restoring) Saved[id] = sv.scrollOffset; };
				sv.verticalScroller.valueChanged += onScroll;
				root.RegisterCallback<DetachFromPanelEvent>(__ => sv.verticalScroller.valueChanged -= onScroll);

				if (Saved.TryGetValue(id, out Vector2 off) && off.y > 0.5f) {
					restoring = true;
					void DoRestore() => sv.scrollOffset = off;
					// mehrfach, da Höhe/Layout erst über ein paar Frames stehen
					sv.schedule.Execute(DoRestore).ExecuteLater(1);
					sv.schedule.Execute(DoRestore).ExecuteLater(20);
					sv.schedule.Execute(DoRestore).ExecuteLater(60);
					sv.schedule.Execute(() => restoring = false).ExecuteLater(120);
				}
			});
		}

		private static ScrollView FindAncestorScrollView(VisualElement from) {
			for (VisualElement p = from.hierarchy.parent; p != null; p = p.hierarchy.parent) {
				if (p is ScrollView sv) return sv;
			}
			return null;
		}
	}
}
