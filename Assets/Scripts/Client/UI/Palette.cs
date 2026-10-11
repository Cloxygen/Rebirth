using UnityEngine;

public static class Palette
{
    private static Color H(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);

    public static readonly Color Stage0 = H(0x0b, 0x0a, 0x0e);
    public static readonly Color Stage1 = H(0x14, 0x11, 0x19);
    public static readonly Color Stage2 = H(0x1d, 0x18, 0x26);

    public static readonly Color MetalHi = H(0xc3, 0xb0, 0x88);
    public static readonly Color Metal = H(0x8a, 0x7a, 0x5c);
    public static readonly Color MetalLo = H(0x2f, 0x28, 0x19);
    public static readonly Color MetalLine = H(0x4a, 0x3f, 0x2b);

    public static readonly Color HpHi = H(0xe7, 0x41, 0x4a);
    public static readonly Color Hp = H(0xb0, 0x18, 0x20);
    public static readonly Color HpLo = H(0x4d, 0x07, 0x0c);

    public static readonly Color RsHi = H(0x57, 0xb6, 0xf2);
    public static readonly Color Rs = H(0x1f, 0x6f, 0xc4);
    public static readonly Color RsLo = H(0x0a, 0x25, 0x49);

    public static readonly Color EssHi = H(0xfd, 0xfa, 0xf0);
    public static readonly Color Ess = H(0xd8, 0xcf, 0xa6);
    public static readonly Color EssLo = H(0x8f, 0x85, 0x60);

    public static readonly Color Gold = H(0xee, 0xba, 0x4d);
    public static readonly Color GoldLo = H(0x8a, 0x64, 0x12);
    public static readonly Color Buff = H(0x67, 0xc4, 0x6f);
    public static readonly Color Debuff = H(0xb8, 0x4e, 0xe0);
    public static readonly Color Hex = H(0xd8, 0x40, 0x40);
    public static readonly Color Ink = H(0xef, 0xe7, 0xd6);
    public static readonly Color InkDim = H(0x9c, 0x94, 0x84);

    public static readonly Color Orn = H(0xc3, 0xb0, 0x88, 0.3f);

    public static readonly Color BarHi = H(0x2a, 0x23, 0x18);
    public static readonly Color BarLo = H(0x14, 0x10, 0x09);
    public static readonly Color Slot = H(0x1c, 0x16, 0x26);
    public static readonly Color SlotLine = H(0x0b, 0x08, 0x10);
}
