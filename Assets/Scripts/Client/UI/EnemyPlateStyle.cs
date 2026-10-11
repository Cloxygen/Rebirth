using UnityEngine;

public enum EnemyRank
{
    Normal,
    Elite,
    Boss
}

[System.Serializable]
public struct EnemyBarStyle
{
    public Color healthColor;

    public bool showChip;

    public Color chipColor;

    public Color trackColor;
    public float cornerRadius;

    public static EnemyBarStyle Default => new EnemyBarStyle
    {
        healthColor = Palette.Hp,
        showChip = true,
        chipColor = new Color(0x90 / 255f, 0x14 / 255f, 0x1a / 255f, 1f),
        trackColor = new Color(0x10 / 255f, 0x0c / 255f, 0x17 / 255f, 1f),
        cornerRadius = 4f,
    };
}
