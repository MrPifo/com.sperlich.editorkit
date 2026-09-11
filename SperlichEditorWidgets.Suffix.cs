using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>The control cell of a <see cref="SperlichFieldColumn.Row"/> (so suffixes / inline buttons
		/// land next to the field, not inside the fixed-width label cell). Falls back to the old row[1] guess
		/// for rows not built by <see cref="SperlichFieldColumn"/>.</summary>
		private static VisualElement ControlHost(VisualElement row) {
			VisualElement cell = row.Q(SperlichFieldColumn.ControlCellName);
			if (cell != null) return cell;
			return row.childCount > 1 ? row[1] : row;
		}

		/// <summary>Disables a whole row and dims it — the visual for <c>[SReadOnly]</c> and a read-only
		/// <c>[Box]</c> body.</summary>
		public static void MarkReadOnly(VisualElement row) {
			if (row == null) return;
			row.SetEnabled(false);
			row.style.opacity = 0.75f;
		}

		/// <summary>Appends (or overlays) a small grey suffix label to a field row built by
		/// <see cref="SperlichFieldColumn.Row"/>. When <paramref name="pollMs"/> &gt; 0 the text is refreshed
		/// on that interval (value-diffed), otherwise it is set once.</summary>
		public static void AttachSuffixLabel(VisualElement row, Func<string> getText, bool overlay, int pollMs = 0) {
			if (row == null || getText == null) return;
			VisualElement host = ControlHost(row);

			var label = new Label(getText() ?? string.Empty) {
				pickingMode = PickingMode.Ignore,
				style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, flexShrink = 0, unityTextAlign = TextAnchor.MiddleLeft },
			};

			if (overlay) {
				host.style.position = Position.Relative;
				label.style.position = Position.Absolute;
				label.style.right = 6;
				label.style.top = 0;
				label.style.bottom = 0;
			} else {
				label.style.marginLeft = 5;
			}
			host.Add(label);

			if (pollMs > 0) {
				label.schedule.Execute(() => {
					string t = getText() ?? string.Empty;
					if (t != label.text) label.text = t;
				}).Every(pollMs);
			}
		}

		/// <summary>Appends chained inline buttons to the right of a field row (<c>[InlineButton]</c>). The
		/// caller supplies the click actions (multi-object invoke lives in the engine).</summary>
		public static void AttachInlineButtons(VisualElement row, IList<(string label, string icon, Action click)> buttons) {
			if (row == null || buttons == null || buttons.Count == 0) return;
			VisualElement host = ControlHost(row);

			foreach ((string label, string icon, Action click) in buttons) {
				Action onClick = click;
				var btn = new Button(() => onClick?.Invoke()) {
					style = { height = 18, marginLeft = 4, marginTop = 0, marginBottom = 0, flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center },
				};
				ApplyNeonButtonStyle(btn);
				if (!string.IsNullOrEmpty(icon)) {
					Texture tex = EditorGUIUtility.IconContent(icon)?.image;
					if (tex != null) btn.Add(new Image { image = tex, pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit, style = { width = 13, height = 13, marginRight = string.IsNullOrEmpty(label) ? 0 : 3, flexShrink = 0 } });
				}
				if (!string.IsNullOrEmpty(label)) {
					btn.Add(new Label(label) { pickingMode = PickingMode.Ignore, style = { fontSize = 10, color = SperlichEditorTheme.TextPrimary } });
				}
				host.Add(btn);
			}
		}
	}
}
