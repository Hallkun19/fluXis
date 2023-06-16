using System;
using System.Collections.Generic;
using System.Linq;
using fluXis.Game.Audio;
using fluXis.Game.Configuration;
using fluXis.Game.Map;
using fluXis.Game.Map.Events;
using fluXis.Game.Mods;
using fluXis.Game.Scoring;
using fluXis.Game.Screens.Gameplay.Input;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace fluXis.Game.Screens.Gameplay.Ruleset;

public partial class HitObjectManager : Container<HitObject>
{
    [Resolved]
    private AudioClock clock { get; set; }

    private Bindable<float> scrollSpeed;
    public float ScrollSpeed => scrollSpeed.Value;
    public Playfield Playfield { get; }
    public Performance Performance { get; }
    public MapInfo Map { get; set; }
    public List<HitObjectInfo> FutureHitObjects { get; } = new();
    public List<HitObject> HitObjects { get; } = new();

    public double CurrentTime { get; private set; }
    public int CurrentKeyCount { get; private set; }
    public LaneSwitchEvent CurrentLaneSwitchEvent { get; private set; }

    public bool Dead { get; set; }
    public bool SkipNextHitSounds { get; set; }

    public HealthMode HealthMode
    {
        get
        {
            if (Playfield.Screen.Mods.Any(m => m is HardMod)) return HealthMode.Drain;
            if (Playfield.Screen.Mods.Any(m => m is EasyMod)) return HealthMode.Requirement;

            return HealthMode.Normal;
        }
    }

    public float Health { get; private set; }
    public float HealthDrainRate { get; private set; }

    public List<float> ScrollVelocityMarks { get; } = new();

    public bool IsFinished => FutureHitObjects.Count == 0 && HitObjects.Count == 0;
    public bool AutoPlay => Playfield.Screen.Mods.Any(m => m is AutoPlayMod);

    public bool Break => TimeUntilNextHitObject >= 2000;
    public double TimeUntilNextHitObject => (nextHitObject?.Time ?? double.MaxValue) - clock.CurrentTime;
    public double MaxHitObjectTime => PositionFromTime(clock.CurrentTime) + 2000 * Playfield.Screen.Rate / ScrollSpeed;

    private HitObjectInfo nextHitObject
    {
        get
        {
            if (HitObjects.Count > 0)
                return HitObjects[0].Data;

            return FutureHitObjects.Count > 0 ? FutureHitObjects[0] : null;
        }
    }

    public HitObjectManager(Playfield playfield)
    {
        Playfield = playfield;
        Performance = playfield.Screen.Performance;
        Health = HealthMode == HealthMode.Requirement ? 0 : 100;
    }

    [BackgroundDependencyLoader]
    private void load(FluXisConfig config)
    {
        RelativeSizeAxes = Axes.Both;
        Size = new Vector2(1, 1);

        scrollSpeed = config.GetBindable<float>(FluXisSetting.ScrollSpeed);
        Playfield.Screen.OnSeek += onSeek;
    }

    private void onSeek(double prevTime, double newTime)
    {
        if (!AutoPlay) return;

        newTime = Math.Max(newTime, 0);

        SkipNextHitSounds = true;
        clock.Seek(newTime);

        if (newTime < prevTime)
        {
            var hitObjects = Map.HitObjects.Where(h => h.Time >= newTime && h.Time < prevTime).ToList();

            int count = hitObjects.Sum(h => h.IsLongNote() ? 2 : 1);
            Performance.Combo -= count;
            Performance.Judgements[Judgement.Flawless] -= count;
            Performance.HitStats.RemoveAll(h => h.Time > newTime);

            foreach (var hit in hitObjects)
                FutureHitObjects.Add(hit);

            FutureHitObjects.RemoveAll(h => h.Time <= newTime);
            FutureHitObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
        }
    }

