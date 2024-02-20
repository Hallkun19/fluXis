using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fluXis.Game.Map.Events;
using fluXis.Game.Utils;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Logging;

namespace fluXis.Game.Map;

public class MapEvents
{
    public List<LaneSwitchEvent> LaneSwitchEvents { get; init; } = new();
    public List<FlashEvent> FlashEvents { get; init; } = new();
    public List<PulseEvent> PulseEvents { get; init; } = new();
    public List<PlayfieldMoveEvent> PlayfieldMoveEvents { get; init; } = new();
    public List<PlayfieldScaleEvent> PlayfieldScaleEvents { get; init; } = new();
    public List<ShakeEvent> ShakeEvents { get; init; } = new();
    public List<PlayfieldFadeEvent> PlayfieldFadeEvents { get; init; } = new();
    public List<ShaderEvent> ShaderEvents { get; init; } = new();

    public MapEvents Load(string content)
    {
        var lines = content.Split(Environment.NewLine);

        foreach (var line in lines)
        {
            int index = line.IndexOf('(');
            int index2 = line.IndexOf(')');
            if (index == -1 || index2 == -1) continue;

            var type = line[..index];
            var args = line[(index + 1)..index2].Split(',');

            switch (type)
            {
                case "LaneSwitch":
                {
                    var laneSwitch = new LaneSwitchEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture),
                        Count = int.Parse(args[1])
                    };

                    if (args.Length > 2)
                        laneSwitch.Speed = float.Parse(args[2], CultureInfo.InvariantCulture);

                    LaneSwitchEvents.Add(laneSwitch);
                    break;
                }

                case "Flash":
                    if (args.Length < 8) continue;

                    float duration = float.Parse(args[1], CultureInfo.InvariantCulture);
                    bool inBackground = args[2] == "true";
                    Easing easing = (Easing)Enum.Parse(typeof(Easing), args[3]);
                    Colour4 startColor = Colour4.FromHex(args[4]);
                    float startOpacity = float.Parse(args[5], CultureInfo.InvariantCulture);
                    Colour4 endColor = Colour4.FromHex(args[6]);
                    float endOpacity = float.Parse(args[7], CultureInfo.InvariantCulture);

                    FlashEvents.Add(new FlashEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture),
                        Duration = duration,
                        InBackground = inBackground,
                        Easing = easing,
                        StartColor = startColor,
                        StartOpacity = startOpacity,
                        EndColor = endColor,
                        EndOpacity = endOpacity
                    });
                    break;

                case "Pulse":
                    PulseEvents.Add(new PulseEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture)
                    });
                    break;

                case "PlayfieldMove":
                    if (args.Length < 4) continue;

                    PlayfieldMoveEvents.Add(new PlayfieldMoveEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture),
                        OffsetX = float.Parse(args[1], CultureInfo.InvariantCulture),
                        Duration = float.Parse(args[2], CultureInfo.InvariantCulture),
                        Easing = Enum.TryParse<Easing>(args[3], out var ease) ? ease : Easing.None
                    });
                    break;

                case "PlayfieldScale":
                    if (args.Length < 5) continue;

                    PlayfieldScaleEvents.Add(new PlayfieldScaleEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture),
                        ScaleX = float.Parse(args[1], CultureInfo.InvariantCulture),
                        ScaleY = float.Parse(args[2], CultureInfo.InvariantCulture),
                        Duration = float.Parse(args[3], CultureInfo.InvariantCulture),
                        Easing = Enum.TryParse<Easing>(args[4], out var ease2) ? ease2 : Easing.None
                    });
                    break;

                case "Shake":
                    if (args.Length < 3) continue;

                    ShakeEvents.Add(new ShakeEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture),
                        Duration = float.Parse(args[1], CultureInfo.InvariantCulture),
                        Magnitude = float.Parse(args[2], CultureInfo.InvariantCulture)
                    });
                    break;

                case "PlayfieldFade":
                    if (args.Length < 3) continue;

                    PlayfieldFadeEvents.Add(new PlayfieldFadeEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture),
                        FadeTime = float.Parse(args[1], CultureInfo.InvariantCulture),
                        Alpha = float.Parse(args[2], CultureInfo.InvariantCulture)
                    });
                    break;

                case "Shader":
                    if (args.Length < 3)
                        continue;

                    var startIdx = line.IndexOf('{');
                    var endIdx = line.LastIndexOf('}');

                    if (startIdx == -1 || endIdx == -1)
                        continue;

                    var dataJson = line[startIdx..(endIdx + 1)];
                    Logger.Log(dataJson);
                    var data = dataJson.Deserialize<JObject>();

                    ShaderEvents.Add(new ShaderEvent
                    {
                        Time = float.Parse(args[0], CultureInfo.InvariantCulture),
                        ShaderName = args[1],
                        ShaderParams = data
                    });
                    break;
            }
        }

        sort();

        return this;
    }

    private void sort()
    {
        LaneSwitchEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        FlashEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        PulseEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        PlayfieldMoveEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        PlayfieldScaleEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        ShakeEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        PlayfieldFadeEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        ShaderEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    public string Save()
    {
        sort();

        var content = "";
        content += LaneSwitchEvents.Aggregate(string.Empty, (current, laneSwitch) => current + (laneSwitch + Environment.NewLine));
        content += FlashEvents.Aggregate(string.Empty, (current, flash) => current + (flash + Environment.NewLine));
        content += PulseEvents.Aggregate(string.Empty, (current, pulse) => current + (pulse + Environment.NewLine));
        content += PlayfieldMoveEvents.Aggregate(string.Empty, (current, playfieldMove) => current + (playfieldMove + Environment.NewLine));
        content += PlayfieldScaleEvents.Aggregate(string.Empty, (current, playfieldScale) => current + (playfieldScale + Environment.NewLine));
        content += ShakeEvents.Aggregate(string.Empty, (current, shake) => current + (shake + Environment.NewLine));
        content += PlayfieldFadeEvents.Aggregate(string.Empty, (current, playfieldFade) => current + (playfieldFade + Environment.NewLine));
        content += ShaderEvents.Aggregate(string.Empty, (current, shader) => current + (shader + Environment.NewLine));
        return content;
    }
}
