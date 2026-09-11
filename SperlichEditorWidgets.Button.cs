using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Preset-sized button for <c>[SButton]</c>. <paramref name="size"/> picks the width
		/// (<see cref="ButtonSize.Full"/> stretches), <paramref name="height"/> the height, and
		/// <paramref name="anchor"/> the horizontal placement when the button does not stretch. Returns a
		/// full-width row wrapper containing the button so anchoring works.</summary>
		public static VisualElement MakeButton(string text, ButtonSize size, ButtonSize height, ButtonAnchor anchor,
			Action onClick, bool isAccent = false, string icon = null) {

			var btn = new Button(onClick);
			btn.style.height = HeightFor(height);
			btn.style.marginLeft = 0;
			btn.style.marginRight = 0;
			btn.style.marginTop = 1;
			btn.style.marginBottom = 1;
			btn.style.flexDirection = FlexDirection.Row;
			btn.style.alignItems = Align.Center;
			btn.style.justifyContent = Justify.Center;
			ApplyNeonButtonStyle(btn, isAccent);

			if (!string.IsNullOrEmpty(icon)) {
				Texture iconTex = EditorGUIUtility.IconContent(icon)?.image;
				if (iconTex != null) {
					btn.Add(new Image { image = iconTex, scaleMode = ScaleMode.ScaleToFit, style = { width = 14, height = 14, marginRight = text != null ? 4 : 0, flexShrink = 0 } });
				}
			}
			if (!string.IsNullOrEmpty(text)) {
				btn.Add(new Label(text) { pickingMode = PickingMode.Ignore, style = { color = SperlichEditorTheme.TextPrimary } });
			}

			if (size == ButtonSize.Full) {
				btn.style.width = Length.Percent(100f);
				var fullRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 1, marginBottom = 1 } };
				btn.style.flexGrow = 1;
				fullRow.Add(btn);
				return fullRow;
			}

			btn.style.width = WidthFor(size);
			var row = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row, marginTop = 1, marginBottom = 1,
					justifyContent = anchor == ButtonAnchor.Left ? Justify.FlexStart
						: anchor == ButtonAnchor.Right ? Justify.FlexEnd : Justify.Center,
				}
			};
			row.Add(btn);
			return row;
		}

		private static float WidthFor(ButtonSize size) => size switch {
			ButtonSize.Small => 90f,
			ButtonSize.Large => 220f,
			ButtonSize.Full => 220f,
			_ => 140f,
		};

		private static float HeightFor(ButtonSize height) => height switch {
			ButtonSize.Small => 16f,
			ButtonSize.Large => 28f,
			ButtonSize.Full => 28f,
			_ => 20f,
		};

		/// <summary>Horizontal segmented bar where every segment fires a method (no bound property) — the
		/// visual pendant to <see cref="CreateFlagButtons"/>, used for <c>[ButtonGroup]</c>.</summary>
		public static VisualElement CreateMethodButtonGroup(IList<(string label, string icon, Action click)> items, Color accent) {
			var bar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2, marginBottom = 2 } };
			if (items == null || items.Count == 0) return bar;

			for (int i = 0; i < items.Count; i++) {
				(string label, string icon, Action click) = items[i];
				Action onClick = click;

				var seg = new VisualElement { pickingMode = PickingMode.Position };
				seg.style.flexGrow = 1;
				seg.style.flexDirection = FlexDirection.Row;
				seg.style.alignItems = Align.Center;
				seg.style.justifyContent = Justify.Center;
				seg.style.height = 20;
				seg.style.paddingLeft = 6;
				seg.style.paddingRight = 6;
				seg.style.marginRight = i < items.Count - 1 ? 3 : 0;
				seg.style.borderTopWidth = 1;
				seg.style.borderBottomWidth = 1;
				seg.style.borderLeftWidth = 1;
				seg.style.borderRightWidth = 1;
				seg.style.backgroundColor = SperlichEditorTheme.ButtonBg;
				SetBorderColor(seg, SperlichEditorTheme.ButtonBorder);
				SetRadius(seg, 3);
				SetHoverCursor(seg, MouseCursor.Link);
				ApplyColorTransition(seg, 100, "background-color", "border-color");

				if (!string.IsNullOrEmpty(icon)) {
					Texture iconTex = EditorGUIUtility.IconContent(icon)?.image;
					if (iconTex != null) seg.Add(new Image { image = iconTex, pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit, style = { width = 13, height = 13, marginRight = string.IsNullOrEmpty(label) ? 0 : 4, flexShrink = 0 } });
				}
				if (!string.IsNullOrEmpty(label)) {
					seg.Add(new Label(label) { pickingMode = PickingMode.Ignore, style = { fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextSecondary } });
				}

				seg.RegisterCallback<MouseEnterEvent>(_ => {
					seg.style.backgroundColor = SperlichEditorTheme.ButtonHoverBg;
					SetBorderColor(seg, accent);
				});
				seg.RegisterCallback<MouseLeaveEvent>(_ => {
					seg.style.backgroundColor = SperlichEditorTheme.ButtonBg;
					SetBorderColor(seg, SperlichEditorTheme.ButtonBorder);
				});
				seg.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());
				ApplyHoverJuice(seg, "background-color", "border-color");

				bar.Add(seg);
			}
			return bar;
		}
	}
}
