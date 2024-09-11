using System;
using fluXis.Game.Database.Maps;
using fluXis.Game.Input;
using fluXis.Shared.Components.Users;
using fluXis.Shared.Scoring;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Screens;

namespace fluXis.Game.Screens.Result;

public partial class Results : FluXisScreen, IKeyBindingHandler<FluXisGlobalKeybind>
{
    public override float Zoom => 1.2f;
    public override float BackgroundDim => 0.75f;
    public override float BackgroundBlur => 0.2f;

    public ScoreInfo ComparisonScore { get; set; }

    protected ScoreInfo Score { get; }
    protected RealmMap Map { get; }
    protected APIUser Player { get; }

    protected DependencyContainer ExtraDependencies { get; set; }

    protected virtual bool PlayEnterAnimation => false;

    public Action OnRestart;

    private ResultsContent content;
    private ResultsFooter footer;

    public Results(RealmMap map, ScoreInfo score, APIUser player)
    {
        Map = map;
        Score = score;
        Player = player;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        ExtraDependencies.CacheAs(this);
        ExtraDependencies.CacheAs(Score);
        ExtraDependencies.CacheAs(Map);
        ExtraDependencies.CacheAs(Player ?? APIUser.Default);

        InternalChildren = new Drawable[]
        {
            content = new ResultsContent(CreateRightContent()),
            footer = new ResultsFooter
            {
                BackAction = this.Exit,
                RestartAction = OnRestart
            }
        };
    }

    protected virtual Drawable[] CreateRightContent() => Array.Empty<Drawable>();

    protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => ExtraDependencies = new DependencyContainer(base.CreateChildDependencies(parent));

    public override void OnEntering(ScreenTransitionEvent e)
    {
        this.FadeOut();

        using (BeginDelayedSequence(ENTER_DELAY))
        {
            this.FadeInFromZero(FADE_DURATION);
            content.Show(PlayEnterAnimation);
            footer.Show();
        }
    }

    public override bool OnExiting(ScreenExitEvent e)
    {
        this.FadeOut(FADE_DURATION);
        content.Hide();
        footer.Hide();
        return base.OnExiting(e);
    }

    public bool OnPressed(KeyBindingPressEvent<FluXisGlobalKeybind> e)
    {
        switch (e.Action)
        {
            case FluXisGlobalKeybind.Back:
                this.Exit();
                return true;
        }

        return false;
    }

    public void OnReleased(KeyBindingReleaseEvent<FluXisGlobalKeybind> e) { }
}