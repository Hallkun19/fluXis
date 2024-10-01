﻿using System.Collections.Generic;
using System.Linq;
using fluXis.Game.Graphics.Sprites;
using fluXis.Game.Graphics.UserInterface;
using fluXis.Game.Graphics.UserInterface.Color;
using fluXis.Game.Overlay.Mouse;
using fluXis.Shared.Components.Maps;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace fluXis.Game.Map.Drawables.Card;

public partial class MapCard
{
    private partial class TooltipEntry : GridContainer
    {
        public TooltipEntry(APIMap map)
        {
            Size = new Vector2(320, 14);
            ColumnDimensions = new Dimension[]
            {
                new(GridSizeMode.Absolute, 32),
                new(),
                new(GridSizeMode.Absolute, 48)
            };

            Content = new[]
            {
                new Drawable[]
                {
                    new RoundedChip
                    {
                        AutoSizeAxes = Axes.None,
                        Size = new Vector2(32, 14),
                        WebFontSize = 10,
                        BackgroundColour = FluXisColors.GetKeyColor(map.Mode),
                        TextColour = Colour4.Black.Opacity(.75f),
                        Text = $"{map.Mode}K"
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 6 },
                        Child = new TruncatingText
                        {
                            RelativeSizeAxes = Axes.X,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = $"{map.Difficulty}",
                            WebFontSize = 12
                        }
                    },
                    new DifficultyChip
                    {
                        Size = new Vector2(48, 14),
                        WebFontSize = 10,
                        Rating = map.NotesPerSecond
                    }
                }
            };
        }
    }

    private partial class MapCardTooltip : CustomTooltipContainer<APIMapSet>
    {
        private FillFlowContainer flow { get; }
        private List<APIMap> maps;

        public MapCardTooltip()
        {
            Child = flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(12),
                Spacing = new Vector2(8)
            };
        }

        public override void SetContent(APIMapSet content)
        {
            var list = content.Maps;
            list.Sort((a, b) => a.NotesPerSecond.CompareTo(b.NotesPerSecond));

            if (maps is not null && maps.SequenceEqual(list))
                return;

            maps = list;
            flow.ChildrenEnumerable = list.Select(m => new TooltipEntry(m));
        }
    }
}