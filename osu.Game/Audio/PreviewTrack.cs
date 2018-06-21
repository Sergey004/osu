﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Threading;

namespace osu.Game.Audio
{
    public abstract class PreviewTrack : Component
    {
        public event Action Stopped;
        public event Action Started;

        private Track track;
        private bool wasPlaying;

        [BackgroundDependencyLoader]
        private void load()
        {
            track = GetTrack();

            if (track != null)
                track.Looping = false;
        }

        /// <summary>
        /// Length of the track.
        /// </summary>
        public double Length => track?.Length ?? 0;

        /// <summary>
        /// The current track time.
        /// </summary>
        public double CurrentTime => track?.CurrentTime ?? 0;

        /// <summary>
        /// Whether the track is loaded.
        /// </summary>
        public bool TrackLoaded => track?.IsLoaded ?? false;

        protected override void Update()
        {
            base.Update();

            // Todo: Track currently doesn't signal its completion, so we have to handle it manually
            if (track != null && wasPlaying && track.HasCompleted)
                Stop();
        }

        private ScheduledDelegate startDelegate;

        public void Start() => startDelegate = Schedule(() =>
        {
            if (!IsLoaded)
                return;

            if (track == null)
                return;

            if (wasPlaying)
                return;
            wasPlaying = true;

            track.Restart();
            Started?.Invoke();
        });

        public void Stop()
        {
            startDelegate?.Cancel();

            if (!IsLoaded)
                return;

            if (track == null)
                return;

            if (!wasPlaying)
                return;
            wasPlaying = false;

            track.Stop();
            Stopped?.Invoke();
        }

        protected abstract Track GetTrack();
    }
}
