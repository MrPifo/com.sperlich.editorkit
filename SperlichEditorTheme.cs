using UnityEngine;

namespace Sperlich.EditorKit {

	/// <summary>Kanonische Farbpalette für alle Sperlich-Package-Editor-Inspektoren (dunkles, blaugraues "Neon-Dashboard"-Design, ursprünglich im AnimSequencer-Inspector entwickelt).</summary>
	public static class SperlichEditorTheme {

		public static readonly Color BgDark = new Color(0.12f, 0.13f, 0.16f);
		public static readonly Color BgStep = new Color(0.17f, 0.18f, 0.22f);
		public static readonly Color BgStepBody = new Color(0.14f, 0.15f, 0.18f);
		/// <summary>Heller und stärker desaturierter als BgStep/BgStepBody — kanonischer Hintergrund für Foldout-/Chevron-Section-Bodies, damit die nie auf Unitys unstyled Default-Grau zurückfallen.</summary>
		public static readonly Color BgPanel = new Color(0.195f, 0.20f, 0.225f);

		public static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.08f);
		public static readonly Color BorderStrong = new Color(1f, 1f, 1f, 0.16f);

		public static readonly Color TextPrimary = Color.white;
		public static readonly Color TextSecondary = new Color(0.78f, 0.78f, 0.78f);
		public static readonly Color TextMuted = new Color(0.55f, 0.55f, 0.55f);
		public static readonly Color TextFaint = new Color(0.45f, 0.45f, 0.45f);

		public static readonly Color ButtonBg = new Color(0.22f, 0.23f, 0.27f);
		public static readonly Color ButtonHoverBg = new Color(0.28f, 0.30f, 0.38f);
		public static readonly Color ButtonBorder = new Color(0.35f, 0.38f, 0.45f, 0.3f);
		public static readonly Color ButtonAccent = new Color(0.30f, 0.90f, 0.50f);

		public static readonly Color ToggleOnBg = new Color(0.25f, 0.75f, 0.65f);
		public static readonly Color ToggleOffBg = new Color(0.25f, 0.25f, 0.30f);

		public static readonly Color BadgeInfoBg = new Color(0.15f, 0.45f, 0.85f);
		public static readonly Color BadgeWarnBg = new Color(0.85f, 0.55f, 0.15f);
		public static readonly Color BadgeDangerBg = new Color(0.85f, 0.2f, 0.2f);
		public static readonly Color BadgeNeutralBg = new Color(0.30f, 0.32f, 0.38f);
	}
}
