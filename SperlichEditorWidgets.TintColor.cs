using UnityEngine;

namespace Sperlich.EditorKit {

	public static partial class SperlichEditorWidgets {

		/// <summary>Maps a <see cref="Sperlich.EditorKit.TintColor"/> preset to an actual color — kept here (not
		/// on the enum, C# can't put logic there). <see cref="Sperlich.EditorKit.TintColor.None"/> maps to
		/// white as a harmless fallback; callers should check for <c>None</c> themselves before resolving.</summary>
		public static Color TintColorToColor(TintColor color) => color switch {
			TintColor.White => new Color(0.92f, 0.92f, 0.92f),
			TintColor.Black => new Color(0.08f, 0.08f, 0.09f),
			TintColor.Gray => new Color(0.55f, 0.55f, 0.55f),
			TintColor.LightGray => new Color(0.75f, 0.75f, 0.75f),
			TintColor.DarkGray => new Color(0.32f, 0.32f, 0.34f),
			TintColor.Brown => new Color(0.55f, 0.38f, 0.27f),
			TintColor.Red => new Color(0.85f, 0.35f, 0.34f),
			TintColor.DarkRed => new Color(0.60f, 0.20f, 0.20f),
			TintColor.Orange => new Color(0.88f, 0.58f, 0.28f),
			TintColor.Amber => new Color(0.92f, 0.70f, 0.25f),
			TintColor.Yellow => new Color(0.85f, 0.78f, 0.30f),
			TintColor.Lime => new Color(0.68f, 0.82f, 0.35f),
			TintColor.Green => new Color(0.42f, 0.76f, 0.48f),
			TintColor.DarkGreen => new Color(0.25f, 0.50f, 0.30f),
			TintColor.Teal => new Color(0.20f, 0.65f, 0.55f),
			TintColor.Cyan => new Color(0.27f, 0.69f, 0.79f),
			TintColor.SkyBlue => new Color(0.40f, 0.68f, 0.90f),
			TintColor.Blue => new Color(0.35f, 0.55f, 0.90f),
			TintColor.DarkBlue => new Color(0.22f, 0.32f, 0.65f),
			TintColor.Indigo => new Color(0.40f, 0.35f, 0.75f),
			TintColor.Purple => new Color(0.58f, 0.45f, 0.82f),
			TintColor.Violet => new Color(0.68f, 0.45f, 0.85f),
			TintColor.Magenta => new Color(0.82f, 0.35f, 0.75f),
			TintColor.Pink => new Color(0.85f, 0.45f, 0.65f),
			TintColor.Maroon => new Color(0.50f, 0.25f, 0.32f),
			_ => new Color(0.92f, 0.92f, 0.92f), // None / unknown — harmless fallback, callers should check None first
		};

		/// <summary>Shared resolution order for the "hex string OR <see cref="TintColor"/> palette" pattern used
		/// across attributes (<c>[HLine]</c>, <c>[ProgressBar]</c>, <c>[Tag]</c>, <c>[AccentColor]</c>, ...):
		/// an explicit hex string wins, then the palette entry, otherwise <c>null</c> (caller decides the
		/// fallback, e.g. a theme colour).</summary>
		public static Color? ResolveColor(string hex, TintColor tint) {
			if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
			if (tint != TintColor.None) return TintColorToColor(tint);
			return null;
		}
	}
}
