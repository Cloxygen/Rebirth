using System.Collections.Generic;
using UnityEngine;

public class TooltipContent
{
    public string title;
    public Color titleColor = Color.white;
    public string rarity;
    public string type;
    public string subtype;
    public readonly List<string> lines = new List<string>();
    public string description;

    public void Clear()
    {
        title = null;
        titleColor = Color.white;
        rarity = null;
        type = null;
        subtype = null;
        lines.Clear();
        description = null;
    }
}
