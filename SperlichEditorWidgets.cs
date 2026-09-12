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

			// MouseEnterEvent/MouseLeaveEvent fire only at the element boundary — not when moving
			// between parent and child (MouseOverEvent/MouseOutEvent would re-fire for every child
			// transition, causing rapid hover-state flicker and a cascade of 100ms transitions).
			btn.RegisterCallback<MouseEnterEvent>(_ => {
				btn.style.backgroundColor = SperlichEditorTheme.ButtonHoverBg;
				if (isAccent) SetBorderColor(btn, SperlichEditorTheme.ButtonAccent);
			});
			btn.RegisterCallback<MouseLeaveEvent>(_ => {
				btn.style.backgroundColor = SperlichEditorTheme.ButtonBg;
				SetBorderColor(btn, SperlichEditorTheme.ButtonBorder);
			});
			ApplyHoverJuice(btn, "background-color", "border-color");
		}

		/// <summary>Subtle hover-lift + click-press feedback: a light scale-up while the pointer is over the
		/// element, a quick scale-down while it's pressed. Purely a transform effect -- never touches
		/// background/border colors itself, so it composes safely on top of a button's own selected/hover color
		/// logic (<see cref="ApplyNeonButtonStyle"/>, <c>CreateFlagButtons</c>, an align-button bar, ...).
		/// <paramref name="alsoTransition"/> lets a caller fold its own color properties into the same single
		/// transition list (UI Toolkit replaces the whole list on every assignment, so they can't be set separately).</summary>
		public static void ApplyHoverJuice(VisualElement el, params string[] alsoTransition) {
			var props = new List<StylePropertyName> { new StylePropertyName("scale") };
			if (alsoTransition != null) foreach (string p in alsoTransition) props.Add(new StylePropertyName(p));
			el.style.transitionProperty = props;
			el.style.transitionDuration = new List<TimeValue> { new TimeValue(110, TimeUnit.Millisecond) };
			el.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) };
			el.style.scale = new StyleScale(new Scale(Vector3.one));

			bool hovering = false, pressed = false;
			void Apply() => el.style.scale = new StyleScale(new Scale(Vector3.one * (pressed ? 0.94f : hovering ? 1.03f : 1f)));
			el.RegisterCallback<MouseEnterEvent>(_ => { hovering = true; Apply(); });
			el.RegisterCallback<MouseLeaveEvent>(_ => { hovering = false; pressed = false; Apply(); });
			el.RegisterCallback<MouseDownEvent>(_ => { pressed = true; Apply(); });
			el.RegisterCallback<MouseUpEvent>(_ => { pressed = false; Apply(); });
		}

		public static Button MakeButton(string text, int width, Action onClick, bool isAccent = false) {
			var btn = new Button(onClick) { text = text };
			if (width > 0) btn.style.width = width;
			btn.style.height = 20;
			ApplyNeonButtonStyle(btn, isAccent);
			return btn;
		}

		/// <summary>Small round "×" icon button for removing a list entry -- meant to sit as an absolute-positioned
		/// corner overlay on a card (see <c>GlyphActionRegistryEditor</c>'s Action/Device cards) instead of inline
		/// next to a field, which crowds the field and looks tacked-on. Neutral until hovered, then reddens.</summary>
		public static VisualElement CreateRemoveButton(Action onClick, int size = 18) {
			var btn = new VisualElement { pickingMode = PickingMode.Position, style = {
				width = size, height = size, flexShrink = 0, alignItems = Align.Center, justifyContent = Justify.Center,
				backgroundColor = SperlichEditorTheme.BgDark,
			} };
			SetRadius(btn, size / 2f);
			SetHoverCursor(btn, MouseCursor.Link);
			ApplyHoverJuice(btn, "background-color");

			var glyph = new Label("×") { pickingMode = PickingMode.Ignore, style = {
				fontSize = size >= 18 ? 13 : 11, color = SperlichEditorTheme.TextMuted, unityFontStyleAndWeight = FontStyle.Bold,
			} };
			btn.Add(glyph);

			Color danger = SperlichEditorTheme.BadgeDangerBg;
			btn.RegisterCallback<MouseEnterEvent>(_ => {
				btn.style.backgroundColor = new Color(danger.r, danger.g, danger.b, 0.25f);
				glyph.style.color = danger;
			});
			btn.RegisterCallback<MouseLeaveEvent>(_ => {
				btn.style.backgroundColor = SperlichEditorTheme.BgDark;
				glyph.style.color = SperlichEditorTheme.TextMuted;
			});
			btn.RegisterCallback<ClickEvent>(evt => { evt.StopPropagation(); onClick(); });
			return btn;
		}

		public static Label CreateBadge(string text, Color bg, Color? textColor = null) {
			var badge = new Label(text);
			badge.style.fontSize = 10;
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

		/// <summary>Schweregrad für <see cref="CreateMessageBox"/> — bestimmt Farbe und Symbol.</summary>
		public enum MessageKind { Info, Warning, Error }

		/// <summary>Farbige Hinweis-/Warn-/Fehlerbox im Sperlich-Stil (Pendant zu Unitys <c>HelpBox</c>), z.B.
		/// um eine Projekt-Konfigurationsverletzung direkt im Inspector sichtbar zu machen. Text bricht
		/// automatisch um; die Box wächst mit dem Inhalt.</summary>
		public static VisualElement CreateMessageBox(string message, MessageKind kind = MessageKind.Info) {
			(Color bg, Color border, string icon) = kind switch {
				MessageKind.Warning => (new Color(SperlichEditorTheme.BadgeWarnBg.r, SperlichEditorTheme.BadgeWarnBg.g, SperlichEditorTheme.BadgeWarnBg.b, 0.18f), SperlichEditorTheme.BadgeWarnBg, "⚠"),
				MessageKind.Error => (new Color(SperlichEditorTheme.BadgeDangerBg.r, SperlichEditorTheme.BadgeDangerBg.g, SperlichEditorTheme.BadgeDangerBg.b, 0.18f), SperlichEditorTheme.BadgeDangerBg, "✕"),
				_ => (SperlichEditorTheme.BgPanel, SperlichEditorTheme.BorderStrong, "ℹ"),
			};

			var box = CreateBox(4, border);
			box.style.backgroundColor = bg;
			box.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
			box.style.alignItems = Align.FlexStart;
			box.style.paddingLeft = 8;
			box.style.paddingRight = 8;
			box.style.paddingTop = 6;
			box.style.paddingBottom = 6;
			box.style.marginBottom = 4;

			box.Add(new Label(icon) { style = { color = border, fontSize = 12, marginRight = 6, flexShrink = 0, unityFontStyleAndWeight = FontStyle.Bold } });
			box.Add(new Label(message) {
				style = {
					whiteSpace = UnityEngine.UIElements.WhiteSpace.Normal, flexGrow = 1, flexShrink = 1,
					fontSize = 11, color = SperlichEditorTheme.TextPrimary,
				}
			});
			return box;
		}

		/// <summary>Kleines "ⓘ"-Symbol, das nur einen Hover-Tooltip trägt — für ausführlichere Erklärtexte, die
		/// nicht dauerhaft Platz im Inspector beanspruchen sollen (Sperlich-Editor-Konvention: erklärende
		/// Absätze wandern in den Tooltip statt als grauer Fließtext stehen zu bleiben).</summary>
		public static VisualElement CreateInfoIcon(string tooltip) {
			return new Label("ⓘ") {
				tooltip = tooltip,
				pickingMode = PickingMode.Position,
				style = { color = SperlichEditorTheme.TextMuted, fontSize = 11, marginLeft = 6, flexShrink = 0 }
			};
		}

		/// <summary>Dünner farbiger Akzent-Balken für den linken Rand eines Headers, einer Gruppe oder Karte —
		/// bindet eine Section visuell an eine semantische Farbe (z.B. einen Typ/eine Kategorie), ohne den
		/// ganzen Hintergrund einzufärben. Flach, ohne Rundung (bewusst schlicht, damit er an Header-Zeilen
		/// bündig sitzt).</summary>
		public static VisualElement CreateColorSidebar(Color color, float width = 4f) {
			return new VisualElement { style = { width = width, flexShrink = 0, alignSelf = Align.Stretch, backgroundColor = color } };
		}

		/// <summary>Verpackt beliebigen Inhalt (Header, Box, Feldgruppe, ganze Karte, …) mit einem farbigen
		/// <see cref="CreateColorSidebar"/>-Balken links davon. Generischer als der <c>sidebarColor</c>-Parameter
		/// von <see cref="CreateChevronSection"/>, weil er nicht an das Chevron-Header/Body-Muster gebunden ist.</summary>
		public static VisualElement CreateSidebarGroup(Color color, VisualElement content, float width = 4f) {
			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row } };
			row.Add(CreateColorSidebar(color, width));
			content.style.flexGrow = 1;
			row.Add(content);
			return row;
		}

		/// <summary>Kollabierbare Sektion mit handgezeichnetem ▼/▶-Pfeil statt nativem Foldout (Sperlich-Editor-Konvention, siehe AnimSequencerEditor).</summary>
		/// <param name="persistKey">Wenn gesetzt: der Auf-/Zu-Zustand wird unter diesem Schlüssel (+ Titel) in
		/// <see cref="EditorPrefs"/> gemerkt, sodass er einen Inspector-Rebuild (Undo/Redo, Domain-Reload) übersteht.</param>
		/// <param name="sidebarColor">Optional: ein <see cref="CreateColorSidebar"/>-Balken am linken Rand des Headers
		/// (z.B. um eine Kategorie/einen Typ farblich zu kennzeichnen, wie im AnimSequencer-Inspector).</param>
		public static (VisualElement header, VisualElement body, Label arrow) CreateChevronSection(string title, bool expanded, Color headerBg, Color? bodyBg = null, string persistKey = null, Color? sidebarColor = null) {
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

			if (sidebarColor.HasValue) {
				var sidebar = CreateColorSidebar(sidebarColor.Value);
				sidebar.style.marginLeft = -6; // cancels header's paddingLeft so the bar sits flush with the edge
				sidebar.style.marginRight = 6;
				header.Add(sidebar);
			}

			var arrow = new Label(expanded ? "▼" : "▶");
			arrow.style.fontSize = 10;
			arrow.style.width = 12;
			arrow.style.color = SperlichEditorTheme.TextMuted;
			header.Add(arrow);

			var titleLabel = new Label(title);
			titleLabel.style.fontSize = 11;
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
		public static VisualElement CreateSegmentedControl(SerializedProperty enumProp, string[] labels, Color accent, Action onChanged = null, int buttonsPerRow = 0) =>
			CreateSegmentedControl(enumProp, labels, _ => accent, onChanged, buttonsPerRow);

		/// <summary>Same as <see cref="CreateSegmentedControl(SerializedProperty,string[],Color,Action,int)"/>, but
		/// each segment gets its own colour instead of one shared accent (e.g. a Sequential/Parallel toggle
		/// where each state has a distinct meaning-colour).</summary>
		public static VisualElement CreateSegmentedControl(SerializedProperty enumProp, string[] labels, Color[] accentPerSegment, Action onChanged = null, int buttonsPerRow = 0) =>
			CreateSegmentedControl(enumProp, labels, i => i >= 0 && i < accentPerSegment.Length ? accentPerSegment[i] : SperlichEditorTheme.ButtonAccent, onChanged, buttonsPerRow);

		private static VisualElement CreateSegmentedControl(SerializedProperty enumProp, string[] labels, Func<int, Color> accentFor, Action onChanged, int buttonsPerRow = 0) {
			VisualElement rootElement;
			var segments = new List<VisualElement>();
			var hovered = new bool[labels.Length];

			void Refresh() {
				int current = enumProp.enumValueIndex;
				for (int i = 0; i < segments.Count; i++) {
					bool active = i == current;
					bool isHovered = hovered[i];
					Color accent = accentFor(i);
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

			VisualElement CreateSegment(int index, float marginRight) {
				var segment = new VisualElement { pickingMode = PickingMode.Position };
				segment.style.flexGrow = 1;
				segment.style.borderTopWidth = 1;
				segment.style.borderBottomWidth = 1;
				segment.style.borderLeftWidth = 1;
				segment.style.borderRightWidth = 1;
				SetRadius(segment, 3);
				segment.style.marginRight = marginRight;
				segment.style.paddingTop = 4;
				segment.style.paddingBottom = 4;
				ApplyColorTransition(segment, 100, "background-color", "border-color");
				SetHoverCursor(segment, MouseCursor.Link);

				var label = new Label(labels[index]) { pickingMode = PickingMode.Ignore };
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

				return segment;
			}

			bool isMultiRow = buttonsPerRow > 0 && labels.Length > buttonsPerRow;
			if (!isMultiRow) {
				var track = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, marginBottom = 6 } };
				rootElement = track;

				for (int i = 0; i < labels.Length; i++) {
					var segment = CreateSegment(i, i < labels.Length - 1 ? 3 : 0);
					segments.Add(segment);
					track.Add(segment);
				}
			} else {
				var column = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Column, marginBottom = 6 } };
				rootElement = column;

				int rowCount = Mathf.CeilToInt((float)labels.Length / buttonsPerRow);
				for (int r = 0; r < rowCount; r++) {
					int start = r * buttonsPerRow;
					int end = Mathf.Min(start + buttonsPerRow, labels.Length);
					var rowTrack = new VisualElement {
						style = {
							flexDirection = UnityEngine.UIElements.FlexDirection.Row,
							marginBottom = r < rowCount - 1 ? 3 : 0
						}
					};
					for (int i = start; i < end; i++) {
						var segment = CreateSegment(i, i < end - 1 ? 3 : 0);
						segments.Add(segment);
						rowTrack.Add(segment);
					}
					column.Add(rowTrack);
				}
			}

			// Re-read on any external change (Revert / Undo / OnValueChanged / multi-select) — without this the
			// segments only restyled on click or hover.
			rootElement.TrackPropertyValue(enumProp, _ => Refresh());
			Refresh();
			return rootElement;
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
		/// <param name="getBadge">Optional: kleines abgerundetes Zahl-/Wert-Kästchen am rechten Zeilenrand
		/// (z.B. der Enum-Konstantenwert). Leerer/<c>null</c>-Rückgabewert = kein Kästchen für diese Zeile.</param>
		public static VisualElement BuildDropdown(System.Func<int> getCount, System.Func<int, string> getLabel,
			System.Func<int> getSelected, System.Action<int> onSelect, Color? accent = null, System.Func<int, string> getBadge = null) {
			Color accentColor = accent ?? SperlichEditorTheme.ButtonAccent;

			var field = new VisualElement { pickingMode = PickingMode.Position };
			field.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
			field.style.alignItems = Align.Center;
			field.style.justifyContent = Justify.SpaceBetween;
			field.style.backgroundColor = SperlichEditorTheme.BgDark;
			field.style.paddingLeft = 6;
			field.style.paddingRight = 6;
			field.style.height = 22;
			field.style.flexGrow = 1;
			SetRadius(field, 3);
			ApplyColorTransition(field, 100, "background-color");
			SetHoverCursor(field, MouseCursor.Link);
			field.RegisterCallback<MouseEnterEvent>(_ => field.style.backgroundColor = Color.Lerp(SperlichEditorTheme.BgDark, Color.white, 0.06f));
			field.RegisterCallback<MouseLeaveEvent>(_ => field.style.backgroundColor = SperlichEditorTheme.BgDark);

			var valueLabel = new Label {
				pickingMode = PickingMode.Ignore,
				style = {
					fontSize = 12,
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
			// dismissHandler (trickle-down, root) fires before field's eigener PointerDown-Handler (target-phase)
			// bei EIN und demselben Klick auf das Feld. Schließt dismissHandler dabei das Popup, muss field's
			// Handler direkt danach das sofortige Wiederöffnen im selben Klick unterdrücken - sonst schließt es
			// nie sichtbar (zu/auf im selben Frame).
			bool suppressReopen = false;

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
				if (openPopup != null) return;
				VisualElement panelRoot = ResolveOverlayRoot(field);
				if (panelRoot == null) return;

				var popup = CreateBox(4, SperlichEditorTheme.BorderStrong);
				popup.style.position = Position.Absolute;
				popup.style.backgroundColor = SperlichEditorTheme.BgPanel;
				popup.style.maxHeight = 320;
				if (EditorStyles.label?.font != null) {
					popup.style.unityFont = EditorStyles.label.font;
				}

				// Stylesheets vom Quell-Element vererben, damit Fonts und Themes immer greifen
				for (var p = field; p != null; p = p.hierarchy.parent) {
					int count = p.styleSheets.count;
					for (int s = 0; s < count; s++) {
						var sheet = p.styleSheets[s];
						if (!popup.styleSheets.Contains(sheet)) {
							popup.styleSheets.Add(sheet);
						}
					}
				}

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
					row.style.paddingTop = 5;
					row.style.paddingBottom = 5;
					ApplyColorTransition(row, 80, "background-color");
					SetHoverCursor(row, MouseCursor.Link);

					var check = new Label(selected ? "✓" : "") {
						pickingMode = PickingMode.Ignore,
						style = {
							fontSize = 13,
							unityFontStyleAndWeight = FontStyle.Bold,
							color = accentColor,
							width = 16,
							flexShrink = 0,
							unityFont = EditorStyles.label?.font
						}
					};
					var label = new Label(GetOptionLabel(i)) {
						pickingMode = PickingMode.Ignore,
						style = {
							fontSize = 12,
							color = selected ? SperlichEditorTheme.TextPrimary : SperlichEditorTheme.TextSecondary,
							flexGrow = 1,
							whiteSpace = WhiteSpace.NoWrap,
							overflow = Overflow.Hidden,
							textOverflow = TextOverflow.Ellipsis,
							unityFont = EditorStyles.label?.font
						}
					};
					row.Add(check);
					row.Add(label);
					if (getBadge != null) {
						string badgeText = getBadge(index);
						if (!string.IsNullOrEmpty(badgeText)) row.Add(MakeIndexBadge(badgeText, selected));
					}

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
				popup.BringToFront();

				const float margin = 4f;
				Rect fieldBound = field.worldBound;
				Vector2 topLeft = panelRoot.WorldToLocal(new Vector2(fieldBound.xMin, fieldBound.yMax));
				Vector2 topRight = panelRoot.WorldToLocal(new Vector2(fieldBound.xMax, fieldBound.yMax));

				float popupMinWidth = Mathf.Max(fieldBound.width, 140f);
				popup.style.minWidth = popupMinWidth;

				float panelWidth = panelRoot.contentRect.width;
				float targetLeft = topLeft.x;
				if (panelWidth > 0f && targetLeft + popupMinWidth > panelWidth - margin) {
					targetLeft = Mathf.Max(margin, topRight.x - popupMinWidth);
					if (targetLeft + popupMinWidth > panelWidth - margin) {
						targetLeft = Mathf.Max(margin, panelWidth - popupMinWidth - margin);
					}
				}

				popup.style.left = targetLeft;
				popup.style.top = topLeft.y + 2;

				popup.RegisterCallback<GeometryChangedEvent>(evt => {
					if (openPopup != popup || panelRoot == null) return;
					float curPanelWidth = panelRoot.contentRect.width;
					float curPanelHeight = panelRoot.contentRect.height;
					if (curPanelWidth <= 0f || curPanelHeight <= 0f) return;

					float actualWidth = evt.newRect.width;
					float actualHeight = evt.newRect.height;

					Rect curFieldBound = field.worldBound;
					Vector2 curTopLeft = panelRoot.WorldToLocal(new Vector2(curFieldBound.xMin, curFieldBound.yMax));
					Vector2 curTopRight = panelRoot.WorldToLocal(new Vector2(curFieldBound.xMax, curFieldBound.yMax));
					Vector2 curFieldTop = panelRoot.WorldToLocal(new Vector2(curFieldBound.xMin, curFieldBound.yMin));

					float newLeft = curTopLeft.x;
					if (newLeft + actualWidth > curPanelWidth - margin) {
						newLeft = Mathf.Max(margin, curTopRight.x - actualWidth);
						if (newLeft + actualWidth > curPanelWidth - margin) {
							newLeft = Mathf.Max(margin, curPanelWidth - actualWidth - margin);
						}
					}
					popup.style.left = newLeft;

					float newTop = curTopLeft.y + 2;
					if (newTop + actualHeight > curPanelHeight - margin) {
						if (curFieldTop.y - 2 - actualHeight >= margin || curFieldTop.y > (curPanelHeight - newTop)) {
							newTop = Mathf.Max(margin, curFieldTop.y - actualHeight - 2);
						}
					}
					popup.style.top = newTop;
				});

				openPopup = popup;

				dismissTree = field.panel?.visualTree;
				if (dismissTree != null) {
					dismissHandler = evt => {
						if (openPopup == null) return;
						var t = evt.target as VisualElement;
						if (t != null && (t == openPopup || openPopup.Contains(t))) return;
						bool clickedField = t != null && (t == field || field.Contains(t));
						ClosePopup();
						if (clickedField) suppressReopen = true;
					};
					wheelDismissHandler = evt => {
						if (openPopup == null) return;
						var t = evt.target as VisualElement;
						if (t != null && (t == openPopup || openPopup.Contains(t))) return;
						ClosePopup();
					};
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

			// Öffnet nur wenn geschlossen - das Schließen (auch bei erneutem Klick auf field selbst) übernimmt
			// ausschließlich dismissHandler oben, der pro Klick garantiert vor diesem Handler feuert
			// (trickle-down vom Root vs. target-phase hier). suppressReopen verhindert, dass ein Klick, der
			// gerade erst geschlossen hat, im selben Frame sofort wieder öffnet.
			field.RegisterCallback<PointerDownEvent>(evt => {
				if (evt.button != 0) return;
				if (suppressReopen) { suppressReopen = false; return; }
				OpenPopup();
			});
			field.RegisterCallback<DetachFromPanelEvent>(_ => ClosePopup());

			return field;
		}

		/// <summary>One node of a <see cref="BuildCascadingDropdown"/> tree: either a selectable leaf (an index
		/// into the caller's flat option list) or a category with children, drilled into in place (Unity
		/// "Add Component" style) rather than opening a side flyout. Nest categories inside
		/// <see cref="Children"/> as deep as needed — the popup keeps a back-stack.</summary>
		public sealed class DropdownMenuNode {
			public readonly string Label;
			public readonly Texture2D Icon;
			public readonly int ItemIndex;
			public readonly List<DropdownMenuNode> Children;
			public bool IsLeaf => Children == null;

			private DropdownMenuNode(string label, Texture2D icon, int itemIndex, List<DropdownMenuNode> children) {
				Label = label;
				Icon = icon;
				ItemIndex = itemIndex;
				Children = children;
			}

			public static DropdownMenuNode Category(string label, Texture2D icon, List<DropdownMenuNode> children) => new(label, icon, -1, children);
			public static DropdownMenuNode Leaf(int itemIndex) => new(null, null, itemIndex, null);
		}

		/// <summary>Cascading category dropdown in the style of Unity's own "Add Component" menu: click the
		/// field, get a list of categories; click one and the SAME panel slides to show its contents, with a
		/// back row at the top to go up a level (works to any nesting depth). Use over <see cref="BuildDropdown"/>
		/// once a flat option list has grown too long to scan (e.g. 25+ items that fall into natural categories).</summary>
		/// <param name="rootNodes">Top-level tree — a mix of <see cref="DropdownMenuNode.Category"/> and <see cref="DropdownMenuNode.Leaf"/> is fine at any level.</param>
		/// <param name="getLabel">Display text for a leaf's flat item index (used for the trigger's current value and each leaf row).</param>
		/// <param name="getSelected">Flat index of the current selection, -1 = none.</param>
		/// <param name="onSelect">Called with the clicked flat index; must persist the value itself.</param>
		public static VisualElement BuildCascadingDropdown(List<DropdownMenuNode> rootNodes,
			Func<int, string> getLabel, Func<int> getSelected, Action<int> onSelect, Color? accent = null) {
			Color accentColor = accent ?? SperlichEditorTheme.ButtonAccent;

			var field = new VisualElement { pickingMode = PickingMode.Position };
			field.style.flexDirection = FlexDirection.Row;
			field.style.alignItems = Align.Center;
			field.style.justifyContent = Justify.SpaceBetween;
			field.style.backgroundColor = SperlichEditorTheme.BgDark;
			field.style.paddingLeft = 6;
			field.style.paddingRight = 6;
			field.style.height = 22;
			field.style.flexGrow = 1;
			SetRadius(field, 3);
			ApplyColorTransition(field, 100, "background-color");
			SetHoverCursor(field, MouseCursor.Link);
			field.RegisterCallback<MouseEnterEvent>(_ => field.style.backgroundColor = Color.Lerp(SperlichEditorTheme.BgDark, Color.white, 0.06f));
			field.RegisterCallback<MouseLeaveEvent>(_ => field.style.backgroundColor = SperlichEditorTheme.BgDark);

			var valueLabel = new Label {
				pickingMode = PickingMode.Ignore,
				style = { fontSize = 12, color = SperlichEditorTheme.TextPrimary, flexGrow = 1, flexShrink = 1, whiteSpace = WhiteSpace.NoWrap, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis }
			};
			var chevron = new Label("▾") { pickingMode = PickingMode.Ignore, style = { fontSize = 9, color = SperlichEditorTheme.TextMuted, marginLeft = 4, flexShrink = 0 } };
			field.Add(valueLabel);
			field.Add(chevron);

			void RefreshLabel() {
				int idx = getSelected();
				valueLabel.text = idx >= 0 ? getLabel(idx) : "—";
			}
			RefreshLabel();

			VisualElement openPopup = null;
			VisualElement dismissTree = null;
			EventCallback<PointerDownEvent> dismissHandler = null;
			EditorApplication.CallbackFunction focusWatch = null;
			bool suppressReopen = false;

			void ClosePopup() {
				openPopup?.RemoveFromHierarchy();
				openPopup = null;
				if (dismissTree != null && dismissHandler != null) dismissTree.UnregisterCallback(dismissHandler, TrickleDown.TrickleDown);
				dismissTree = null;
				dismissHandler = null;
				if (focusWatch != null) { EditorApplication.update -= focusWatch; focusWatch = null; }
			}

			bool ContainsSelected(DropdownMenuNode node) {
				if (node.IsLeaf) return node.ItemIndex == getSelected();
				foreach (DropdownMenuNode child in node.Children) if (ContainsSelected(child)) return true;
				return false;
			}

			VisualElement MakeRow(string label, Texture2D icon, bool selected, bool hasChevron) {
				var row = new VisualElement { pickingMode = PickingMode.Position };
				row.style.flexDirection = FlexDirection.Row;
				row.style.alignItems = Align.Center;
				row.style.paddingLeft = 8;
				row.style.paddingRight = 8;
				row.style.paddingTop = 5;
				row.style.paddingBottom = 5;
				ApplyColorTransition(row, 80, "background-color");
				SetHoverCursor(row, MouseCursor.Link);
				row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f));
				row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);

				if (icon != null) {
					row.Add(new Image { image = icon, scaleMode = ScaleMode.ScaleToFit, style = { width = 14, height = 14, marginRight = 6, flexShrink = 0 } });
				} else {
					row.Add(new Label(selected ? "✓" : "") { pickingMode = PickingMode.Ignore, style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, color = accentColor, width = 16, flexShrink = 0 } });
				}
				row.Add(new Label(label) {
					pickingMode = PickingMode.Ignore,
					style = { fontSize = 12, color = selected ? SperlichEditorTheme.TextPrimary : SperlichEditorTheme.TextSecondary, flexGrow = 1, whiteSpace = WhiteSpace.NoWrap }
				});
				if (hasChevron) row.Add(new Label("▸") { pickingMode = PickingMode.Ignore, style = { fontSize = 11, color = SperlichEditorTheme.TextMuted, marginLeft = 6, flexShrink = 0 } });
				return row;
			}

			void OpenPopup() {
				if (openPopup != null) return;
				VisualElement panelRoot = ResolveOverlayRoot(field);
				if (panelRoot == null) return;

				var popup = CreateBox(4, SperlichEditorTheme.BorderStrong);
				popup.style.position = Position.Absolute;
				popup.style.backgroundColor = SperlichEditorTheme.BgPanel;
				popup.style.maxHeight = 320;
				if (EditorStyles.label?.font != null) popup.style.unityFont = EditorStyles.label.font;
				for (var p = field; p != null; p = p.hierarchy.parent) {
					int count = p.styleSheets.count;
					for (int s = 0; s < count; s++) {
						var sheet = p.styleSheets[s];
						if (!popup.styleSheets.Contains(sheet)) popup.styleSheets.Add(sheet);
					}
				}

				var scroll = new ScrollView(ScrollViewMode.Vertical);
				popup.Add(scroll);

				// Back-stack of drilled-into levels; empty = showing rootNodes.
				var navStack = new List<(List<DropdownMenuNode> nodes, string label)>();

				void RenderLevel() {
					scroll.Clear();
					List<DropdownMenuNode> nodes = navStack.Count > 0 ? navStack[^1].nodes : rootNodes;

					if (navStack.Count > 0) {
						var header = new VisualElement { pickingMode = PickingMode.Position };
						header.style.flexDirection = FlexDirection.Row;
						header.style.alignItems = Align.Center;
						header.style.paddingLeft = 8;
						header.style.paddingRight = 8;
						header.style.paddingTop = 5;
						header.style.paddingBottom = 5;
						header.style.borderBottomWidth = 1;
						header.style.borderBottomColor = SperlichEditorTheme.BorderSubtle;
						ApplyColorTransition(header, 80, "background-color");
						SetHoverCursor(header, MouseCursor.Link);
						header.RegisterCallback<MouseEnterEvent>(_ => header.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f));
						header.RegisterCallback<MouseLeaveEvent>(_ => header.style.backgroundColor = Color.clear);
						header.Add(new Label("◂") { pickingMode = PickingMode.Ignore, style = { fontSize = 13, color = accentColor, marginRight = 6, flexShrink = 0 } });
						header.Add(new Label(navStack[^1].label) {
							pickingMode = PickingMode.Ignore,
							style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextPrimary, flexGrow = 1 }
						});
						header.RegisterCallback<ClickEvent>(evt => {
							evt.StopPropagation();
							navStack.RemoveAt(navStack.Count - 1);
							RenderLevel();
						});
						scroll.Add(header);
					}

					foreach (DropdownMenuNode node in nodes) {
						if (node.IsLeaf) {
							bool selected = node.ItemIndex == getSelected();
							var row = MakeRow(getLabel(node.ItemIndex), null, selected, hasChevron: false);
							row.RegisterCallback<ClickEvent>(evt => {
								evt.StopPropagation();
								onSelect(node.ItemIndex);
								RefreshLabel();
								ClosePopup();
							});
							scroll.Add(row);
						} else {
							var row = MakeRow(node.Label, node.Icon, ContainsSelected(node), hasChevron: true);
							row.RegisterCallback<ClickEvent>(evt => {
								evt.StopPropagation();
								navStack.Add((node.Children, node.Label));
								RenderLevel();
							});
							scroll.Add(row);
						}
					}
					// Quick fade blip on every render (open AND each drill-in/back navigation) — cheap micro
					// feedback that something changed. Driven imperatively (not a CSS transition): a
					// transition needs its "from" value actually painted for one frame before the "to" value
					// lands, which a same-frame Clear()+repopulate never gives it — it would just snap.
					scroll.style.opacity = 0f;
					scroll.experimental.animation.Start(0f, 1f, 110, (e, v) => e.style.opacity = v);
				}
				RenderLevel();

				panelRoot.Add(popup);
				popup.BringToFront();

				const float margin = 4f;
				Rect fieldBound = field.worldBound;
				Vector2 topLeft = panelRoot.WorldToLocal(new Vector2(fieldBound.xMin, fieldBound.yMax));
				Vector2 topRight = panelRoot.WorldToLocal(new Vector2(fieldBound.xMax, fieldBound.yMax));
				float popupMinWidth = Mathf.Max(fieldBound.width, 170f);
				popup.style.minWidth = popupMinWidth;
				float panelWidth = panelRoot.contentRect.width;
				float targetLeft = topLeft.x;
				if (panelWidth > 0f && targetLeft + popupMinWidth > panelWidth - margin) {
					targetLeft = Mathf.Max(margin, topRight.x - popupMinWidth);
				}
				popup.style.left = targetLeft;
				popup.style.top = topLeft.y + 2;

				// Vertical clamping: a long list (e.g. many categories/leaves) can run past the bottom of the
				// window when the field sits low in a full Inspector. Once real layout is known, flip the popup
				// to open upward from the field instead of letting it get clipped off-window.
				EventCallback<GeometryChangedEvent> clampVertical = null;
				clampVertical = _ => {
					popup.UnregisterCallback(clampVertical);
					float panelHeight = panelRoot.contentRect.height;
					if (panelHeight <= 0f) return;
					float bottomEdge = topLeft.y + 2 + popup.layout.height;
					if (bottomEdge <= panelHeight - margin) return;
					Vector2 aboveTopLeft = panelRoot.WorldToLocal(new Vector2(fieldBound.xMin, fieldBound.yMin));
					float openedUpTop = aboveTopLeft.y - popup.layout.height - 2;
					popup.style.top = openedUpTop >= margin ? openedUpTop : Mathf.Max(margin, panelHeight - popup.layout.height - margin);
				};
				popup.RegisterCallback(clampVertical);

				popup.style.opacity = 0f;
				popup.experimental.animation.Start(0f, 1f, 120, (e, v) => {
					e.style.opacity = v;
					e.style.translate = new Translate(0, -4f * (1f - v), 0);
				});

				openPopup = popup;
				dismissTree = field.panel?.visualTree;
				if (dismissTree != null) {
					dismissHandler = evt => {
						if (openPopup == null) return;
						var t = evt.target as VisualElement;
						if (t != null && (t == openPopup || openPopup.Contains(t))) return;
						bool clickedField = t != null && (t == field || field.Contains(t));
						ClosePopup();
						if (clickedField) suppressReopen = true;
					};
					dismissTree.RegisterCallback(dismissHandler, TrickleDown.TrickleDown);
				}

				EditorWindow triggerWindow = EditorWindow.focusedWindow;
				focusWatch = () => {
					if (openPopup == null) return;
					if (EditorWindow.focusedWindow != triggerWindow) ClosePopup();
				};
				EditorApplication.update += focusWatch;
			}

			field.RegisterCallback<PointerDownEvent>(evt => {
				if (evt.button != 0) return;
				if (suppressReopen) { suppressReopen = false; return; }
				OpenPopup();
			});
			field.RegisterCallback<DetachFromPanelEvent>(_ => ClosePopup());

			return field;
		}

		/// <summary>Flaches, mehrfach auswählbares Dropdown im Sperlich-Stil für Flags/Multi-Select (Checkbox-Häkchen pro Zeile, bleibt offen bis zum Klick außerhalb).</summary>
		public static VisualElement BuildMultiSelectDropdown(
			Func<int> getCount,
			Func<int, string> getLabel,
			Func<int, bool> isSelected,
			Action<int> onToggle,
			Func<string> getHeaderLabel,
			Color? accent = null,
			Func<int, string> getBadge = null) {

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

			void RefreshHeader() {
				valueLabel.text = getHeaderLabel != null ? getHeaderLabel() : "—";
			}
			RefreshHeader();

			VisualElement openPopup = null;
			VisualElement dismissTree = null;
			EventCallback<PointerDownEvent> dismissHandler = null;
			EventCallback<WheelEvent> wheelDismissHandler = null;
			EditorApplication.CallbackFunction focusWatch = null;
			// Siehe BuildDropdown: dismissHandler feuert pro Klick garantiert vor field's eigenem Handler und
			// schließt dabei auch bei erneutem Klick auf field selbst. suppressReopen verhindert, dass field's
			// Handler das Popup im selben Klick sofort wieder öffnet.
			bool suppressReopen = false;

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
				if (openPopup != null) return;
				VisualElement panelRoot = ResolveOverlayRoot(field);
				if (panelRoot == null) return;

				var popup = CreateBox(4, SperlichEditorTheme.BorderStrong);
				popup.style.position = Position.Absolute;
				popup.style.backgroundColor = SperlichEditorTheme.BgPanel;
				popup.style.maxHeight = 320;
				if (EditorStyles.label?.font != null) {
					popup.style.unityFont = EditorStyles.label.font;
				}

				for (var p = field; p != null; p = p.hierarchy.parent) {
					int count = p.styleSheets.count;
					for (int s = 0; s < count; s++) {
						var sheet = p.styleSheets[s];
						if (!popup.styleSheets.Contains(sheet)) {
							popup.styleSheets.Add(sheet);
						}
					}
				}

				var optionHost = new ScrollView(ScrollViewMode.Vertical);
				popup.Add(optionHost);

				int optionCount = getCount();
				var checkLabels = new Label[optionCount];
				var textLabels = new Label[optionCount];
				var badgeLabels = new Label[optionCount];

				for (int i = 0; i < optionCount; i++) {
					int index = i;
					bool active = isSelected(index);

					var row = new VisualElement { pickingMode = PickingMode.Position };
					row.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
					row.style.alignItems = Align.Center;
					row.style.paddingLeft = 8;
					row.style.paddingRight = 8;
					row.style.paddingTop = 4;
					row.style.paddingBottom = 4;
					ApplyColorTransition(row, 80, "background-color");
					SetHoverCursor(row, MouseCursor.Link);

					var check = new Label(active ? "✓" : "") {
						pickingMode = PickingMode.Ignore,
						style = {
							fontSize = 10,
							color = accentColor,
							width = 14,
							flexShrink = 0,
							unityFont = EditorStyles.label?.font
						}
					};
					var label = new Label(getLabel(i)) {
						pickingMode = PickingMode.Ignore,
						style = {
							fontSize = 11,
							color = active ? SperlichEditorTheme.TextPrimary : SperlichEditorTheme.TextSecondary,
							flexGrow = 1,
							whiteSpace = WhiteSpace.NoWrap,
							overflow = Overflow.Hidden,
							textOverflow = TextOverflow.Ellipsis,
							unityFont = EditorStyles.label?.font
						}
					};
					row.Add(check);
					row.Add(label);
					if (getBadge != null) {
						string badgeText = getBadge(index);
						if (!string.IsNullOrEmpty(badgeText)) {
							Label badge = MakeIndexBadge(badgeText, active);
							badgeLabels[i] = badge;
							row.Add(badge);
						}
					}

					checkLabels[i] = check;
					textLabels[i] = label;

					row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f));
					row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
					row.RegisterCallback<ClickEvent>(evt => {
						evt.StopPropagation();
						onToggle(index);
						RefreshHeader();
						for (int j = 0; j < optionCount; j++) {
							bool isSel = isSelected(j);
							checkLabels[j].text = isSel ? "✓" : "";
							textLabels[j].style.color = isSel ? SperlichEditorTheme.TextPrimary : SperlichEditorTheme.TextSecondary;
							if (badgeLabels[j] != null) StyleIndexBadge(badgeLabels[j], isSel);
						}
					});

					optionHost.Add(row);
				}

				panelRoot.Add(popup);
				popup.BringToFront();

				const float margin = 4f;
				Rect fieldBound = field.worldBound;
				Vector2 topLeft = panelRoot.WorldToLocal(new Vector2(fieldBound.xMin, fieldBound.yMax));
				Vector2 topRight = panelRoot.WorldToLocal(new Vector2(fieldBound.xMax, fieldBound.yMax));

				float popupMinWidth = Mathf.Max(fieldBound.width, 140f);
				popup.style.minWidth = popupMinWidth;

				float panelWidth = panelRoot.contentRect.width;
				float targetLeft = topLeft.x;
				if (panelWidth > 0f && targetLeft + popupMinWidth > panelWidth - margin) {
					targetLeft = Mathf.Max(margin, topRight.x - popupMinWidth);
					if (targetLeft + popupMinWidth > panelWidth - margin) {
						targetLeft = Mathf.Max(margin, panelWidth - popupMinWidth - margin);
					}
				}

				popup.style.left = targetLeft;
				popup.style.top = topLeft.y + 2;

				popup.RegisterCallback<GeometryChangedEvent>(evt => {
					if (openPopup != popup || panelRoot == null) return;
					float pw = panelRoot.contentRect.width;
					float ph = panelRoot.contentRect.height;
					float w = evt.newRect.width > 0f ? evt.newRect.width : popupMinWidth;
					float h = evt.newRect.height;
					float l = targetLeft;
					if (pw > 0f && l + w > pw - margin) l = Mathf.Max(margin, pw - w - margin);
					popup.style.left = l;
					if (ph > 0f && topLeft.y + 2 + h > ph - margin) {
						Vector2 fieldTop = panelRoot.WorldToLocal(new Vector2(fieldBound.xMin, fieldBound.yMin));
						popup.style.top = Mathf.Max(margin, fieldTop.y - h - 2);
					}
				});

				openPopup = popup;
				dismissTree = field.panel?.visualTree;
				if (dismissTree != null) {
					dismissHandler = evt => {
						if (openPopup == null) return;
						if (evt.target is VisualElement targetVe && (openPopup.Contains(targetVe) || openPopup == targetVe)) return;
						bool clickedField = evt.target is VisualElement fieldVe && (fieldVe == field || field.Contains(fieldVe));
						ClosePopup();
						if (clickedField) suppressReopen = true;
					};
					wheelDismissHandler = evt => {
						if (openPopup == null) return;
						if (evt.target is VisualElement targetVe && (openPopup.Contains(targetVe) || openPopup == targetVe)) return;
						ClosePopup();
					};
					dismissTree.RegisterCallback(dismissHandler, TrickleDown.TrickleDown);
					dismissTree.RegisterCallback(wheelDismissHandler, TrickleDown.TrickleDown);
				}

				var triggerWindow = EditorWindow.focusedWindow;
				focusWatch = () => {
					if (EditorWindow.focusedWindow != triggerWindow) ClosePopup();
				};
				EditorApplication.update += focusWatch;
			}

			// Öffnet nur wenn geschlossen - schließen (auch bei erneutem Klick auf field) übernimmt dismissHandler.
			field.RegisterCallback<PointerDownEvent>(evt => {
				if (evt.button != 0) return;
				if (suppressReopen) { suppressReopen = false; return; }
				OpenPopup();
			});
			field.RegisterCallback<DetachFromPanelEvent>(_ => ClosePopup());

			return field;
		}

		/// <summary>Flaches Flags-Dropdown im Sperlich-Stil für <c>[Flags]</c> Enums (Checkboxen, bleibt offen, unterstützt Nothing/Everything).</summary>
		public static VisualElement CreateFlagsDropdown(SerializedProperty enumProp, Color? accent = null, Action<int> onChanged = null) {
			string[] names = enumProp.enumNames ?? System.Array.Empty<string>();
			string[] displayNames = enumProp.enumDisplayNames ?? names;

			string FormatFlagsLabel(int mask) {
				if (mask == 0) return "Nothing";
				var active = new List<string>();
				for (int i = 0; i < names.Length; i++) {
					int bitMask = 1 << i;
					if ((mask & bitMask) != 0) {
						active.Add(i < displayNames.Length && !string.IsNullOrEmpty(displayNames[i]) ? displayNames[i] : ObjectNames.NicifyVariableName(names[i]));
					}
				}
				if (active.Count == 0) return "Nothing";
				if (active.Count == names.Length) return "Everything";
				if (active.Count == 1) return active[0];
				return string.Join(", ", active);
			}

			// Options: 0: Nothing, 1: Everything, 2..(names.Length + 1): Individual flags
			int totalOptions = names.Length + 2;

			int AllMask() {
				int all = 0;
				for (int i = 0; i < names.Length; i++) all |= (1 << i);
				return all;
			}

			string GetOptionLabel(int index) {
				if (index == 0) return "Nothing";
				if (index == 1) return "Everything";
				int flagIdx = index - 2;
				if (flagIdx >= 0 && flagIdx < displayNames.Length && !string.IsNullOrEmpty(displayNames[flagIdx])) {
					return displayNames[flagIdx];
				}
				if (flagIdx >= 0 && flagIdx < names.Length) {
					return ObjectNames.NicifyVariableName(names[flagIdx]);
				}
				return "—";
			}

			bool IsOptionSelected(int index) {
				int mask = enumProp.intValue;
				if (index == 0) return mask == 0;
				if (index == 1) return mask != 0 && (mask & AllMask()) == AllMask();
				int bit = 1 << (index - 2);
				return (mask & bit) != 0;
			}

			void ToggleOption(int index) {
				int mask = enumProp.intValue;
				if (index == 0) {
					mask = 0;
				} else if (index == 1) {
					mask = AllMask();
				} else {
					int bit = 1 << (index - 2);
					if ((mask & bit) != 0) {
						mask &= ~bit;
					} else {
						mask |= bit;
					}
				}
				enumProp.intValue = mask;
				enumProp.serializedObject.ApplyModifiedProperties();
				onChanged?.Invoke(mask);
			}

			var dd = BuildMultiSelectDropdown(
				() => totalOptions,
				GetOptionLabel,
				IsOptionSelected,
				ToggleOption,
				() => FormatFlagsLabel(enumProp.intValue),
				accent,
				index => index < 2 ? null : (1 << (index - 2)).ToString()   // bit value badge (Nothing/Everything: none)
			);

			dd.TrackPropertyValue(enumProp, _ => {
				Label valLbl = dd.Q<Label>();
				if (valLbl != null) valLbl.text = FormatFlagsLabel(enumProp.intValue);
			});

			return dd;
		}

		/// <summary>Flaches Enum-Dropdown im Sperlich-Stil — ersetzt Unitys native Enum-Popups.</summary>
		/// <param name="enumType">Optional: der echte Enum-Typ. Ist er gesetzt, bekommt jede Zeile ein
		/// abgerundetes Kästchen mit dem dahinterliegenden Konstantenwert (z.B. <c>Priority.High = 10</c>).</param>
		public static VisualElement CreateEnumDropdown(SerializedProperty enumProp, Color? accent = null, Action<int> onChanged = null, Type enumType = null, bool sortAlphabetically = false) {
			// enumNames / enumDisplayNames throw ("type is not a enum value") if the SerializedProperty has
			// gone stale — e.g. an SDictionary key element after its backing list was rewritten by the
			// serialization callback. Fail soft instead of crashing the whole inspector.
			string[] SafeNames() { try { return enumProp.enumNames; } catch { return null; } }
			string[] SafeDisplay() { try { return enumProp.enumDisplayNames; } catch { return null; } }
			int SafeIndex() { try { return enumProp.enumValueIndex; } catch { return -1; } }

			string LabelFor(int index) {
				var display = SafeDisplay();
				if (display != null && index >= 0 && index < display.Length && string.IsNullOrEmpty(display[index]) == false) return display[index];
				var raw = SafeNames();
				if (raw != null && index >= 0 && index < raw.Length) return ObjectNames.NicifyVariableName(raw[index]);
				return "—";
			}

			Dictionary<string, long> valueByName = null;
			if (enumType != null && enumType.IsEnum) {
				valueByName = new Dictionary<string, long>();
				foreach (string n in Enum.GetNames(enumType)) valueByName[n] = Convert.ToInt64(Enum.Parse(enumType, n));
			}
			string BadgeFor(int index) {
				string[] raw = SafeNames();
				if (valueByName == null || raw == null || index < 0 || index >= raw.Length) return null;
				return valueByName.TryGetValue(raw[index], out long v) ? v.ToString() : null;
			}

			// Display-order permutation: identity normally, alphabetical-by-label when requested. Recomputed
			// per call (cheap — a handful of entries) so it stays correct if the property's option set changes.
			int[] DisplayOrder() {
				string[] names = SafeNames();
				if (names == null) return null;
				var order = new int[names.Length];
				for (int k = 0; k < order.Length; k++) order[k] = k;
				if (sortAlphabetically) Array.Sort(order, (a, b) => string.Compare(LabelFor(a), LabelFor(b), StringComparison.OrdinalIgnoreCase));
				return order;
			}

			var dd = BuildDropdown(
				() => DisplayOrder()?.Length ?? 0,
				displayIndex => { int[] order = DisplayOrder(); return LabelFor(order != null ? order[displayIndex] : displayIndex); },
				() => { int[] order = DisplayOrder(); return order != null ? Array.IndexOf(order, SafeIndex()) : SafeIndex(); },
				displayIndex => {
					try {
						int[] order = DisplayOrder();
						int actual = order != null ? order[displayIndex] : displayIndex;
						if (enumProp.enumValueIndex == actual) return;
						enumProp.enumValueIndex = actual;
						enumProp.serializedObject.ApplyModifiedProperties();
						onChanged?.Invoke(actual);
					} catch { /* stale property */ }
				},
				accent,
				valueByName != null ? (Func<int, string>)(displayIndex => { int[] order = DisplayOrder(); return BadgeFor(order != null ? order[displayIndex] : displayIndex); }) : null);
			dd.TrackPropertyValue(enumProp, _ => {
				Label valLbl = dd.Q<Label>();
				if (valLbl != null) valLbl.text = LabelFor(SafeIndex());
			});
			return dd;
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

				// Hover tint independent of Refresh()'s selected-state colors -- lightens the off state a touch,
				// intensifies the on (accent) state a touch, either way reverting to Refresh()'s own colors on leave.
				b.RegisterCallback<MouseEnterEvent>(_ => {
					bool on = (flagsProp.intValue & (1 << bit)) != 0;
					b.style.backgroundColor = on
						? new Color(accentColor.r, accentColor.g, accentColor.b, 0.26f)
						: Color.Lerp(SperlichEditorTheme.ButtonBg, Color.white, 0.07f);
				});
				b.RegisterCallback<MouseLeaveEvent>(_ => Refresh());
				ApplyHoverJuice(b, "background-color", "border-color");

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
			VisualElement field = null;
			if (prop != null) {
				if (prop.propertyType == SerializedPropertyType.Float || prop.propertyType == SerializedPropertyType.Integer) {
					field = CreateDragNumberField(prop);   // keeps the drag-to-scrub grip in compact clusters
				} else {
					var pf = new PropertyField(prop, " ");
					SperlichFieldColumn.HideInternalLabel(pf);
					field = pf;
				}
			}
			return CreateCompactField(caption, field, captionAbove);
		}

		/// <summary>Kompaktes Feld für Feld-Cluster mit benutzerdefiniertem Control.</summary>
		public static VisualElement CreateCompactField(string caption, VisualElement customControl, bool captionAbove = false) {
			var wrap = new VisualElement {
				style = {
					flexDirection = captionAbove ? UnityEngine.UIElements.FlexDirection.Column : UnityEngine.UIElements.FlexDirection.Row,
					alignItems = captionAbove ? Align.Stretch : Align.Center,
					marginRight = 6,
				}
			};
			var cap = new Label(caption) {
				style = {
					fontSize = 11, color = SperlichEditorTheme.TextMuted,
					marginRight = captionAbove ? 0 : 5, marginBottom = captionAbove ? 1 : 0,
					flexShrink = 0, unityTextAlign = TextAnchor.MiddleLeft,
				}
			};
			wrap.Add(cap);
			if (customControl != null) {
				customControl.style.flexGrow = 1;
				wrap.Add(customControl);
			}
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

		/// <summary>Sucht die Panel-Wurzel des EditorWindows, damit Overlays und Popups über allen Inspector-Elementen (inkl. IMGUI und Footern) gezeichnet werden.</summary>
		public static VisualElement ResolveOverlayRoot(VisualElement from) {
			if (from.panel != null && from.panel.visualTree != null) {
				return from.panel.visualTree;
			}
			VisualElement contentRoot = from;
			for (var p = from.hierarchy.parent; p != null; p = p.hierarchy.parent) {
				contentRoot = p;
			}
			return contentRoot;
		}

		/// <summary>Kleines abgerundetes Wert-Kästchen (z.B. Enum-Konstantenwert, Layer-Index) für den rechten
		/// Rand einer Dropdown-Zeile. <paramref name="selected"/> färbt es im Akzent-Ton statt neutral-grau.</summary>
		public static Label MakeIndexBadge(string text, bool selected) {
			var badge = new Label(text) {
				pickingMode = PickingMode.Ignore,
				style = {
					fontSize = 10, flexShrink = 0, marginLeft = 6,
					// Uniform box for up to two digits (0..99); longer numbers grow past this.
					minWidth = 22,
					paddingLeft = 5, paddingRight = 5, paddingTop = 1, paddingBottom = 1,
					unityTextAlign = TextAnchor.MiddleCenter,
					borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
					unityFont = EditorStyles.label?.font,
				}
			};
			SetRadius(badge, 5);
			StyleIndexBadge(badge, selected);
			return badge;
		}

		private static void StyleIndexBadge(Label badge, bool selected) {
			Color accent = SperlichEditorTheme.ButtonAccent;
			if (selected) {
				badge.style.backgroundColor = new Color(accent.r, accent.g, accent.b, 0.16f);
				SetBorderColor(badge, new Color(accent.r, accent.g, accent.b, 0.35f));
				badge.style.color = new Color(0.74f, 0.84f, 0.97f);
			} else {
				badge.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f);
				SetBorderColor(badge, new Color(1f, 1f, 1f, 0.13f));
				badge.style.color = SperlichEditorTheme.TextMuted;
			}
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