    protected override void Update()
    {
        updateTime();

        while (FutureHitObjects is { Count: > 0 } && PositionFromTime(FutureHitObjects[0].Time) <= MaxHitObjectTime)
        {
            HitObject hitObject = new HitObject(this, FutureHitObjects[0]);
            FutureHitObjects.RemoveAt(0);
            HitObjects.Add(hitObject);
            AddInternal(hitObject);
        }

        if (AutoPlay)
            updateAutoPlay();
        else
            updateInput();

        foreach (var hitObject in HitObjects.Where(h => h.Missed && h.Exists).ToList())
        {
            if (hitObject.Data.IsLongNote())
            {
                if (!hitObject.LongNoteMissed)
                {
                    hitObject.MissLongNote();
                    miss(hitObject);
                }

                if (hitObject.IsOffScreen())
                {
                    hitObject.Kill();
                    HitObjects.Remove(hitObject);
                    RemoveInternal(hitObject, true);
                }
            }
            else
            {
                miss(hitObject);
            }
        }

        foreach (var laneSwitchEvent in Playfield.Screen.MapEvents.LaneSwitchEvents)
        {
            if (laneSwitchEvent.Time <= clock.CurrentTime)
            {
                CurrentKeyCount = laneSwitchEvent.Count;
                CurrentLaneSwitchEvent = laneSwitchEvent;
            }
        }

        if (!Playfield.Screen.IsPaused.Value && !Break && HealthMode == HealthMode.Drain)
        {
            HealthDrainRate = Math.Max(HealthDrainRate, -1f);

            Health -= HealthDrainRate * ((float)Clock.ElapsedFrameTime / 1000f);
            HealthDrainRate += 0.001f * (float)Clock.ElapsedFrameTime;
        }

        Health = Math.Clamp(Health, 0, 100);
        if (Health == 0 && !Dead && HealthMode != HealthMode.Requirement && !Playfield.Screen.Mods.Any(m => m is NoFailMod))
            Playfield.Screen.Die();

        base.Update();
    }

    private void updateTime()
    {
        int curSv = 0;

        while (Map.ScrollVelocities != null && curSv < Map.ScrollVelocities.Count && Map.ScrollVelocities[curSv].Time <= clock.CurrentTime)
            curSv++;

        CurrentTime = PositionFromTime(clock.CurrentTime, curSv);
    }

    private void updateAutoPlay()
    {
        bool[] pressed = new bool[Map.KeyCount];

        List<HitObject> belowTime = HitObjects.Where(h => h.Data.Time <= clock.CurrentTime && h.Exists).ToList();

        foreach (var hitObject in belowTime.Where(h => !h.GotHit).ToList())
        {
            if (!SkipNextHitSounds)
                Playfield.Screen.HitSound.Play();

            hit(hitObject, false);
            pressed[hitObject.Data.Lane - 1] = true;
        }

        foreach (var hitObject in belowTime.Where(h => h.Data.IsLongNote()).ToList())
        {
            hitObject.IsBeingHeld = true;
            pressed[hitObject.Data.Lane - 1] = true;

            if (hitObject.Data.HoldEndTime <= clock.CurrentTime)
                hit(hitObject, true);
        }

        for (var i = 0; i < pressed.Length; i++)
            Playfield.Receptors[i].IsDown = pressed[i];

        SkipNextHitSounds = false;
    }

    private void updateInput()
    {
        if (Dead)
            return;

        GameplayInput input = Playfield.Screen.Input;

        if (input.JustPressed.Contains(true))
        {
            Playfield.Screen.HitSound.Play();

            List<HitObject> hitable = HitObjects.Where(hit => hit.Hitable && input.JustPressed[hit.Data.Lane - 1]).ToList();

            bool[] pressed = new bool[Map.KeyCount];

            if (hitable.Count > 0)
            {
                foreach (var hitObject in hitable)
                {
                    if (!pressed[hitObject.Data.Lane - 1])
                    {
                        hit(hitObject, false);
                        pressed[hitObject.Data.Lane - 1] = true;
                    }
                }
            }
        }

        if (input.Pressed.Contains(true))
        {
            foreach (var hit in HitObjects)
            {
                if (hit.Hitable && hit.GotHit && hit.Data.IsLongNote() && input.Pressed[hit.Data.Lane - 1])
                    hit.IsBeingHeld = true;
            }
        }

        if (input.JustReleased.Contains(true))
        {
            List<HitObject> releaseable = HitObjects.Where(hit => hit.Releasable && input.JustReleased[hit.Data.Lane - 1]).ToList();

            bool[] pressed = new bool[Map.KeyCount];

            if (releaseable.Count > 0)
            {
                foreach (var hitObject in releaseable)
                {
                    if (!pressed[hitObject.Data.Lane - 1])
                    {
                        hit(hitObject, true);
                        pressed[hitObject.Data.Lane - 1] = true;
                    }
                }
            }
        }
    }

