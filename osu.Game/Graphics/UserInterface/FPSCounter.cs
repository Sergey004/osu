// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Graphics.UserInterface
{
    public class FPSCounter : VisibilityContainer, IHasCustomTooltip
    {
        private RollingCounter<double> msCounter = null!;
        private RollingCounter<double> fpsCounter = null!;

        private Container mainContent = null!;

        private Container background = null!;

        private const float idle_background_alpha = 0.4f;

        private readonly BindableBool showFpsDisplay = new BindableBool(true);

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public FPSCounter()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            InternalChildren = new Drawable[]
            {
                mainContent = new Container
                {
                    Alpha = 0,
                    Size = new Vector2(42, 26),
                    Children = new Drawable[]
                    {
                        background = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = 5,
                            CornerExponent = 5f,
                            Masking = true,
                            Alpha = idle_background_alpha,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = colours.Gray0,
                                    RelativeSizeAxes = Axes.Both,
                                },
                            }
                        },
                        msCounter = new FrameTimeCounter
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Margin = new MarginPadding(1),
                            Y = -2,
                        },
                        fpsCounter = new FramesPerSecondCounter
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Margin = new MarginPadding(2),
                            Y = 10,
                            Scale = new Vector2(0.8f),
                        }
                    }
                },
            };

            config.BindWith(OsuSetting.ShowFpsDisplay, showFpsDisplay);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            displayTemporarily();

            showFpsDisplay.BindValueChanged(showFps =>
            {
                State.Value = showFps.NewValue ? Visibility.Visible : Visibility.Hidden;
                if (showFps.NewValue)
                    displayTemporarily();
            }, true);

            State.BindValueChanged(state => showFpsDisplay.Value = state.NewValue == Visibility.Visible);
        }

        protected override void PopIn() => this.FadeIn(100);

        protected override void PopOut() => this.FadeOut(100);

        protected override bool OnHover(HoverEvent e)
        {
            background.FadeTo(1, 200);
            displayTemporarily();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeTo(idle_background_alpha, 200);
            displayTemporarily();
            base.OnHoverLost(e);
        }

        private bool isDisplayed;

        private ScheduledDelegate? fadeOutDelegate;

        private void displayTemporarily()
        {
            if (!isDisplayed)
            {
                mainContent.FadeTo(1, 300, Easing.OutQuint);
                isDisplayed = true;
            }

            fadeOutDelegate?.Cancel();
            fadeOutDelegate = null;

            if (!IsHovered)
            {
                fadeOutDelegate = Scheduler.AddDelayed(() =>
                {
                    mainContent.FadeTo(0, 300, Easing.OutQuint);
                    isDisplayed = false;
                }, 2000);
            }
        }

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        protected override void Update()
        {
            base.Update();

            // TODO: this is wrong (elapsed clock time, not actual run time).
            double newFrameTime = gameHost.UpdateThread.Clock.ElapsedFrameTime;
            double newFps = gameHost.DrawThread.Clock.FramesPerSecond;

            bool hasSignificantChanges =
                Math.Abs(msCounter.Current.Value - newFrameTime) > 5 ||
                Math.Abs(fpsCounter.Current.Value - newFps) > 10;

            if (hasSignificantChanges)
                displayTemporarily();

            // If the frame time spikes up, make sure it shows immediately on the counter.
            if (msCounter.Current.Value < 20 && newFrameTime > 20)
                msCounter.SetCountWithoutRolling(newFrameTime);
            else
                msCounter.Current.Value = newFrameTime;

            fpsCounter.Current.Value = newFps;

            fpsCounter.Colour = getColour(fpsCounter.DisplayedCount / gameHost.DrawThread.Clock.MaximumUpdateHz);

            double equivalentHz = 1000 / msCounter.DisplayedCount;

            msCounter.Colour = getColour(equivalentHz / gameHost.UpdateThread.Clock.MaximumUpdateHz);
        }

        private ColourInfo getColour(double performanceRatio)
        {
            if (performanceRatio < 0.5f)
                return Interpolation.ValueAt(performanceRatio, colours.Red, colours.Orange2, 0, 0.5);

            return Interpolation.ValueAt(performanceRatio, colours.Orange2, colours.Lime0, 0.5, 0.9);
        }

        public ITooltip GetCustomTooltip() => new FPSCounterTooltip();

        public object TooltipContent => this;

        public class FramesPerSecondCounter : RollingCounter<double>
        {
            protected override double RollingDuration => 400;

            protected override OsuSpriteText CreateSpriteText()
            {
                return new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Font = OsuFont.Default.With(fixedWidth: true, size: 16, weight: FontWeight.SemiBold),
                    Spacing = new Vector2(-2),
                };
            }

            protected override LocalisableString FormatCount(double count)
            {
                return $"{count:#,0}fps";
            }
        }

        public class FrameTimeCounter : RollingCounter<double>
        {
            protected override double RollingDuration => 1000;

            protected override OsuSpriteText CreateSpriteText()
            {
                return new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Font = OsuFont.Default.With(fixedWidth: true, size: 16, weight: FontWeight.SemiBold),
                    Spacing = new Vector2(-1),
                };
            }

            protected override LocalisableString FormatCount(double count)
            {
                if (count < 1)
                    return $"{count:N1}ms";

                return $"{count:N0}ms";
            }
        }
    }
}
