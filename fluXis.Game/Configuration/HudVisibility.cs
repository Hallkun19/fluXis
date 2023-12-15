using System.ComponentModel;

namespace fluXis.Game.Configuration;

public enum HudVisibility
{
    [Description("Always")]
    Always,

    [Description("Never")]
    Hidden,

    [Description("Show During Breaks")]
    ShowDuringBreaks,

    [Description("Show During Gameplay")]
    ShowDuringGameplay
}