    private void hit(HitObject hitObject, bool isHoldEnd)
    {
        double diff = isHoldEnd ? hitObject.Data.HoldEndTime - clock.CurrentTime : hitObject.Data.Time - clock.CurrentTime;
        diff = AutoPlay ? 0 : diff;
        diff /= clock.Rate;
        hitObject.GotHit = true;

        judmentDisplay(hitObject, diff);

        Performance.IncCombo();

        if (!hitObject.Data.IsLongNote() || isHoldEnd)
        {
            hitObject.Kill();
            HitObjects.Remove(hitObject);
            RemoveInternal(hitObject, true);
        }
    }

    private void miss(HitObject hitObject)
    {
        if (Performance.Combo >= 5)
            Playfield.Screen.Combobreak.Play();

        judmentDisplay(hitObject, 0, !AutoPlay);
        Performance.ResetCombo();

        if (hitObject.Data.IsLongNote()) return;

        hitObject.Kill();
        HitObjects.Remove(hitObject);
        RemoveInternal(hitObject, true);
    }

    private void judmentDisplay(HitObject hitObject, double difference, bool missed = false)
    {
        HitWindow hitWindow = missed ? HitWindow.FromKey(Judgement.Miss) : HitWindow.FromTiming((float)Math.Abs(difference));

        if (Dead)
            return;

        Performance.AddHitStat(new HitStat(hitObject.Data.Time, (float)difference, hitWindow.Key));
        Performance.AddJudgement(hitWindow.Key);

        if (HealthMode == HealthMode.Drain)
            HealthDrainRate -= hitWindow.DrainRate;
        else
            Health += hitWindow.Health;

        if (hitWindow.Key == Judgement.Miss && Playfield.Screen.Mods.Any(m => m is FragileMod))
            Playfield.Screen.Die();

        if (hitWindow.Key != Judgement.Flawless && Playfield.Screen.Mods.Any(m => m is FlawlessMod))
            Playfield.Screen.Die();
    }

    public void LoadMap(MapInfo map)
    {
        Map = map;
        CurrentKeyCount = map.InitialKeyCount;
        initScrollVelocityMarks();

        foreach (var hit in map.HitObjects)
        {
            if (Playfield.Screen.Mods.Any(m => m is NoLnMod))
                hit.HoldTime = 0;

            FutureHitObjects.Add(hit);
        }
    }

    private void initScrollVelocityMarks()
    {
        if (Map.ScrollVelocities == null || Map.ScrollVelocities.Count == 0 || Playfield.Screen.Mods.Any(m => m is NoSvMod))
            return;

        ScrollVelocityInfo first = Map.ScrollVelocities[0];

        float time = first.Time;
        ScrollVelocityMarks.Add(time);

        for (var i = 1; i < Map.ScrollVelocities.Count; i++)
        {
            ScrollVelocityInfo prev = Map.ScrollVelocities[i - 1];
            ScrollVelocityInfo current = Map.ScrollVelocities[i];

            time += (int)((current.Time - prev.Time) * prev.Multiplier);
            ScrollVelocityMarks.Add(time);
        }
    }

    public double PositionFromTime(double time, int index = -1)
    {
        if (Map.ScrollVelocities == null || Map.ScrollVelocities.Count == 0 || Playfield.Screen.Mods.Any(m => m is NoSvMod))
            return time;

        if (index == -1)
        {
            for (index = 0; index < Map.ScrollVelocities.Count; index++)
            {
                if (time < Map.ScrollVelocities[index].Time)
                    break;
            }
        }

        if (index == 0)
            return time;

        ScrollVelocityInfo prev = Map.ScrollVelocities[index - 1];

        double position = ScrollVelocityMarks[index - 1];
        position += (time - prev.Time) * prev.Multiplier;
        return position;
    }
}
