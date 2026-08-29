using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	/// <summary>Kompakter An/Aus-Schalter im Sperlich-Editor-Stil, ersetzt Unitys Standard-Checkbox in Custom-Inspektoren (ursprünglich aus AnimSequencerEditor portiert).</summary>
	public class PillToggle : VisualElement {

		public event Action Clicked;

		private readonly VisualElement pill;
		private readonly VisualElement knob;
		private readonly Color onBg;
		private readonly Color offBg;
		private bool currentValue;
		private bool isHovered;

		public PillToggle(bool value, Color? onBg = null, Color? offBg = null) {
			this.onBg = onBg ?? SperlichEditorTheme.ToggleOnBg;
			this.offBg = offBg ?? SperlichEditorTheme.ToggleOffBg;

			pill = new VisualElement();
			pill.style.width = 30;
			pill.style.height = 14;
			SperlichEditorWidgets.SetRadius(pill, 7);
			pill.style.flexShrink = 0;
			pill.style.position = Position.Relative;

			knob = new VisualElement();
			knob.style.width = 10;
			knob.style.height = 10;
			SperlichEditorWidgets.SetRadius(knob, 5);
			knob.style.backgroundColor = Color.white;
			knob.style.position = Position.Absolute;
			knob.style.top = 2;

			pill.Add(knob);
			Add(pill);
			SperlichEditorWidgets.SetHoverCursor(this, UnityEditor.MouseCursor.Link);

			// Startzustand ohne Transition setzen (kein Animieren beim ersten Aufbau), Transition erst danach aktivieren.
			ApplyValue(value);
			var duration = new List<TimeValue> { new TimeValue(120, TimeUnit.Millisecond) };
			var easing = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) };
			pill.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("background-color") };
			pill.style.transitionDuration = duration;
			pill.style.transitionTimingFunction = easing;
			knob.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("left") };
			knob.style.transitionDuration = duration;
			knob.style.transitionTimingFunction = easing;

			RegisterCallback<ClickEvent>(evt => {
				evt.StopPropagation();
				Clicked?.Invoke();
			});
			RegisterCallback<MouseEnterEvent>(_ => { isHovered = true; ApplyValue(currentValue); });
			RegisterCallback<MouseLeaveEvent>(_ => { isHovered = false; ApplyValue(currentValue); });
		}

		public void SetValue(bool value) => ApplyValue(value);

		private void ApplyValue(bool value) {
			currentValue = value;
			Color baseColor = value ? onBg : offBg;
			pill.style.backgroundColor = isHovered ? Color.Lerp(baseColor, Color.white, 0.15f) : baseColor;
			knob.style.left = value ? 18 : 2;
		}
	}
}
