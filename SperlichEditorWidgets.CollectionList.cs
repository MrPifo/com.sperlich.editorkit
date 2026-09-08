using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>
		/// Sperlich collection editor ("compact rows" style): a chevron section header with a count badge and
		/// a "+" button, thin alternating element rows each with a 6-dot reorder handle, an index and an "×"
		/// remove, and a "+ add" footer. Generic over the backing store — the caller supplies count / add /
		/// remove / move and fills each row via <paramref name="buildRow"/>. Returns the element plus a
		/// <c>rebuild</c> action the caller should call on external structural changes (Undo, etc.).
		/// </summary>
		/// <param name="onMove">(from, to) reorder; pass <c>null</c> to disable drag-reordering.</param>
		public static (VisualElement element, Action rebuild) CreateCollectionList(
			string title, string persistKey,
			Func<int> getCount, Action onAdd, Action<int> onRemove, Action<int, int> onMove,
			Action<int, VisualElement> buildRow,
			Color? accent = null, string emptyText = "Leer", string addText = "+ hinzufügen") {

			Color acc = accent ?? SperlichEditorTheme.ButtonAccent;

			var (header, body, _) = CreateChevronSection(title, true, SperlichEditorTheme.BgStep, SperlichEditorTheme.BgStepBody, persistKey);
			body.style.paddingLeft = 4;
			body.style.paddingRight = 4;
			body.style.paddingTop = 3;
			body.style.paddingBottom = 5;

			// Declared before the header buttons so the Rebuild local function (which reads them) is
			// definitely-assigned where those buttons capture it.
			var badge = MakeIndexBadge("0", false);
			badge.style.marginLeft = 8;
			var rowsHost = new VisualElement();

			header.Add(badge);
			header.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { flexGrow = 1 } });
			header.Add(HeaderIconButton("+", acc, () => { onAdd(); Rebuild(); }));
			body.Add(rowsHost);

			var footerBtn = MakeButton(addText, 0, () => { onAdd(); Rebuild(); }, isAccent: true);
			footerBtn.style.marginTop = 4;
			footerBtn.style.marginLeft = 2;
			body.Add(footerBtn);

			var wrap = new VisualElement { style = { marginBottom = 4 } };
			wrap.Add(header);
			wrap.Add(body);

			void Rebuild() {
				int n = getCount();
				badge.text = n.ToString();
				rowsHost.Clear();

				if (n == 0) {
					rowsHost.Add(new Label(emptyText) {
						style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, unityFontStyleAndWeight = FontStyle.Italic, marginLeft = 4, marginTop = 3, marginBottom = 3 }
					});
					return;
				}

				for (int i = 0; i < n; i++) rowsHost.Add(BuildElementRow(i, Rebuild));
			}

			VisualElement BuildElementRow(int index, Action rebuild) {
				var row = new VisualElement {
					style = {
						flexDirection = FlexDirection.Row, alignItems = Align.FlexStart,
						paddingLeft = 4, paddingRight = 4, paddingTop = 3, paddingBottom = 3,
						backgroundColor = index % 2 == 0 ? SperlichEditorTheme.BgDark : SperlichEditorTheme.BgStep,
						borderTopWidth = 2, borderTopColor = Color.clear,
					}
				};

				if (onMove != null) {
					VisualElement grip = CreateDragGrip();
					grip.style.marginTop = 3;
					grip.style.marginRight = 2;
					WireRowDrag(grip, row, index, rowsHost, onMove, rebuild);
					row.Add(grip);
				}

				row.Add(new Label(index.ToString()) {
					style = { width = 16, flexShrink = 0, marginTop = 3, fontSize = 9, color = SperlichEditorTheme.TextFaint, unityTextAlign = TextAnchor.MiddleLeft }
				});

				var content = new VisualElement { style = { flexGrow = 1, flexShrink = 1, minWidth = 0 } };
				buildRow(index, content);
				row.Add(content);

				int captured = index;
				var rm = new Label("×") {
					tooltip = "Entfernen",
					pickingMode = PickingMode.Position,
					style = { width = 16, flexShrink = 0, marginTop = 2, marginLeft = 2, fontSize = 12, unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextMuted }
				};
				SetHoverCursor(rm, MouseCursor.Link);
				rm.RegisterCallback<MouseEnterEvent>(_ => rm.style.color = SperlichEditorTheme.BadgeDangerBg);
				rm.RegisterCallback<MouseLeaveEvent>(_ => rm.style.color = SperlichEditorTheme.TextMuted);
				rm.RegisterCallback<ClickEvent>(evt => { evt.StopPropagation(); onRemove(captured); rebuild(); });
				row.Add(rm);

				return row;
			}

			Rebuild();
			return (wrap, Rebuild);
		}

		private static VisualElement HeaderIconButton(string glyph, Color accent, Action onClick) {
			var b = new VisualElement {
				pickingMode = PickingMode.Position,
				style = {
					width = 18, height = 18, marginRight = 4, flexShrink = 0,
					alignItems = Align.Center, justifyContent = Justify.Center,
					backgroundColor = SperlichEditorTheme.BgDark,
				}
			};
			SetRadius(b, 3);
			SetHoverCursor(b, MouseCursor.Link);
			b.Add(new Label(glyph) { pickingMode = PickingMode.Ignore, style = { fontSize = 13, color = accent, unityFontStyleAndWeight = FontStyle.Bold } });
			b.RegisterCallback<MouseEnterEvent>(_ => b.style.backgroundColor = SperlichEditorTheme.ButtonHoverBg);
			b.RegisterCallback<MouseLeaveEvent>(_ => b.style.backgroundColor = SperlichEditorTheme.BgDark);
			b.RegisterCallback<ClickEvent>(evt => { evt.StopPropagation(); onClick(); });
			return b;
		}

		private static void WireRowDrag(VisualElement grip, VisualElement row, int index, VisualElement rowsHost, Action<int, int> onMove, Action rebuild) {
			bool dragging = false;
			int target = index;

			void ClearIndicators() {
				foreach (VisualElement c in rowsHost.Children()) c.style.borderTopColor = Color.clear;
			}

			grip.RegisterCallback<PointerDownEvent>(e => {
				dragging = true;
				target = index;
				grip.CapturePointer(e.pointerId);
				row.style.opacity = 0.6f;
				e.StopPropagation();
			});
			grip.RegisterCallback<PointerMoveEvent>(e => {
				if (!dragging) return;
				float y = rowsHost.WorldToLocal((Vector2)e.position).y;
				int t = rowsHost.childCount;
				var kids = rowsHost.Children();
				int i = 0;
				foreach (VisualElement c in kids) {
					if (y < c.layout.yMin + c.layout.height * 0.5f) { t = i; break; }
					i++;
				}
				target = Mathf.Clamp(t, 0, rowsHost.childCount);
				ClearIndicators();
				if (target < rowsHost.childCount) rowsHost[target].style.borderTopColor = SperlichEditorTheme.ButtonAccent;
				else if (rowsHost.childCount > 0) rowsHost[rowsHost.childCount - 1].style.borderBottomColor = SperlichEditorTheme.ButtonAccent;
			});
			grip.RegisterCallback<PointerUpEvent>(e => {
				if (!dragging) return;
				dragging = false;
				grip.ReleasePointer(e.pointerId);
				row.style.opacity = 1f;
				ClearIndicators();
				int dst = target > index ? target - 1 : target;
				dst = Mathf.Clamp(dst, 0, rowsHost.childCount - 1);
				if (dst != index) { onMove(index, dst); rebuild(); }
			});
			grip.RegisterCallback<PointerCaptureOutEvent>(_ => { dragging = false; row.style.opacity = 1f; ClearIndicators(); });
		}
	}
}
