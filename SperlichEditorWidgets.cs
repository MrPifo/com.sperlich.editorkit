using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>Wiederverwendbare UI-Toolkit-Bausteine im Sperlich-Editor-Stil, gemeinsam für alle Sperlich-Package-Inspektoren.</summary>
	public static partial class SperlichEditorWidgets {

		// Das interne "defaultCursorId"-Feld heißt je nach Unity-Version leicht anders — daher wird stattdessen
		// nach dem einzigen privaten int-Feld des Cursor-Structs gesucht (neben Texture2D texture und Vector2 hotspot
		// gibt es nur dieses eine), damit systemeigene Cursor (Hand, Resize, ...) ohne Custom-Textur gesetzt werden können.
		private static readonly FieldInfo CursorIdField = FindCursorIdField();

		private static FieldInfo FindCursorIdField() {
			foreach (var f in typeof(UnityEngine.UIElements.Cursor).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (f.FieldType == typeof(int)) return f;
			}
			return null;
		}

		/// <summary>Setzt einen systemeigenen Editor-Cursor (z.B. Hand/Link beim Hover über klickbare Elemente) über Reflection, da UI Toolkit dafür keine öffentliche API bietet. No-op, falls das interne Feld in einer Unity-Version nicht existiert.</summary>
		public static void SetHoverCursor(VisualElement element, MouseCursor cursor) {
			if (CursorIdField == null) return;
			try {
				// Wichtig: Cursor ist ein Struct. Muss vor dem SetValue-Aufruf explizit geboxt werden,
				// sonst landet die Änderung auf einer wertlosen Kopie statt auf dem Objekt, das wir weiterverwenden.
				object boxedCursor = new UnityEngine.UIElements.Cursor();
				CursorIdField.SetValue(boxedCursor, (int)cursor);
				element.style.cursor = new StyleCursor((UnityEngine.UIElements.Cursor)boxedCursor);
			} catch {
				// Best effort — falsche Feld-Signatur in dieser Unity-Version, Cursor bleibt Standard.
			}
		}

		public static VisualElement CreateBox(int radius, Color borderColor) {
			var box = new VisualElement();
			box.style.borderTopWidth = 1;
			box.style.borderBottomWidth = 1;
			box.style.borderLeftWidth = 1;
			box.style.borderRightWidth = 1;
			SetBorderColor(box, borderColor);
			SetRadius(box, radius);
			box.style.overflow = Overflow.Hidden;
			return box;
		}

		public static VisualElement Spacer(int height) => new VisualElement { style = { height = height } };

		/// <summary>Registriert eine kurze USS-Transition für die genannten Style-Properties (z.B. "background-color", "border-color", "left") — sorgt dafür, dass Hover-/State-Wechsel weich überblenden statt zu springen.</summary>
		public static void ApplyColorTransition(VisualElement element, int durationMs, params string[] properties) {
			var props = new List<StylePropertyName>();
			foreach (var p in properties) props.Add(new StylePropertyName(p));
			element.style.transitionProperty = props;
			element.style.transitionDuration = new List<TimeValue> { new TimeValue(durationMs, TimeUnit.Millisecond) };
			element.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) };
		}

		public static void ApplyNeonButtonStyle(VisualElement btn, bool isAccent = false) {
			btn.style.backgroundColor = SperlichEditorTheme.ButtonBg;
			btn.style.color = SperlichEditorTheme.TextPrimary;
			btn.style.borderTopWidth = 1;
			btn.style.borderBottomWidth = 1;
			btn.style.borderLeftWidth = 1;
			btn.style.borderRightWidth = 1;
			SetBorderColor(btn, SperlichEditorTheme.ButtonBorder);
			SetRadius(btn, 3);
			SetHoverCursor(btn, MouseCursor.Link);

			btn.RegisterCallback<MouseOverEvent>(_ => {
				btn.style.backgroundColor = SperlichEditorTheme.ButtonHoverBg;
				if (isAccent) SetBorderColor(btn, SperlichEditorTheme.ButtonAccent);
			});
			btn.RegisterCallback<MouseOutEvent>(_ => {
				btn.style.backgroundColor = SperlichEditorTheme.ButtonBg;
				SetBorderColor(btn, SperlichEditorTheme.ButtonBorder);
			});
		}

		public static Button MakeButton(string text, int width, Action onClick, bool isAccent = false) {
			var btn = new Button(onClick) { text = text };
			if (width > 0) btn.style.width = width;
			btn.style.height = 20;
			ApplyNeonButtonStyle(btn, isAccent);
			return btn;
		}

		public static Label CreateBadge(string text, Color bg, Color? textColor = null) {
			var badge = new Label(text);
			badge.style.fontSize = 9;
			badge.style.unityFontStyleAndWeight = FontStyle.Bold;
			badge.style.color = textColor ?? SperlichEditorTheme.TextPrimary;
			badge.style.backgroundColor = bg;
			badge.style.paddingTop = 2;
			badge.style.paddingBottom = 2;
			badge.style.paddingLeft = 6;
			badge.style.paddingRight = 6;
			SetRadius(badge, 3);
			return badge;
		}

		/// <summary>Kollabierbare Sektion mit handgezeichnetem ▼/▶-Pfeil statt nativem Foldout (Sperlich-Editor-Konvention, siehe AnimSequencerEditor).</summary>
		/// <param name="persistKey">Wenn gesetzt: der Auf-/Zu-Zustand wird unter diesem Schlüssel (+ Titel) in
		/// <see cref="EditorPrefs"/> gemerkt, sodass er einen Inspector-Rebuild (Undo/Redo, Domain-Reload) übersteht.</param>
		public static (VisualElement header, VisualElement body, Label arrow) CreateChevronSection(string title, bool expanded, Color headerBg, Color? bodyBg = null, string persistKey = null) {
			string prefKey = persistKey != null ? "Sperlich.Section/" + persistKey + "/" + title : null;
			if (prefKey != null) expanded = EditorPrefs.GetBool(prefKey, expanded);

			var header = new VisualElement { pickingMode = PickingMode.Position };
			header.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
			header.style.alignItems = Align.Center;
			header.style.backgroundColor = headerBg;
			header.style.paddingLeft = 6;
			header.style.paddingTop = 4;
			header.style.paddingBottom = 4;
			ApplyColorTransition(header, 100, "background-color");
			SetHoverCursor(header, MouseCursor.Link);
			Color headerHoverBg = Color.Lerp(headerBg, Color.white, 0.08f);
			header.RegisterCallback<MouseEnterEvent>(_ => header.style.backgroundColor = headerHoverBg);
			header.RegisterCallback<MouseLeaveEvent>(_ => header.style.backgroundColor = headerBg);

			var arrow = new Label(expanded ? "▼" : "▶");
			arrow.style.fontSize = 9;
			arrow.style.width = 12;
			arrow.style.color = SperlichEditorTheme.TextMuted;
			header.Add(arrow);

			var titleLabel = new Label(title);
			titleLabel.style.fontSize = 10;
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			titleLabel.style.color = SperlichEditorTheme.TextSecondary;
			header.Add(titleLabel);

			var body = new VisualElement { style = { display = expanded ? DisplayStyle.Flex : DisplayStyle.None, backgroundColor = bodyBg ?? SperlichEditorTheme.BgPanel } };

			header.RegisterCallback<ClickEvent>(_ => {
				bool nowExpanded = body.style.display == DisplayStyle.None;
				body.style.display = nowExpanded ? DisplayStyle.Flex : DisplayStyle.None;
				arrow.text = nowExpanded ? "▼" : "▶";
				if (prefKey != null) EditorPrefs.SetBool(prefKey, nowExpanded);
			});

			return (header, body, arrow);
		}

		/// <summary>Label+Control-Zeile mit fester Label-Spaltenbreite. Bewusst OHNE Unitys "unity-base-field__aligned"-Klasse: deren automatische Ausrichtung vermisst das interne Label eines Feldes und bricht, sobald ein Feld für den flachen Sperlich-Look umgebaut wurde.</summary>
		public static VisualElement CreateAlignedRow(string label, VisualElement control) {
			var row = new VisualElement { style = { minHeight = 22, flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };

			var lbl = new Label(label);
			lbl.style.width = new Length(45, LengthUnit.Percent);
			lbl.style.minWidth = 127;
			lbl.style.paddingLeft = 3;
			lbl.style.color = SperlichEditorTheme.TextSecondary;
			row.Add(lbl);

			var inputContainer = new VisualElement { style = { flexGrow = 1, flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };
			inputContainer.Add(control);
			row.Add(inputContainer);
			return row;
		}

		/// <summary>Anklickbare Segmented-Control für ein Enum-SerializedProperty — flache umrandete Segmente statt Dropdown, aktives Segment mit Akzent-Rahmen (Sperlich-Editor-Konvention).</summary>
		public static VisualElement CreateSegmentedControl(SerializedProperty enumProp, string[] labels, Color accent, Action onChanged = null) {
			var track = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, marginBottom = 6 } };
			var segments = new List<VisualElement>();
			var hovered = new bool[labels.Length];

			void Refresh() {
				int current = enumProp.enumValueIndex;
				for (int i = 0; i < segments.Count; i++) {
					bool active = i == current;
					bool isHovered = hovered[i];
					var seg = segments[i];
					SetBorderColor(seg, active ? accent : (isHovered ? SperlichEditorTheme.BorderStrong : SperlichEditorTheme.BorderSubtle));
					if (active) {
						seg.style.backgroundColor = new Color(accent.r, accent.g, accent.b, isHovered ? 0.18f : 0.12f);
					} else {
						seg.style.backgroundColor = isHovered ? new Color(1f, 1f, 1f, 0.05f) : Color.clear;
					}
					var label = (Label)seg[0];
					label.style.color = active ? accent : (isHovered ? SperlichEditorTheme.TextSecondary : SperlichEditorTheme.TextMuted);
					label.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
				}
			}

			for (int i = 0; i < labels.Length; i++) {
				int index = i;
				var segment = new VisualElement { pickingMode = PickingMode.Position };
				segment.style.flexGrow = 1;
				segment.style.borderTopWidth = 1;
				segment.style.borderBottomWidth = 1;
				segment.style.borderLeftWidth = 1;
				segment.style.borderRightWidth = 1;
				SetRadius(segment, 3);
				segment.style.marginRight = i < labels.Length - 1 ? 3 : 0;
				segment.style.paddingTop = 4;
				segment.style.paddingBottom = 4;
				ApplyColorTransition(segment, 100, "background-color", "border-color");
				SetHoverCursor(segment, MouseCursor.Link);

				var label = new Label(labels[i]) { pickingMode = PickingMode.Ignore };
				label.style.fontSize = 10;
				label.style.unityTextAlign = TextAnchor.MiddleCenter;
				ApplyColorTransition(label, 100, "color");
				segment.Add(label);

				segment.RegisterCallback<ClickEvent>(_ => {
					if (enumProp.enumValueIndex != index) {
						enumProp.enumValueIndex = index;
						enumProp.serializedObject.ApplyModifiedProperties();
					}
					Refresh();
					onChanged?.Invoke();
				});
				segment.RegisterCallback<MouseEnterEvent>(_ => { hovered[index] = true; Refresh(); });
				segment.RegisterCallback<MouseLeaveEvent>(_ => { hovered[index] = false; Refresh(); });

				segments.Add(segment);
				track.Add(segment);
			}

			Refresh();
			return track;
		}

		/// <summary>Selbstgezeichneter, per Drag bedienbarer Fortschrittsbalken für ein Float-SerializedProperty — ersetzt Unitys nativen Slider (dessen interne Bauteile sich zwischen Unity-Versionen ändern) durch ein voll kontrolliertes, im Sperlich-Stil eingefärbtes Element. Ruf die zurückgegebene refresh-Action nach externen Wertänderungen auf.</summary>
		public static (VisualElement track, Action refresh) CreateDraggableBar(SerializedProperty floatProp, float min, float max, Color accent, int height = 5) {
			var track = new VisualElement { pickingMode = PickingMode.Position };
			track.style.height = height;
			track.style.backgroundColor = SperlichEditorTheme.BgDark;
			track.style.flexGrow = 1;
			track.style.position = Position.Relative;
			SetRadius(track, height / 2f);
			ApplyColorTransition(track, 100, "background-color");
			SetHoverCursor(track, MouseCursor.SlideArrow);

			var fill = new VisualElement { style = { height = height, backgroundColor = accent, position = Position.Absolute, left = 0, top = 0 } };
			SetRadius(fill, height / 2f);
			track.Add(fill);

			Color trackHoverBg = Color.Lerp(SperlichEditorTheme.BgDark, Color.white, 0.1f);
			track.RegisterCallback<MouseEnterEvent>(_ => track.style.backgroundColor = trackHoverBg);
			track.RegisterCallback<MouseLeaveEvent>(_ => track.style.backgroundColor = SperlichEditorTheme.BgDark);

			void Refresh() {
				float t = max > min ? Mathf.InverseLerp(min, max, floatProp.floatValue) : 0f;
				fill.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);
			}

			void SetFromLocalX(float localX) {
				float width = track.resolvedStyle.width;
				if (width <= 0f) return;
				float t = Mathf.Clamp01(localX / width);
				floatProp.floatValue = Mathf.Lerp(min, max, t);
				floatProp.serializedObject.ApplyModifiedProperties();
				Refresh();
			}

			bool dragging = false;
			track.RegisterCallback<PointerDownEvent>(evt => {
				dragging = true;
				track.CapturePointer(evt.pointerId);
				SetFromLocalX(evt.localPosition.x);
			});
			track.RegisterCallback<PointerMoveEvent>(evt => {
				if (dragging) SetFromLocalX(evt.localPosition.x);
			});
			track.RegisterCallback<PointerUpEvent>(evt => {
				if (dragging == false) return;
				dragging = false;
				track.ReleasePointer(evt.pointerId);
			});

			Refresh();
			return (track, Refresh);
		}

		/// <summary>Flaches, klickbares Dropdown im Sperlich-Stil (Haken beim aktiven Eintrag, Hover, schließt
		/// bei Klick außerhalb ODER wenn der Editor-Fokus das Fenster wechselt). Generischer Kern hinter
		/// <see cref="CreateEnumDropdown"/> und <see cref="CreateAssetDropdown{T}"/>.</summary>
		/// <param name="getCount">Anzahl Optionen.</param>
		/// <param name="getLabel">Anzeigetext für Option <c>index</c>.</param>
		/// <param name="getSelected">Index der aktuell gewählten Option (-1 = keine).</param>
		/// <param name="onSelect">Wird mit dem geklickten Index aufgerufen; muss den Wert selbst persistieren.</param>
		public static VisualElement BuildDropdown(System.Func<int> getCount, System.Func<int, string> getLabel,
			System.Func<int> getSelected, System.Action<int> onSelect, Color? accent = null) {
			Color accentColor = accent ?? SperlichEditorTheme.ButtonAccent;

			var field = new VisualElement { pickingMode = PickingMode.Position };
			field.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
			field.style.alignItems = Align.Center;
			field.style.justifyContent = Justify.SpaceBetween;
			field.style.backgroundColor = SperlichEditorTheme.BgDark;
			field.style.paddingLeft = 6;
			field.style.paddingRight = 6;
			field.style.height = 20;
			field.style.flexGrow = 1;
			SetRadius(field, 3);
			ApplyColorTransition(field, 100, "background-color");
			SetHoverCursor(field, MouseCursor.Link);
			field.RegisterCallback<MouseEnterEvent>(_ => field.style.backgroundColor = Color.Lerp(SperlichEditorTheme.BgDark, Color.white, 0.06f));
			field.RegisterCallback<MouseLeaveEvent>(_ => field.style.backgroundColor = SperlichEditorTheme.BgDark);

			var valueLabel = new Label {
				pickingMode = PickingMode.Ignore,
				style = {
					fontSize = 11,
					color = SperlichEditorTheme.TextPrimary,
					flexGrow = 1,
					flexShrink = 1,
					whiteSpace = WhiteSpace.NoWrap,
					overflow = Overflow.Hidden,
					textOverflow = TextOverflow.Ellipsis,
				}
			};
			var chevron = new Label("▾") { pickingMode = PickingMode.Ignore, style = { fontSize = 9, color = SperlichEditorTheme.TextMuted, marginLeft = 4, flexShrink = 0 } };
			field.Add(valueLabel);
			field.Add(chevron);

			int GetOptionCount() => getCount();
			string GetOptionLabel(int index) => getLabel(index);

			void RefreshLabel() {
				int idx = getSelected();
				valueLabel.text = idx >= 0 && idx < GetOptionCount() ? GetOptionLabel(idx) : "—";
			}
			RefreshLabel();

			VisualElement openPopup = null;
			VisualElement dismissTree = null;
			EventCallback<PointerDownEvent> dismissHandler = null;
			EventCallback<WheelEvent> wheelDismissHandler = null;
			// Fires while the popup is open: closes it as soon as the editor focus leaves this window
			// (a click into the Scene / Game / Hierarchy / Project view etc.). The in-panel PointerDown
			// handler above only sees clicks inside the same inspector.
			EditorApplication.CallbackFunction focusWatch = null;

			void ClosePopup() {
				openPopup?.RemoveFromHierarchy();
				openPopup = null;
				if (dismissTree != null) {
					if (dismissHandler != null) dismissTree.UnregisterCallback(dismissHandler, TrickleDown.TrickleDown);
					if (wheelDismissHandler != null) dismissTree.UnregisterCallback(wheelDismissHandler, TrickleDown.TrickleDown);
				}
				dismissTree = null;
				dismissHandler = null;
				wheelDismissHandler = null;
				if (focusWatch != null) { EditorApplication.update -= focusWatch; focusWatch = null; }
			}

			void OpenPopup() {
				if (openPopup != null) { ClosePopup(); return; }
				VisualElement panelRoot = ResolveOverlayRoot(field);
				if (panelRoot == null) return;

				var popup = CreateBox(4, SperlichEditorTheme.BorderStrong);
				popup.style.position = Position.Absolute;
				popup.style.backgroundColor = SperlichEditorTheme.BgPanel;
				popup.style.maxHeight = 320;

				// long option lists (e.g. every FontDefinition in the project) scroll instead of running off-screen
				var optionHost = new ScrollView(ScrollViewMode.Vertical);
				popup.Add(optionHost);

				int optionCount = GetOptionCount();
				for (int i = 0; i < optionCount; i++) {
					int index = i;
					bool selected = index == getSelected();

					var row = new VisualElement { pickingMode = PickingMode.Position };
					row.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
					row.style.alignItems = Align.Center;
					row.style.paddingLeft = 8;
					row.style.paddingRight = 8;
					row.style.paddingTop = 4;
					row.style.paddingBottom = 4;
					ApplyColorTransition(row, 80, "background-color");
					SetHoverCursor(row, MouseCursor.Link);

					var check = new Label(selected ? "✓" : "") { pickingMode = PickingMode.Ignore, style = { fontSize = 10, color = accentColor, width = 14, flexShrink = 0 } };
					var label = new Label(GetOptionLabel(i)) { pickingMode = PickingMode.Ignore, style = { fontSize = 11, color = selected ? SperlichEditorTheme.TextPrimary : SperlichEditorTheme.TextSecondary, flexGrow = 1, whiteSpace = WhiteSpace.NoWrap, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } };
					row.Add(check);
					row.Add(label);

					row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f));
					row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
					row.RegisterCallback<ClickEvent>(evt => {
						evt.StopPropagation();
						onSelect(index);
						RefreshLabel();
						ClosePopup();
					});

					optionHost.Add(row);
				}

				panelRoot.Add(popup);

				Rect fieldBound = field.worldBound;
				Vector2 topLeft = panelRoot.WorldToLocal(new Vector2(fieldBound.xMin, fieldBound.yMax));
				popup.style.left = topLeft.x;
				popup.style.top = topLeft.y + 2;
				popup.style.minWidth = Mathf.Max(fieldBound.width, 140f);

				openPopup = popup;

				dismissTree = field.panel?.visualTree;
				if (dismissTree != null) {
					dismissHandler = evt => {
						if (openPopup == null) return;
						var t = evt.target as VisualElement;
						if (t != null && (t == field || field.Contains(t))) return;
						if (t != null && (t == openPopup || openPopup.Contains(t))) return;
						ClosePopup();
					};
					wheelDismissHandler = _ => ClosePopup();
					dismissTree.RegisterCallback(dismissHandler, TrickleDown.TrickleDown);
					dismissTree.RegisterCallback(wheelDismissHandler, TrickleDown.TrickleDown);
				}

				EditorWindow triggerWindow = EditorWindow.focusedWindow;
				focusWatch = () => {
					if (openPopup == null) return;
					if (EditorWindow.focusedWindow != triggerWindow) ClosePopup();
				};
				EditorApplication.update += focusWatch;
			}

			field.RegisterCallback<ClickEvent>(_ => OpenPopup());
			field.RegisterCallback<DetachFromPanelEvent>(_ => ClosePopup());

			return field;
		}

		/// <summary>Flaches Enum-Dropdown im Sperlich-Stil — ersetzt Unitys native Enum-Popups.</summary>
		public static VisualElement CreateEnumDropdown(SerializedProperty enumProp, Color? accent = null, Action<int> onChanged = null) {
			string LabelFor(int index) {
				var display = enumProp.enumDisplayNames;
				if (display != null && index >= 0 && index < display.Length && string.IsNullOrEmpty(display[index]) == false) return display[index];
				var raw = enumProp.enumNames;
				if (raw != null && index >= 0 && index < raw.Length) return ObjectNames.NicifyVariableName(raw[index]);
				return "—";
			}
			return BuildDropdown(
				() => enumProp.enumNames?.Length ?? 0,
				LabelFor,
				() => enumProp.enumValueIndex,
				i => {
					if (enumProp.enumValueIndex == i) return;
					enumProp.enumValueIndex = i;
					enumProp.serializedObject.ApplyModifiedProperties();
					onChanged?.Invoke(i);
				},
				accent);
		}

		/// <summary>Flaches Dropdown, das alle Assets vom Typ <typeparamref name="T"/> im Projekt listet
		/// (optional mit "None" an erster Stelle) und die Auswahl in ein Object-Reference-Property schreibt.
		/// Die Liste wird bei jedem Öffnen neu eingelesen, damit frisch angelegte Assets sofort auftauchen.</summary>
		public static VisualElement CreateAssetDropdown<T>(SerializedProperty objectProp, Color? accent = null, bool includeNone = true)
			where T : UnityEngine.Object {

			var assets = new System.Collections.Generic.List<T>();
			void Rescan() {
				assets.Clear();
				foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name)) {
					var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
					if (a != null) assets.Add(a);
				}
				assets.Sort((x, y) => string.Compare(x.name, y.name, System.StringComparison.OrdinalIgnoreCase));
			}
			Rescan();

			int Offset() => includeNone ? 1 : 0;
			int Count() => assets.Count + Offset();
			string LabelFor(int i) {
				if (includeNone && i == 0) return "None";
				int ai = i - Offset();
				return ai >= 0 && ai < assets.Count ? assets[ai].name : "—";
			}
			int Selected() {
				var cur = objectProp.objectReferenceValue as T;
				if (cur == null) return includeNone ? 0 : -1;
				int ai = assets.IndexOf(cur);
				return ai >= 0 ? ai + Offset() : -1;
			}
			void Pick(int i) {
				int ai = i - Offset();
				objectProp.objectReferenceValue = includeNone && i == 0 ? null
					: (ai >= 0 && ai < assets.Count ? assets[ai] : null);
				objectProp.serializedObject.ApplyModifiedProperties();
			}

			VisualElement field = BuildDropdown(Count, LabelFor, Selected, Pick, accent);
			// re-read the project list just before the popup opens (pointer-down fires before the open click)
			field.RegisterCallback<PointerDownEvent>(_ => Rescan(), TrickleDown.TrickleDown);
			return field;
		}

		/// <summary>Zeile aus kleinen Umschalt-Buttons für ein <c>[Flags]</c>-Enum-Property (wie TMPs "Font Style"):
		/// mehrere Bits gleichzeitig aktiv, aktive Buttons mit Akzent-Rahmen. <paramref name="exclusiveGroups"/>
		/// listet Bit-Gruppen, in denen immer nur eins gesetzt sein darf (z.B. Uppercase / Lowercase / SmallCaps).</summary>
		/// <param name="separatorsBefore">Bit-Indizes, vor denen ein dünner senkrechter Trenner eingefügt wird
		/// (z.B. um Groß-/Kleinschreibung von Bold/Italic optisch abzusetzen).</param>
		public static VisualElement CreateFlagButtons(SerializedProperty flagsProp, string[] captions, string[] tooltips,
			Color? accent = null, int[][] exclusiveGroups = null, int[] separatorsBefore = null) {

			Color accentColor = accent ?? SperlichEditorTheme.ButtonAccent;
			var bar = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, flexWrap = Wrap.Wrap } };
			int n = captions.Length;
			var btns = new VisualElement[n];

			void Refresh() {
				int mask = flagsProp.intValue;
				for (int i = 0; i < n; i++) {
					bool on = (mask & (1 << i)) != 0;
					btns[i].style.backgroundColor = on ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.16f) : SperlichEditorTheme.ButtonBg;
					SetBorderColor(btns[i], on ? accentColor : SperlichEditorTheme.ButtonBorder);
					((Label)btns[i][0]).style.color = on ? accentColor : SperlichEditorTheme.TextSecondary;
				}
			}

			for (int i = 0; i < n; i++) {
				int bit = i;
				if (separatorsBefore != null && System.Array.IndexOf(separatorsBefore, i) >= 0) {
					bar.Add(new VisualElement {
						style = {
							width = 1, height = 14, backgroundColor = SperlichEditorTheme.BorderStrong,
							marginLeft = 3, marginRight = 6, alignSelf = Align.Center,
						}
					});
				}
				var b = new VisualElement { pickingMode = PickingMode.Position, tooltip = tooltips != null && i < tooltips.Length ? tooltips[i] : null };
				b.style.height = 20;
				b.style.minWidth = 24;
				b.style.marginRight = 3;
				b.style.marginBottom = 2;
				b.style.paddingLeft = 6;
				b.style.paddingRight = 6;
				b.style.borderTopWidth = 1;
				b.style.borderBottomWidth = 1;
				b.style.borderLeftWidth = 1;
				b.style.borderRightWidth = 1;
				b.style.justifyContent = Justify.Center;
				b.style.alignItems = Align.Center;
				SetRadius(b, 3);
				SetHoverCursor(b, MouseCursor.Link);
				b.Add(new Label(captions[i]) { pickingMode = PickingMode.Ignore, style = { fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });

				b.RegisterCallback<ClickEvent>(_ => {
					int mask = flagsProp.intValue;
					bool turningOn = (mask & (1 << bit)) == 0;
					if (turningOn && exclusiveGroups != null) {
						foreach (int[] group in exclusiveGroups) {
							if (System.Array.IndexOf(group, bit) < 0) continue;
							foreach (int other in group) mask &= ~(1 << other);
						}
					}
					mask = turningOn ? (mask | (1 << bit)) : (mask & ~(1 << bit));
					flagsProp.intValue = mask;
					flagsProp.serializedObject.ApplyModifiedProperties();
					Refresh();
				});

				btns[i] = b;
				bar.Add(b);
			}

			bar.TrackPropertyValue(flagsProp, _ => Refresh());
			Refresh();
			return bar;
		}

		/// <summary>Kompaktes Feld für Feld-Cluster: winziges Caption-Label + schmales PropertyField ohne
		/// eigenes Label. <paramref name="captionAbove"/> = Caption über dem Feld (wie Unitys Vector-Felder),
		/// sonst links daneben. Zusammen mit <see cref="CreateFieldCluster"/> für Reihen wie "Margins".</summary>
		public static VisualElement CreateCompactField(string caption, SerializedProperty prop, bool captionAbove = false) {
			var wrap = new VisualElement {
				style = {
					flexDirection = captionAbove ? UnityEngine.UIElements.FlexDirection.Column : UnityEngine.UIElements.FlexDirection.Row,
					alignItems = captionAbove ? Align.Stretch : Align.Center,
					marginRight = 6,
				}
			};
			var cap = new Label(caption) {
				style = {
					fontSize = 10, color = SperlichEditorTheme.TextMuted,
					marginRight = captionAbove ? 0 : 5, marginBottom = captionAbove ? 1 : 0,
					flexShrink = 0, unityTextAlign = TextAnchor.MiddleLeft,
				}
			};
			VisualElement field = null;
			if (prop != null) {
				if (prop.propertyType == SerializedPropertyType.Float || prop.propertyType == SerializedPropertyType.Integer) {
					field = CreateDragNumberField(prop);   // keeps the drag-to-scrub grip in compact clusters
				} else {
					var pf = new PropertyField(prop, " ");
					SperlichFieldColumn.HideInternalLabel(pf);
					field = pf;
				}
				field.style.flexGrow = 1;
			}
			wrap.Add(cap);
			if (field != null) wrap.Add(field);
			return wrap;
		}

		/// <summary>Reihe aus mehreren <see cref="CreateCompactField"/>-Feldern; bricht um, sobald der Platz
		/// nicht mehr für alle reicht (<paramref name="minFieldWidth"/> pro Feld). Für Margins-/Spacing-Cluster.</summary>
		public static VisualElement CreateFieldCluster(int minFieldWidth, params VisualElement[] compactFields) {
			var row = new VisualElement {
				style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = Align.Center, flexGrow = 1 }
			};
			foreach (var f in compactFields) {
				f.style.flexGrow = 1;
				f.style.flexShrink = 1;
				f.style.flexBasis = 0;
				f.style.minWidth = minFieldWidth;
				row.Add(f);
			}
			return row;
		}

		/// <summary>Sucht den obersten Vorfahren, der noch Editor-Styling (u.a. die Font-Definition) trägt — das InspectorElement bzw. ersatzweise das oberste Inhalts-Element unter der Panel-Wurzel. Popups/Overlays MÜSSEN hier eingehängt werden, nicht direkt in <c>panel.visualTree</c>: die nackte Panel-Wurzel vererbt keinen Font, wodurch Text im Overlay unsichtbar bleibt (ohne Fehlermeldung).</summary>
		public static VisualElement ResolveOverlayRoot(VisualElement from) {
			VisualElement contentRoot = from;
			for (var p = from.hierarchy.parent; p != null && p.hierarchy.parent != null; p = p.hierarchy.parent) {
				contentRoot = p;
				if (p.GetType().Name == "InspectorElement") {
					return p;
				}
			}
			return contentRoot;
		}

		public static void SetRadius(VisualElement element, float radius) {
			element.style.borderTopLeftRadius = radius;
			element.style.borderTopRightRadius = radius;
			element.style.borderBottomLeftRadius = radius;
			element.style.borderBottomRightRadius = radius;
		}

		public static void SetBorderColor(VisualElement element, Color color) {
			element.style.borderTopColor = color;
			element.style.borderBottomColor = color;
			element.style.borderLeftColor = color;
			element.style.borderRightColor = color;
		}
	}
}
