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

		/// <summary>Zeile um ein <see cref="SerializedProperty"/>. Das interne Feld-Label wird auf die
		/// Spaltenbreite gebracht und mit <paramref name="label"/> beschriftet (bei Zahlenfeldern bleibt es
		/// so als Ziehgriff nutzbar). Für aufklappbare/verschachtelte Properties stattdessen <see cref="Raw"/>.</summary>
		public VisualElement Property(SerializedProperty prop, string label = null, int indent = 0) {
			if (prop == null) return new VisualElement();

			// plain number fields get the drag-to-scrub grip; Range fields keep Unity's slider
			bool plainNumber = (prop.propertyType == SerializedPropertyType.Float || prop.propertyType == SerializedPropertyType.Integer)
				&& SperlichEditorWidgets.PropertyHasRange(prop) == false;
			if (plainNumber) {
				return Row(label ?? prop.displayName, SperlichEditorWidgets.CreateDragNumberField(prop), indent);
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
			pf.RegisterCallback<GeometryChangedEvent>(_ => DeAlign(pf));
			return pf;
		}

		private float ColumnWidth(int indent) => Mathf.Max(0f, LabelWidth - indent * IndentStep);

		/// <summary>Bringt das interne Feld-Label (nur das äußerste — innere X/Y/Z-Labels bleiben unangetastet)
		/// auf die Spaltenbreite und in den Sperlich-Stil, und hebt Unitys Auto-Ausrichtung auf. Läuft bei jedem
		/// Geometry-Pass erneut, da PropertyField seine Kinder verzögert (und je nach Typ nachträglich) erzeugt.</summary>
		public static void ApplyColumnLabel(VisualElement field, float width, float marginLeft) {
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
				}
				DeAlign(field);
			}
			field.RegisterCallback<GeometryChangedEvent>(_ => Apply());
			field.schedule.Execute(Apply);
		}

		/// <summary>Blendet das interne Feld-Label komplett aus und hebt die Auto-Ausrichtung auf — für
		/// kompakte Cluster-Felder (Margins-/Spacing-Reihe), die ihre Beschriftung von außen bekommen.</summary>
		public static void HideInternalLabel(VisualElement field) {
			void Apply() {
				Label l = field.Q<Label>(className: "unity-base-field__label");
				if (l != null) {
					l.style.display = DisplayStyle.None;
					l.style.width = 0;
					l.style.minWidth = 0;
					l.style.marginRight = 0;
				}
				DeAlign(field);
			}
			field.RegisterCallback<GeometryChangedEvent>(_ => Apply());
			field.schedule.Execute(Apply);
		}

		private static void DeAlign(VisualElement field) {
			field.Query<VisualElement>(className: "unity-base-field__aligned")
				.ForEach(f => f.RemoveFromClassList("unity-base-field__aligned"));
		}
	}
}
