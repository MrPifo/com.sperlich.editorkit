using System;
using System.Collections.Generic;
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
			Color? accent = null, string emptyText = "Empty", string addText = "+ Add") {

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
				Color rowBg = index % 2 == 0 ? SperlichEditorTheme.BgDark : SperlichEditorTheme.BgStep;
				var row = new VisualElement {
					style = {
						flexDirection = FlexDirection.Row, alignItems = Align.FlexStart,
						paddingLeft = 4, paddingRight = 4, paddingTop = 3, paddingBottom = 3,
						backgroundColor = rowBg,
						borderTopWidth = 2, borderTopColor = Color.clear,
					}
				};

				// Hover-Feedback: Zeile hellt kurz auf. MouseEnter/Leave (nicht Over/Out) feuern nur an der
				// Zeilengrenze, nicht bei jedem Wechsel zwischen den Kind-Controls.
				SetEaseTransition(row, 90, "background-color");
				row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = SperlichEditorTheme.ButtonBg);
				row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = rowBg);

				if (onMove != null) {
					VisualElement grip = CreateDragGrip();
					grip.style.marginTop = 3;
					grip.style.marginRight = 2;
					WireRowDrag(grip, row, index, rowsHost, onMove, rebuild, acc, rowBg);
					row.Add(grip);
				}

				row.Add(new Label(index.ToString()) {
					pickingMode = PickingMode.Ignore,
					style = {
						minWidth = 16, flexShrink = 0, marginTop = 2, marginLeft = 1, marginRight = 4,
						fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold,
						color = SperlichEditorTheme.TextMuted, unityTextAlign = TextAnchor.MiddleRight,
					}
				});

				var content = new VisualElement { style = { flexGrow = 1, flexShrink = 1, minWidth = 0 } };
				buildRow(index, content);
				row.Add(content);

				int captured = index;
				var rm = new Label("×") {
					tooltip = "Remove",
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

		/// <summary>
		/// Verdrahtet Drag-&amp;-Drop-Reordering für eine Listenzeile.
		/// <para>
		/// Der Griff fängt den Pointer. Die gezogene Zeile wird aus dem Flex-Fluss gelöst
		/// (<c>position: absolute</c>), leicht angehoben dargestellt und folgt dem Cursor. An ihrer alten
		/// Stelle bleibt eine getönte Platzhalter-Lücke offen — die übrigen Zeilen bewegen sich <b>nicht</b>.
		/// Eine Akzentlinie am oberen (bzw. unteren) Rand markiert, wo die Zeile landen würde. Erst beim
		/// Loslassen wird <paramref name="onMove"/> aufgerufen und die Liste neu sortiert; ohne Ziel-Wechsel
		/// gleitet die Zeile zurück in ihre Lücke.
		/// </para>
		/// </summary>
		private static void WireRowDrag(VisualElement grip, VisualElement row, int fromIndex, VisualElement rowsHost, Action<int, int> onMove, Action rebuild, Color accent, Color rowBg) {
			const int LiftMs = 110;    // Anheben / Absetzen der gezogenen Zeile
			const int SettleMs = 130;  // Zurückgleiten in die Lücke bei Abbruch

			SetHoverCursor(grip, MouseCursor.MoveArrow);

			bool dragging = false;
			int targetIndex = fromIndex;
			float pointerStartY = 0f;
			float startTop = 0f;
			float rowH = 0f;
			Color restBg = default;
			List<VisualElement> others = null;   // alle Zeilen außer der gezogenen, stabile Reihenfolge
			VisualElement placeholder = null;
			VisualElement dropLine = null;       // eigenes Element, liegt über der schwebenden Zeile → bleibt sichtbar

			// Drop-Linie an die Stelle setzen, an der die Zeile bei Drop einsortiert würde.
			void ShowIndicator(int t) {
				if (dropLine == null || others == null || others.Count == 0) return;
				float y = t < others.Count ? others[t].layout.yMin : others[others.Count - 1].layout.yMax;
				dropLine.style.top = y - 1.5f;
			}

			void ClearInline(VisualElement el) {
				el.style.scale = StyleKeyword.Null;
				el.style.opacity = StyleKeyword.Null;
				el.style.top = StyleKeyword.Null;
				el.style.left = StyleKeyword.Null;
				el.style.right = StyleKeyword.Null;
				el.style.position = StyleKeyword.Null;
				el.style.backgroundColor = restBg;
				SetEaseTransition(el, 90, "background-color");  // Hover-Transition wiederherstellen
			}

			void Finish(bool commit) {
				int from = fromIndex, to = targetIndex;
				dropLine?.RemoveFromHierarchy();
				dropLine = null;
				placeholder?.RemoveFromHierarchy();
				placeholder = null;

				if (commit && to != from) {
					// Nur jetzt sortieren sich die Zeilen um — während des Ziehens stand alles still.
					onMove(from, to);
					rebuild();
					if (to >= 0 && to < rowsHost.childCount) {
						VisualElement landed = rowsHost[to];
						landed.style.scale = new Scale(Vector3.one * 1.03f);
						landed.schedule.Execute(() => {
							SetEaseTransition(landed, LiftMs, "scale");
							landed.style.scale = new Scale(Vector3.one);
						}).ExecuteLater(16);
						landed.schedule.Execute(() => {
							landed.style.scale = StyleKeyword.Null;
							landed.style.transitionProperty = StyleKeyword.Null;
							landed.style.transitionDuration = StyleKeyword.Null;
							landed.style.transitionTimingFunction = StyleKeyword.Null;
						}).ExecuteLater(16 + LiftMs + 40);
					}
					return;
				}

				// Abbruch oder Drop am Ursprung: Zeile zurück in ihre Lücke gleiten lassen.
				SetEaseTransition(row, SettleMs, "top", "scale", "opacity", "background-color");
				row.style.top = startTop;
				row.style.scale = new Scale(Vector3.one);
				row.style.opacity = 1f;
				row.style.backgroundColor = restBg;
				rowsHost.schedule.Execute(() => {
					// BringToFront() hat die Zeile ans Ende der Kinderliste geschoben — Reihenfolge wiederherstellen.
					if (row.parent == rowsHost) rowsHost.Insert(Mathf.Min(from, rowsHost.childCount), row);
					ClearInline(row);
				}).ExecuteLater(SettleMs + 40);
			}

			grip.RegisterCallback<PointerDownEvent>(e => {
				if (e.button != 0 || dragging) return;
				var kids = new List<VisualElement>(rowsHost.Children());
				if (fromIndex >= kids.Count) return;

				others = new List<VisualElement>(kids.Count);
				foreach (VisualElement k in kids) if (k != row) others.Add(k);

				rowH = row.layout.height;
				if (rowH < 1f) rowH = 20f;
				startTop = row.layout.yMin;
				pointerStartY = e.position.y;
				restBg = rowBg;
				dragging = true;
				targetIndex = fromIndex;

				grip.CapturePointer(e.pointerId);

				// Platzhalter hält die Lücke offen, solange gezogen wird.
				placeholder = new VisualElement {
					style = {
						height = rowH, flexShrink = 0,
						backgroundColor = new Color(accent.r, accent.g, accent.b, 0.06f),
						borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
					}
				};
				SetBorderColor(placeholder, new Color(accent.r, accent.g, accent.b, 0.35f));
				rowsHost.Insert(fromIndex, placeholder);

				row.style.position = Position.Absolute;
				row.style.left = 0;
				row.style.right = 0;
				row.style.top = startTop;
				row.BringToFront();

				SetEaseTransition(row, LiftMs, "scale", "opacity", "background-color");  // "top" bewusst ohne Transition → folgt dem Cursor
				row.style.scale = new Scale(Vector3.one * 1.02f);
				row.style.opacity = 0.9f;
				row.style.backgroundColor = SperlichEditorTheme.ButtonHoverBg;

				// Zuletzt einfügen → paint-order über der schwebenden Zeile, Linie bleibt sichtbar.
				dropLine = new VisualElement {
					pickingMode = PickingMode.Ignore,
					style = { position = Position.Absolute, left = 1, right = 1, height = 3, backgroundColor = accent }
				};
				SetRadius(dropLine, 1.5f);
				var knob = new VisualElement {
					pickingMode = PickingMode.Ignore,
					style = { position = Position.Absolute, left = -3, top = -3, width = 9, height = 9, backgroundColor = accent }
				};
				SetRadius(knob, 4.5f);
				dropLine.Add(knob);
				rowsHost.Add(dropLine);

				ShowIndicator(fromIndex);
				e.StopPropagation();
			});

			grip.RegisterCallback<PointerMoveEvent>(e => {
				if (!dragging) return;

				float rawTop = startTop + (e.position.y - pointerStartY);
				float maxTop = Mathf.Max(0f, rowsHost.layout.height - rowH * 0.5f);
				row.style.top = Mathf.Clamp(rawTop, -rowH * 0.5f, maxTop);

				float localY = rowsHost.WorldToLocal((Vector2)e.position).y;
				int t = 0;
				foreach (VisualElement o in others) if (localY > o.layout.center.y) t++;
				t = Mathf.Clamp(t, 0, others.Count);
				if (t != targetIndex) {
					targetIndex = t;
					ShowIndicator(t);
				}
			});

			grip.RegisterCallback<PointerUpEvent>(e => {
				if (!dragging) return;
				dragging = false;
				grip.ReleasePointer(e.pointerId);
				Finish(commit: true);
				e.StopPropagation();
			});

			grip.RegisterCallback<PointerCaptureOutEvent>(_ => {
				if (!dragging) return;
				dragging = false;
				Finish(commit: false);
			});
		}

		/// <summary>Registriert eine kurze Ease-Out-USS-Transition für die genannten animierbaren Properties
		/// (UI Toolkit ersetzt die Transition-Liste bei jeder Zuweisung komplett, daher immer alle nötigen
		/// Properties in einem Aufruf übergeben).</summary>
		private static void SetEaseTransition(VisualElement el, int durationMs, params string[] properties) {
			var props = new List<StylePropertyName>(properties.Length);
			foreach (string p in properties) props.Add(new StylePropertyName(p));
			el.style.transitionProperty = props;
			el.style.transitionDuration = new List<TimeValue> { new TimeValue(durationMs, TimeUnit.Millisecond) };
			el.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) };
		}
	}
}
