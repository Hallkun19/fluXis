using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace fluXis.Game.Screens.Multiplayer;

public partial class MultiplayerMenuMusic : Container
{
    private MultiplayerTrack baseTrack;

    private MultiplayerTrack rankedMain;
    private MultiplayerTrack rankedPrepare;
    private MultiplayerTrack rankedWin;
    private MultiplayerTrack rankedLoose;

    private MultiplayerTrack lobbyList;
    private MultiplayerTrack lobbyPrepare;
    private MultiplayerTrack lobbyWin;
    private MultiplayerTrack lobbyLoose;

    [BackgroundDependencyLoader]
    private void load(ITrackStore trackStore)
    {
        InternalChildren = new[]
        {
            baseTrack = trackStore.Get("Menu/Multiplayer/base.wav"),
            rankedMain = trackStore.Get("Menu/Multiplayer/ranked-main.wav"),
            rankedPrepare = trackStore.Get("Menu/Multiplayer/ranked-prepare.wav"),
            rankedWin = trackStore.Get("Menu/Multiplayer/ranked-win.wav"),
            rankedLoose = trackStore.Get("Menu/Multiplayer/ranked-loose.wav"),
            lobbyList = trackStore.Get("Menu/Multiplayer/lobby-list.wav"),
            lobbyPrepare = trackStore.Get("Menu/Multiplayer/lobby-prepare.wav"),
            lobbyWin = trackStore.Get("Menu/Multiplayer/lobby-win.wav"),
            lobbyLoose = trackStore.Get("Menu/Multiplayer/lobby-loose.wav")
        };
    }

    protected override void LoadComplete()
    {
        baseTrack.Start();

        // Ranked
        rankedMain.Start();
        rankedPrepare.Start();
        rankedWin.Start();
        rankedLoose.Start();

        // OpenLobby
        lobbyList.Start();
        lobbyPrepare.Start();
        lobbyWin.Start();
        lobbyLoose.Start();
    }

    public void GoToLayer(int layer, int mode, int alt = 0)
    {
        baseTrack.VolumeTo(1);

        switch (mode)
        {
            case -1:
                rankedMain.VolumeTo(0);
                rankedPrepare.VolumeTo(0);
                rankedWin.VolumeTo(0);
                rankedLoose.VolumeTo(0);

                lobbyList.VolumeTo(0);
                lobbyPrepare.VolumeTo(0);
                break;

            case 0: // Ranked
                rankedMain.VolumeTo(layer >= 0 ? 1 : 0);
                rankedPrepare.VolumeTo(layer >= 1 ? 1 : 0);

                if (alt == 0)
                    rankedWin.VolumeTo(layer >= 2 ? 1 : 0);
                else
                    rankedLoose.VolumeTo(layer >= 2 ? 1 : 0);
                break;

            case 1: // OpenLobby
                lobbyList.VolumeTo(layer >= 0 ? 1 : 0);
                lobbyPrepare.VolumeTo(layer >= 1 ? 1 : 0);

                if (alt == 0)
                    lobbyWin.VolumeTo(layer >= 2 ? 1 : 0);
                else
                    lobbyLoose.VolumeTo(layer >= 2 ? 1 : 0);
                break;
        }
    }

    public void StopAll()
    {
        baseTrack.VolumeTo(0);

        rankedMain.VolumeTo(0);
        rankedPrepare.VolumeTo(0);
        rankedWin.VolumeTo(0);
        rankedLoose.VolumeTo(0);

        lobbyList.VolumeTo(0);
        lobbyPrepare.VolumeTo(0);
    }

    private partial class MultiplayerTrack : Component
    {
        private Track track { get; init; }

        private double volume
        {
            get => track?.Volume.Value ?? 0;
            set
            {
                if (track == null) return;

                track.Volume.Value = value;
            }
        }

        public void Start()
        {
            if (track == null) return;

            volume = 0;
            track.Looping = true;
            track.Start();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            track?.Dispose();
        }

        public void VolumeTo(double volume, int duration = 400) => this.TransformTo(nameof(volume), volume, duration);

        public static implicit operator MultiplayerTrack(Track track) => new() { track = track };
    }
}