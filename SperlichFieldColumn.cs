using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>
	/// Baut Label+Control-Zeilen mit EINER gemeinsamen, pixelgenauen Label-Spalte für einen ganzen
	/// Inspektor. Ersetzt Unitys "aligned fields"-Automatik (<c>.unity-base-field__aligned</c> /
	/// <c>--unity-property-field-label-width</c>), die bei umgebauten Feldern und gemischten Zeilen
	/// (PropertyField neben Custom-Control) unterschiedlich weit einrückt. Jede Zeile — egal ob
	/// PropertyField, Enum-Dropdown oder PillToggle — beginnt das Control an exakt derselben X-Position.
	///
	/// Bei einem <see cref="Property"/>-Feld wird NICHT ein zweites Label davorgesetzt, sondern das
	/// vorhandene interne Feld-Label auf die Spaltenbreite gebracht und beschriftet. So bleibt bei
	/// Zahlenfeldern Unitys Ziehgriff (drag up/down über dem Label) erhalten und trotzdem sitzt das
	/// Eingabefeld exakt an derselben X-Position wie jedes andere Control.
	/// </summary>
	public sealed class SperlichFieldColumn {

		/// <summary>Feste Breite der Label-Spalte in Pixel. Alle Zeilen dieser Column nutzen denselben Wert.</summary>
		public float LabelWidth { get; }

		/// <summary>Einrück-Schritt in Pixel pro Indent-Stufe (verschiebt nur das Label, das Control bleibt in der Spalte).</summary>
		public float IndentStep { get; }

		public SperlichFieldColumn(float labelWidth = 130f, float indentStep = 12f) {
			LabelWidth = labelWidth;
			IndentStep = indentStep;
		}

		/// <summary>Zeile mit eigenem Label links (feste Spaltenbreite) und beliebigem Control rechts.</summary>
		public VisualElement Row(string label, VisualElement control, int indent = 0) {
			var row = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row, alignItems = Align.Center,
					minHeight = 20, marginTop = 1, marginBottom = 1,
				}
			};

			float w = ColumnWidth(indent);
			var lbl = new Label(label) {
				style = {
					width = w, minWidth = w, maxWidth = w,
					marginLeft = indent * IndentStep,
					flexShrink = 0,
					paddingLeft = 3,
					color = SperlichEditorTheme.TextSecondary,
					fontSize = 12,
					overflow = Overflow.Hidden,
					textOverflow = TextOverflow.Ellipsis,
					whiteSpace = WhiteSpace.NoWrap,
				}
			};

			var controlWrap = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
			control.style.flexGrow = 1;
			controlWrap.Add(control);

			row.Add(lbl);
			row.Add(controlWrap);
			return row;
		}

		/// <summary>Zeile mit einem Float-Slider und integriertem Zahlen-Eingabefeld.</summary>
		public VisualElement Slider(SerializedProperty prop, string label = null, float min = 0f, float max = 1f, int indent = 0) {
			if (prop == null) return new VisualElement();
			var s = new Slider(min, max) { showInputField = true, style = { flexGrow = 1 } };
			s.BindProperty(prop);
			HideInternalLabel(s);
			return Row(label ?? prop.displayName, s, indent);
		}

		/// <summary>Zeile mit einem Integer-Slider und integriertem Zahlen-Eingabefeld (snappt nur auf ganze Zahlen, unterstützt Int- und Float-Properties).</summary>
		public VisualElement Slider(SerializedProperty prop, string label, int min, int max, int indent = 0) {
			return SliderInt(prop, label, min, max, indent);
		}

		/// <summary>Zeile mit einem Integer-Slider und integriertem Zahlen-Eingabefeld (snappt nur auf ganze Zahlen, unterstützt Int- und Float-Properties).</summary>
		public VisualElement SliderInt(SerializedProperty prop, string label = null, int min = 0, int max = 10, int indent = 0) {
			if (prop == null) return new VisualElement();
			var s = new SliderInt(min, max) { showInputField = true, style = { flexGrow = 1 } };
			if (prop.propertyType == SerializedPropertyType.Integer) {
				s.BindProperty(prop);
			} else {
				s.value = Mathf.RoundToInt(prop.floatValue);
				s.RegisterValueChangedCallback(evt => {
					prop.floatValue = evt.newValue;
					prop.serializedObject.ApplyModifiedProperties();
				});
				s.TrackPropertyValue(prop, _ => {
					int target = Mathf.RoundToInt(prop.floatValue);
					if (s.value != target) s.value = target;
				});
			}
			HideInternalLabel(s);
			return Row(label ?? prop.displayName, s, indent);
		}

		/// <summary>Zeile mit einem Drag-Zahlenfeld und optionaler Min/Max-Begrenzung.</summary>
		public VisualElement DragNumber(SerializedProperty prop, string label = null, float min = float.MinValue, float max = float.MaxValue, int indent = 0) {
			if (prop == null) return new VisualElement();
			return Row(label ?? prop.displayName, SperlichEditorWidgets.CreateDragNumberField(prop, 1f, min, max), indent);
		}

		/// <summary>Zeile um ein <see cref="SerializedProperty"/>. Das interne Feld-Label wird auf die
		/// Spaltenbreite gebracht und mit <paramref name="label"/> beschriftet (bei Zahlenfeldern bleibt es
		/// so als Ziehgriff nutzbar). Für aufklappbare/verschachtelte Properties stattdessen <see cref="Raw"/>.</summary>
		public VisualElement Property(SerializedProperty prop, string label = null, int indent = 0) {
			if (prop == null) return new VisualElement();

			if (SperlichEditorWidgets.TryGetRange(prop, out float rMin, out float rMax)) {
				if (prop.propertyType == SerializedPropertyType.Integer) {
					return Slider(prop, label, (int)rMin, (int)rMax, indent);
				}
				return Slider(prop, label, rMin, rMax, indent);
			}

			// plain number fields get the drag-to-scrub grip
			bool plainNumber = (prop.propertyType == SerializedPropertyType.Float || prop.propertyType == SerializedPropertyType.Integer);
			if (plainNumber) {
				return Row(label ?? prop.displayName, SperlichEditorWidgets.CreateDragNumberField(prop), indent);
			}

			if (prop.propertyType == SerializedPropertyType.Boolean) {
				var pill = new PillToggle(prop.boolValue);
				pill.Clicked += () => {
					prop.boolValue = !prop.boolValue;
					prop.serializedObject.ApplyModifiedProperties();
					pill.SetValue(prop.boolValue);
				};
				var boolRow = Row(label ?? prop.displayName, pill, indent);
				boolRow.TrackPropertyValue(prop, sp => pill.SetValue(sp.boolValue));
				return boolRow;
			}

			if (prop.propertyType == SerializedPropertyType.Enum) {
				var dd = SperlichEditorWidgets.CreateEnumDropdown(prop);
				return Row(label ?? prop.displayName, dd, indent);
			}

			if (prop.propertyType == SerializedPropertyType.Color) {
				var cf = new ColorField { style = { flexGrow = 1 }, showAlpha = true };
				cf.BindProperty(prop);
				HideInternalLabel(cf);
				return Row(label ?? prop.displayName, cf, indent);
			}

			if (prop.propertyType == SerializedPropertyType.Gradient) {
				var gf = new GradientField { style = { flexGrow = 1 } };
				gf.BindProperty(prop);
				HideInternalLabel(gf);
				return Row(label ?? prop.displayName, gf, indent);
			}

			var pf = new PropertyField(prop, label ?? prop.displayName);
			float w = ColumnWidth(indent);
			float marginLeft = indent * IndentStep;
			ApplyColumnLabel(pf, w, marginLeft);

			var row = new VisualElement {
				style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, minHeight = 20, marginTop = 1, marginBottom = 1 }
			};
			pf.style.flexGrow = 1;
			row.Add(pf);
			return row;
		}

		/// <summary>Ein PropertyField über die volle Breite (für aufklappbare/verschachtelte Properties, die
		/// ihren eigenen Foldout-Header brauchen). Nur die Auto-Ausrichtung wird deaktiviert, damit die Zeile
		/// nicht gegenüber den <see cref="Property"/>-Zeilen verspringt.</summary>
		public static VisualElement Raw(SerializedProperty prop, string label = null) {
			if (prop == null) return new VisualElement();
			var pf = label == null ? new PropertyField(prop) : new PropertyField(prop, label);
			EventCallback<GeometryChangedEvent> cb = null;
			cb = _ => {
				DeAlign(pf);
				pf.UnregisterCallback(cb);
			};
			pf.RegisterCallback(cb);
			return pf;
		}

		private float ColumnWidth(int indent) => Mathf.Max(0f, LabelWidth - indent * IndentStep);

		/// <summary>Bringt das interne Feld-Label (nur das äußerste — innere X/Y/Z-Labels bleiben unangetastet)
		/// auf die Spaltenbreite und in den Sperlich-Stil, und hebt Unitys Auto-Ausrichtung auf. Der Callback
		/// wird nach dem ersten erfolgreichen Durchlauf abgemeldet, um unendliche Layout-Kaskaden zu vermeiden.</summary>
		public static void ApplyColumnLabel(VisualElement field, float width, float marginLeft) {
			EventCallback<GeometryChangedEvent> geoCallback = null;
			void Apply() {
				Label l = field.Q<Label>(className: "unity-base-field__label");
				if (l != null) {
					l.style.display = DisplayStyle.Flex;
					l.style.width = width;
					l.style.minWidth = width;
					l.style.maxWidth = width;
					l.style.marginLeft = marginLeft;
					l.style.marginRight = 0;
					l.style.paddingLeft = 3;
					l.style.color = SperlichEditorTheme.TextSecondary;
					l.style.fontSize = 12;
					l.style.unityFontStyleAndWeight = FontStyle.Normal;
					l.style.overflow = Overflow.Hidden;
					l.style.textOverflow = TextOverflow.Ellipsis;
					l.style.whiteSpace = WhiteSpace.NoWrap;
					if (geoCallback != null) {
						field.UnregisterCallback(geoCallback);
						geoCallback = null;
					}
				}
				DeAlign(field);
			}
			geoCallback = _ => Apply();
			field.RegisterCallback(geoCallback);
			field.schedule.Execute(Apply);
		}

		/// <summary>Blendet das interne Feld-Label komplett aus und hebt die Auto-Ausrichtung auf — für
		/// kompakte Cluster-Felder (Margins-/Spacing-Reihe), die ihre Beschriftung von außen bekommen.
		/// Der Callback wird nach dem ersten erfolgreichen Durchlauf abgemeldet.</summary>
		public static void HideInternalLabel(VisualElement field) {
			EventCallback<GeometryChangedEvent> geoCallback = null;
			void Apply() {
				Label l = field.Q<Label>(className: "unity-base-field__label");
				if (l != null) {
					l.style.display = DisplayStyle.None;
					l.style.width = 0;
					l.style.minWidth = 0;
					l.style.marginRight = 0;
					if (geoCallback != null) {
						field.UnregisterCallback(geoCallback);
						geoCallback = null;
					}
				}
				DeAlign(field);
			}
			geoCallback = _ => Apply();
			field.RegisterCallback(geoCallback);
			field.schedule.Execute(Apply);
		}

		private static void DeAlign(VisualElement field) {
			field.Query<VisualElement>(className: "unity-base-field__aligned")
				.ForEach(f => f.RemoveFromClassList("unity-base-field__aligned"));
		}
	}
}
