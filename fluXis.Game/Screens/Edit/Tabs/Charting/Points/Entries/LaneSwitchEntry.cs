using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fluXis.Game.Graphics.Sprites;
using fluXis.Game.Map.Events;
using fluXis.Game.Screens.Edit.Tabs.Shared.Points.List;
using fluXis.Game.Screens.Edit.Tabs.Shared.Points.Settings;
using fluXis.Game.Utils;
using osu.Framework.Graphics;

namespace fluXis.Game.Screens.Edit.Tabs.Charting.Points.Entries;

public partial class LaneSwitchEntry : PointListEntry
{
    protected override string Text => "Lane Switch";
    protected override Colour4 Color => Colour4.FromHex("#FF6666");

    private LaneSwitchEvent laneSwitch => Object as LaneSwitchEvent;
    private float beatLength => Map.MapInfo.GetTimingPoint(laneSwitch.Time).MsPerBeat;

    public LaneSwitchEntry(LaneSwitchEvent laneSwitch)
        : base(laneSwitch)
    {
    }

    protected override Drawable[] CreateValueContent()
    {
        return new Drawable[]
        {
            new FluXisSpriteText
            {
                Text = $"{laneSwitch.Count}K {(int)laneSwitch.Speed}ms",
                Colour = Color
            }
        };
    }

    protected override IEnumerable<Drawable> CreateSettings()
    {
        return base.CreateSettings().Concat(new Drawable[]
        {
            new PointSettingsTextBox
            {
                Text = "Key Count",
                ExtraText = "K",
                TextBoxWidth = 50,
                DefaultText = laneSwitch.Count.ToString(),
                OnTextChanged = box =>
                {
                    if (int.TryParse(box.Text, out var result))
                    {
                        if (result > Map.MapInfo.Map!.KeyCount || result < 1)
                            box.NotifyError();
                        else
                            laneSwitch.Count = result;
                    }
                    else
                        box.NotifyError();

                    Map.Update(laneSwitch);
                }
            },
            new PointSettingsTextBox
            {
                Text = "Animation Length",
                ExtraText = "beat(s)",
                TextBoxWidth = 100,
                DefaultText = (laneSwitch.Speed / beatLength).ToStringInvariant(),
                OnTextChanged = box =>
                {
                    if (float.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                        laneSwitch.Speed = result * beatLength;
                    else
                        box.NotifyError();

                    Map.Update(laneSwitch);
                }
            }
        });
    }
}