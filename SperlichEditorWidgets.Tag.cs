using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Small colored pill for <c>[Tag]</c>.</summary>
		public static VisualElement CreateTagPill(string label, TagColor color) {
			var pill = new Label(label) {
				pickingMode = PickingMode.Ignore,
				style = {
					fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold,
					color = TagTextColor(color),
					backgroundColor = TagColorToColor(color),
					paddingLeft = 5, paddingRight = 5, paddingTop = 1, paddingBottom = 1,
					marginLeft = 5, flexShrink = 0, unityTextAlign = TextAnchor.MiddleCenter,
				}
			};
			SetRadius(pill, 8);
			return pill;
		}

		/// <summary>Maps a <see cref="TagColor"/> preset to an actual color — kept here (not on the enum,
		/// which lives in the Attributes assembly) because <c>Color</c> can't be an attribute argument.</summary>
		public static Color TagColorToColor(TagColor color) => color switch {
			TagColor.Gray => new Color(0.55f, 0.55f, 0.55f),
			TagColor.Blue => new Color(0.35f, 0.55f, 0.9f),
			TagColor.Cyan => new Color(0.27f, 0.69f, 0.79f),
			TagColor.Teal => new Color(0.20f, 0.65f, 0.55f),
			TagColor.Green => new Color(0.42f, 0.76f, 0.48f),
			TagColor.Yellow => new Color(0.85f, 0.75f, 0.30f),
			TagColor.Orange => new Color(0.88f, 0.58f, 0.28f),
			TagColor.Red => new Color(0.85f, 0.38f, 0.36f),
			TagColor.Pink => new Color(0.85f, 0.45f, 0.65f),
			TagColor.Purple => new Color(0.58f, 0.45f, 0.82f),
			_ => SperlichEditorTheme.ButtonAccent,
		};

		/// <summary>Dark text reads on every preset in this palette — all presets are mid-to-light tones.</summary>
		private static Color TagTextColor(TagColor color) => new Color(0.08f, 0.09f, 0.1f);
	}
}
