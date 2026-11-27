using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.UI
{
    /// <summary>
    /// Shared presentation palette for The Broken Portal (see UI_STYLE_GUIDE.md).
    /// Presentation only: no gameplay, validation or progression code may depend on these values.
    /// </summary>
    public static class FypUiTheme
    {
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        // Core surfaces and text
        public static readonly Color Parchment = Hex("#F5E8CE");
        public static readonly Color ParchmentDeep = Hex("#F2E1B0");
        public static readonly Color ParchmentEdge = Hex("#E8CA92");
        public static readonly Color Ink = Hex("#302820");
        public static readonly Color InkSoft = Hex("#5C4B3B");
        public static readonly Color InkSlate = Hex("#2B2721");
        public static readonly Color InkSlateMuted = Hex("#A89F90");
        public static readonly Color MagicGold = Hex("#C6A15B");
        public static readonly Color GoldGlow = Hex("#FFD56A");
        public static readonly Color Wood = Hex("#B06049");
        public static readonly Color WoodDark = Hex("#6E3A2C");
        public static readonly Color Moss = Hex("#8E8948");
        public static readonly Color ForestGreen = Hex("#456C58");
        public static readonly Color Ember = Hex("#A4493A");
        public static readonly Color SkyBlue = Hex("#8CCCE6");
        public static readonly Color Disabled = new Color(0.66f, 0.62f, 0.56f, 0.6f);

        // Area accents
        public static readonly Color AccentHub = MagicGold;
        public static readonly Color AccentRuin1 = ForestGreen;
        public static readonly Color AccentRuin2 = Hex("#7A5AA6");
        public static readonly Color AccentFarm = SkyBlue;
        /// <summary>Opaque sky colour sampled from the Magical Farm skybox.</summary>
        public static readonly Color FarmSky = Hex("#9BDDF8");

        // Semantic states (fill tints are for parchment surfaces)
        public static readonly Color CorrectText = ForestGreen;
        public static readonly Color CorrectFill = Hex("#DCE8D2");
        public static readonly Color IncorrectText = Ember;
        public static readonly Color IncorrectFill = Hex("#F3D9CF");
        public static readonly Color HintFill = Hex("#DCEFF6");
        public static readonly Color InfoFill = Hex("#EFE3C8");

        // Farm plot states (match the existing world highlight meanings: harvested = blue, empty = amber)
        public static readonly Color FarmHarvested = SkyBlue;
        public static readonly Color FarmEmpty = Hex("#E0B066");

        /// <summary>Rich-text colour tag for the active code line.</summary>
        public const string ActiveLineTag = "<color=#FFD56A>";
        public const string LineNumberTag = "<color=#A89F90>";

        /// <summary>Standard button tint states (multipliers over the sprite colour).</summary>
        public static ColorBlock ButtonColors()
        {
            var block = ColorBlock.defaultColorBlock;
            block.normalColor = Color.white;
            block.highlightedColor = Hex("#FFF1CC");
            block.pressedColor = Hex("#D9C39A");
            block.selectedColor = Hex("#FFE7A8");
            block.disabledColor = Disabled;
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.08f;
            return block;
        }

        /// <summary>
        /// Dual-font system: Vanilla Caramel SDF for fantasy UI, Cascadia Mono SDF
        /// (Assets/Font/Cascadia) for code. True for a monospaced code font, which needs no
        /// artificial &lt;mspace&gt; formatting.
        /// </summary>
        public static bool IsMonospace(TMPro.TMP_FontAsset font) =>
            font != null && (font.name.IndexOf("Cascadia", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                             font.name.IndexOf("Mono", System.StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Replaces characters that Vanilla Caramel SDF cannot render (U+2026, U+2022, U+00B7) so
        /// dynamic text such as AI hints never silently falls back to another font.
        /// </summary>
        public static string ForFont(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("…", "...").Replace("•", "|").Replace("·", "|");
        }
    }
}
