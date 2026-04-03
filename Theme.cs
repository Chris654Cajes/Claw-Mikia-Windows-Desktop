using System.Drawing;

namespace MusicVault.Theme
{
    /// <summary>
    /// Cyberpunk neon theme colors for Music Vault
    /// </summary>
    public static class ThemeColors
    {
        // Primary Neon Colors
        public static readonly Color NeonPink = Color.FromArgb(250, 2, 77);      // #FA024D
        public static readonly Color NeonPurple = Color.FromArgb(171, 2, 250);   // #AB02FA
        public static readonly Color NeonBlue = Color.FromArgb(30, 0, 255);      // #1E00FF
        public static readonly Color NeonCyan = Color.FromArgb(0, 229, 255);     // #00E5FF
        public static readonly Color NeonGreen = Color.FromArgb(0, 255, 0);      // #00FF00
        public static readonly Color NeonYellow = Color.FromArgb(238, 255, 0);   // #EEFF00
        public static readonly Color NeonOrange = Color.FromArgb(255, 111, 0);   // #FF6F00
        public static readonly Color NeonRed = Color.FromArgb(255, 0, 0);        // #FF0000

        // Dark Theme Backgrounds
        public static readonly Color BgPrimary = Color.FromArgb(10, 10, 15);     // #0A0A0F
        public static readonly Color BgSurface = Color.FromArgb(18, 18, 26);     // #12121A
        public static readonly Color BgCard = Color.FromArgb(26, 26, 38);        // #1A1A26
        public static readonly Color BgHover = Color.FromArgb(35, 35, 50);       // #232332

        // Text Colors
        public static readonly Color TextPrimary = Color.FromArgb(240, 240, 255); // #F0F0FF
        public static readonly Color TextSecondary = Color.FromArgb(136, 136, 170); // #8888AA
        public static readonly Color TextHint = Color.FromArgb(68, 68, 90);      // #44445A
        
        // UI Elements
        public static readonly Color DividerColor = Color.FromArgb(30, 30, 46);  // #1E1E2E
        public static readonly Color SeekbarBg = Color.FromArgb(42, 42, 58);     // #2A2A3A
        public static readonly Color BorderColor = Color.FromArgb(50, 50, 70);   // #323246
    }

    public static class FontSizes
    {
        public const int TitleSize = 18;
        public const int HeadingSize = 14;
        public const int BodySize = 12;
        public const int SmallSize = 10;
    }

    public static class Spacing
    {
        public const int XSmall = 4;
        public const int Small = 8;
        public const int Medium = 12;
        public const int Large = 16;
        public const int XLarge = 24;
    }

    public static class Sizes
    {
        public const int TopAppBarHeight = 60;
        public const int SearchBarHeight = 50;
        public const int MiniPlayerHeight = 130;
        public const int BottomNavHeight = 60;
        public const int ButtonHeight = 36;
        public const int IconSize = 20;
        public const int TabHeight = 40;
    }

    public static class BorderRadius
    {
        public const int Small = 4;
        public const int Medium = 8;
        public const int Large = 12;
    }
}
