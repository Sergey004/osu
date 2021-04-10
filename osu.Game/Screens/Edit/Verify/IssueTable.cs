// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets.Edit.Checks.Components;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Verify
{
    public class IssueTable : TableContainer
    {
        private const float horizontal_inset = 20;
        private const float row_height = 25;
        private const int text_size = 14;

        private readonly FillFlowContainer backgroundFlow;

        public IssueTable()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Padding = new MarginPadding { Horizontal = horizontal_inset };
            RowSize = new Dimension(GridSizeMode.Absolute, row_height);

            Masking = true;
            CornerRadius = 6;

            AddInternal(backgroundFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Depth = 1f,
                Padding = new MarginPadding { Horizontal = -horizontal_inset },
                Margin = new MarginPadding { Top = row_height }
            });
        }

        public IEnumerable<Issue> Issues
        {
            set
            {
                Content = null;
                backgroundFlow.Clear();

                if (value?.Any() != true)
                    return;

                foreach (var issue in value)
                {
                    backgroundFlow.Add(new RowBackground(issue));
                }

                Columns = createHeaders();
                Content = value.Select((g, i) => createContent(i, g)).ToArray().ToRectangular();
            }
        }

        private TableColumn[] createHeaders()
        {
            var columns = new List<TableColumn>
            {
                new TableColumn(string.Empty, Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
                new TableColumn("Type", Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
                new TableColumn("Time", Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
                new TableColumn("Message", Anchor.CentreLeft),
                new TableColumn("Category", Anchor.CentreRight, new Dimension(GridSizeMode.AutoSize)),
            };

            return columns.ToArray();
        }

        private Drawable[] createContent(int index, Issue issue) => new Drawable[]
        {
            new OsuSpriteText
            {
                Text = $"#{index + 1}",
                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Medium)
            },
            new OsuSpriteText
            {
                Text = issue.Template.Type.ToString(),
                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Bold),
                Margin = new MarginPadding { Left = 10 },
                Colour = issue.Template.TypeColour()
            },
            new OsuSpriteText
            {
                Text = issue.GetEditorTimestamp(),
                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Bold),
                Margin = new MarginPadding(10)
            },
            new OsuSpriteText
            {
                Text = issue.ToString(),
                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Medium)
            },
            new OsuSpriteText
            {
                Text = issue.Template.Origin.Metadata().Category.ToString(),
                Font = OsuFont.GetFont(size: text_size, weight: FontWeight.Bold),
                Margin = new MarginPadding(10)
            }
        };

        public class RowBackground : OsuClickableContainer
        {
            private const int fade_duration = 100;

            private readonly Box hoveredBackground;

            [Resolved]
            private EditorClock clock { get; set; }

            [Resolved]
            private Editor editor { get; set; }

            [Resolved]
            private EditorBeatmap editorBeatmap { get; set; }

            public RowBackground(Issue issue)
            {
                RelativeSizeAxes = Axes.X;
                Height = row_height;

                AlwaysPresent = true;

                CornerRadius = 3;
                Masking = true;

                Children = new Drawable[]
                {
                    hoveredBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                    },
                };

                Action = () =>
                {
                    // Supposed to work like clicking timestamps outside of the game.
                    // TODO: Is there already defined behaviour for this I may be able to call?

                    if (issue.Time != null)
                    {
                        clock.Seek(issue.Time.Value);
                        editor.OnPressed(GlobalAction.EditorComposeMode);
                    }

                    if (!issue.HitObjects.Any())
                        return;

                    editorBeatmap.SelectedHitObjects.Clear();
                    editorBeatmap.SelectedHitObjects.AddRange(issue.HitObjects);
                };
            }

            private Color4 colourHover;

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                hoveredBackground.Colour = colourHover = colours.BlueDarker;
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateState();
                base.OnHoverLost(e);
            }

            private void updateState()
            {
                hoveredBackground.FadeColour(colourHover, 450, Easing.OutQuint);

                if (IsHovered)
                    hoveredBackground.FadeIn(fade_duration, Easing.OutQuint);
                else
                    hoveredBackground.FadeOut(fade_duration, Easing.OutQuint);
            }
        }
    }
}
