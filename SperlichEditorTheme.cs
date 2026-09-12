using UnityEngine;

namespace Sperlich.EditorKit {

	/// <summary>Kanonische Farbpalette für alle Sperlich-Package-Editor-Inspektoren (dunkles, blaugraues "Neon-Dashboard"-Design, ursprünglich im AnimSequencer-Inspector entwickelt).</summary>
	public static class SperlichEditorTheme {

		public static readonly Color BgDark = new Color(0.14f, 0.14f, 0.14f);
		public static readonly Color BgStep = new Color(0.19f, 0.19f, 0.19f);
		public static readonly Color BgStepBody = new Color(0.15f, 0.15f, 0.15f);
		/// <summary>Heller und stärker desaturierter als BgStep/BgStepBody — kanonischer Hintergrund für Foldout-/Chevron-Section-Bodies, damit die nie auf Unitys unstyled Default-Grau zurückfallen.</summary>
		public static readonly Color BgPanel = new Color(0.17f, 0.17f, 0.17f);

		public static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.08f);
		public static readonly Color BorderStrong = new Color(1f, 1f, 1f, 0.15f);

		public static readonly Color TextPrimary = Color.white;
		public static readonly Color TextSecondary = new Color(0.80f, 0.80f, 0.80f);
		public static readonly Color TextMuted = new Color(0.58f, 0.58f, 0.58f);
		public static readonly Color TextFaint = new Color(0.48f, 0.48f, 0.48f);

		public static readonly Color ButtonBg = new Color(0.22f, 0.22f, 0.22f);
		public static readonly Color ButtonHoverBg = new Color(0.28f, 0.28f, 0.28f);
		public static readonly Color ButtonBorder = new Color(1f, 1f, 1f, 0.11f);
		public static readonly Color ButtonAccent = new Color(96f / 255f, 165f / 255f, 250f / 255f);
		public static readonly Color ToggleOnBg = new Color(59f / 255f, 130f / 255f, 246f / 255f);
		public static readonly Color ToggleOffBg = new Color(0.23f, 0.23f, 0.23f);

		public static readonly Color BadgeInfoBg = new Color(0.15f, 0.45f, 0.85f);
		public static readonly Color BadgeWarnBg = new Color(0.85f, 0.55f, 0.15f);
		public static readonly Color BadgeDangerBg = new Color(0.85f, 0.2f, 0.2f);
		public static readonly Color BadgeNeutralBg = new Color(0.20f, 0.20f, 0.20f);
	}
}
