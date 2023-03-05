using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fluXis.Game.Map;

public class MapInfo
{
    public string ID { get; set; }
    public string MD5 { get; set; }
    public string AudioFile { get; set; }
    public string BackgroundFile { get; set; }
    public string VideoFile { get; set; }
    public MapMetadata Metadata { get; set; }
    public List<HitObjectInfo> HitObjects;
    public List<TimingPointInfo> TimingPoints;
    public List<ScrollVelocityInfo> ScrollVelocities;
    public List<EventInfo> Events;

    [JsonIgnore]
    public float StartTime => HitObjects[0].Time;

    [JsonIgnore]
    public float EndTime => HitObjects[^1].HoldEndTime;

    [JsonIgnore]
    public int MaxCombo
    {
        get
        {
            int maxCombo = 0;

            foreach (var hitObject in HitObjects)
            {
                maxCombo++;
                if (hitObject.IsLongNote())
                    maxCombo++;
            }

            return maxCombo;
        }
    }

    [JsonIgnore]
    public int KeyCount;

    [JsonIgnore]
    public int InitialKeyCount;

    public MapInfo(MapMetadata metadata)
    {
        ID = "";
        Metadata = metadata;
        HitObjects = new List<HitObjectInfo>();
        TimingPoints = new List<TimingPointInfo>();
        ScrollVelocities = new List<ScrollVelocityInfo>();
        Events = new List<EventInfo>();
    }

    public bool Validate()
    {
        if (HitObjects.Count == 0)
            return false;
        if (TimingPoints.Count == 0)
            return false;

        foreach (var hitObject in HitObjects)
        {
            KeyCount = Math.Max(KeyCount, hitObject.Lane);
        }

        foreach (var timingPoint in TimingPoints)
        {
            if (timingPoint.BPM <= 0)
                return false;

            if (timingPoint.Time < 0)
                return false;

            if (timingPoint.Signature < 0) { }
        }

        if (Events != null)
        {
            foreach (var mapEvent in Events)
            {
                switch (mapEvent.Type)
                {
                    case "laneswitch":
                        if (InitialKeyCount == 0)
                            InitialKeyCount = mapEvent.Value;

                        KeyCount = Math.Max(KeyCount, mapEvent.Value);
                        break;
                }
            }
        }

        if (InitialKeyCount == 0)
            InitialKeyCount = KeyCount;

        return KeyCount is > 3 and < 8;
    }

    public void Sort()
    {
        HitObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
        TimingPoints.Sort((a, b) => a.Time.CompareTo(b.Time));
        ScrollVelocities?.Sort((a, b) => a.Time.CompareTo(b.Time));
        Events?.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    public TimingPointInfo GetTimingPoint(float time)
    {
        TimingPointInfo timingPoint = null;

        foreach (var tp in TimingPoints)
        {
            if (tp.Time > time)
                break;

            timingPoint = tp;
        }

        return timingPoint ?? TimingPoints[0];
    }

    public MapInfo Clone()
    {
        return new MapInfo(Metadata)
        {
            ID = ID,
            MD5 = MD5,
            AudioFile = AudioFile,
            BackgroundFile = BackgroundFile,
            HitObjects = HitObjects,
            TimingPoints = TimingPoints,
            ScrollVelocities = ScrollVelocities,
            Events = Events,
            KeyCount = KeyCount,
            InitialKeyCount = InitialKeyCount
        };
    }
}
