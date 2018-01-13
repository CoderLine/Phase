﻿/*
 * This file is part of alphaTab.
 * Copyright © 2017, Daniel Kuschny and Contributors, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or at your option any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */
using System;
using AlphaTab.Collections;
using AlphaTab.Model;
using AlphaTab.Platform;
using AlphaTab.Platform.Model;
using AlphaTab.Rendering.Utils;

namespace AlphaTab.Rendering.Glyphs
{
    public class ScoreNoteGlyphInfo
    {
        public Glyph Glyph { get; set; }
        public int Line { get; set; }
        public Note Note { get; set; }

        public ScoreNoteGlyphInfo(Glyph glyph, int line, Note note)
        {
            Glyph = glyph;
            Line = line;
        }
    }

    public class ScoreNoteChordGlyph : Glyph
    {
        private readonly FastList<ScoreNoteGlyphInfo> _infos;
        private readonly FastDictionary<int, Glyph> _noteLookup;
        private Glyph _tremoloPicking;
        private float _noteHeadPadding; 

        public ScoreNoteGlyphInfo MinNote { get; set; }
        public ScoreNoteGlyphInfo MaxNote { get; set; }

        public Action SpacingChanged { get; set; }
        public float UpLineX { get; set; }
        public float DownLineX { get; set; }

        public FastDictionary<string, Glyph> BeatEffects { get; set; }

        public Beat Beat { get; set; }
        public BeamingHelper BeamingHelper { get; set; }

        public ScoreNoteChordGlyph()
            : base(0, 0)
        {
            _infos = new FastList<ScoreNoteGlyphInfo>();
            BeatEffects = new FastDictionary<string, Glyph>();
            _noteLookup = new FastDictionary<int, Glyph>();
        }

        public BeamDirection Direction
        {
            get
            {
                return BeamingHelper.Direction;
            }
        }

        public float GetNoteX(Note note, bool onEnd = true)
        {
            if (_noteLookup.ContainsKey(note.String))
            {
                var n = _noteLookup[note.String];
                var pos = X + n.X;
                if (onEnd)
                {
                    pos += n.Width;
                }
                return pos;
            }
            return 0;
        }

        public float GetNoteY(Note note)
        {
            if (_noteLookup.ContainsKey(note.String))
            {
                return Y + _noteLookup[note.String].Y;
            }
            return 0;
        }

        public void AddNoteGlyph(Glyph noteGlyph, Note note, int noteLine)
        {
            var info = new ScoreNoteGlyphInfo(noteGlyph, noteLine, note);
            _infos.Add(info);
            _noteLookup[note.String] = noteGlyph;
            if (MinNote == null || MinNote.Line > info.Line)
            {
                MinNote = info;
            }
            if (MaxNote == null || MaxNote.Line < info.Line)
            {
                MaxNote = info;
            }
        }

        public void UpdateBeamingHelper(float cx)
        {
            if (BeamingHelper != null)
            {
                BeamingHelper.RegisterBeatLineX(ScoreBarRenderer.StaffId, Beat, cx + X + UpLineX, cx + X + DownLineX);
            }
        }

        public bool HasTopOverflow
        {
            get
            {
                return MinNote != null && MinNote.Line < 0;
            }
        }

        public bool HasBottomOverflow
        {
            get
            {
                return MaxNote != null && MaxNote.Line > 8;
            }
        }

        public override void DoLayout()
        {
            _infos.Sort((a, b) => a.Line.CompareTo(b.Line));

            const int padding = 0; // Platform.int(4 * getScale());

            var displacedX = 0f;

            var lastDisplaced = false;
            var lastLine = 0;
            var anyDisplaced = false;
            var direction = Direction;

            var w = 0f;
            for (int i = 0, j = _infos.Count; i < j; i++)
            {
                var g = _infos[i].Glyph;
                g.Renderer = Renderer;
                g.DoLayout();

                var displace = false;
                if (i == 0)
                {
                    displacedX = g.Width + padding;
                }
                else
                {
                    // check if note needs to be repositioned
                    if (Math.Abs(lastLine - _infos[i].Line) <= 1)
                    {
                        // reposition if needed
                        if (!lastDisplaced)
                        {
                            displace = true;
                            g.X = displacedX - (Scale);
                            anyDisplaced = true;
                            lastDisplaced = true; // let next iteration know we are displace now
                        }
                        else
                        {
                            lastDisplaced = false;  // let next iteration know that we weren't displaced now
                        }
                    }
                    else // offset is big enough? no displacing needed
                    {
                        lastDisplaced = false;
                    }
                }

                // for beat direction down we invert the displacement.
                // this means: displaced is on the left side of the stem and not displaced is right
                if (direction == BeamDirection.Down)
                {
                    g.X = displace
                        ? padding
                        : displacedX;
                }
                else
                {
                    g.X = displace
                        ? displacedX
                        : padding;
                }

                lastLine = _infos[i].Line;
                w = Math.Max(w, g.X + g.Width);
            }

            if (anyDisplaced)
            {
                _noteHeadPadding = 0;
                UpLineX = displacedX;
                DownLineX = displacedX;
            }
            else
            {
                _noteHeadPadding = direction == BeamDirection.Down ? -displacedX : 0;
                w += _noteHeadPadding;
                UpLineX = w;
                DownLineX = padding;
            }

            foreach (var effectKey in BeatEffects)
            {
                var effect = BeatEffects[effectKey];
                effect.Renderer = Renderer;
                effect.DoLayout();
            }

            if (Beat.IsTremolo)
            {
                int offset;
                var baseNote = direction == BeamDirection.Up ? MinNote : MaxNote;
                var tremoloX = direction == BeamDirection.Up ? displacedX : 0;
                var speed = Beat.TremoloSpeed.Value;
                switch (speed)
                {
                    case Duration.ThirtySecond:
                        offset = direction == BeamDirection.Up ? -15 : 15;
                        break;
                    case Duration.Sixteenth:
                        offset = direction == BeamDirection.Up ? -12 : 15;
                        break;
                    case Duration.Eighth:
                        offset = direction == BeamDirection.Up ? -10 : 10;
                        break;
                    default:
                        offset = direction == BeamDirection.Up ? -10 : 15;
                        break;
                }

                _tremoloPicking = new TremoloPickingGlyph(tremoloX, baseNote.Glyph.Y + offset * Scale, Beat.TremoloSpeed.Value);
                _tremoloPicking.Renderer = Renderer;
                _tremoloPicking.DoLayout();
            }

            Width = w + padding;
        }

        public override void Paint(float cx, float cy, ICanvas canvas)
        {
            cx += X;
            cy += Y;
            // TODO: this method seems to be quite heavy according to the profiler, why?
            var scoreRenderer = (ScoreBarRenderer)Renderer;

            //
            // Note Effects only painted once
            //
            var effectY = BeamingHelper.Direction == BeamDirection.Up
                            ? scoreRenderer.GetScoreY(MaxNote.Line, 1.5f * NoteHeadGlyph.NoteHeadHeight)
                            : scoreRenderer.GetScoreY(MinNote.Line, -1.0f * NoteHeadGlyph.NoteHeadHeight);
            // TODO: take care of actual glyph height
            var effectSpacing = (BeamingHelper.Direction == BeamDirection.Up)
                            ? 7 * Scale
                            : -7 * Scale;

            foreach (var effectKey in BeatEffects)
            {
                var g = BeatEffects[effectKey];
                g.Y = effectY;
                g.X = Width / 2;
                g.Paint(cx, cy, canvas);
                effectY += effectSpacing;
            }


            // TODO: Take care of beateffects in overflow

            var linePadding = 3 * Scale;
            var lineWidth = Width + linePadding*2;
            if (HasTopOverflow)
            {
                var color = canvas.Color;
                canvas.Color = Renderer.ScoreRenderer.RenderingResources.StaveLineColor;
                var l = -1;
                while (l >= MinNote.Line)
                {
                    // + 1 Because we want to place the line in the center of the note, not at the top
                    var lY = cy + scoreRenderer.GetScoreY(l);
                    canvas.FillRect(cx - linePadding, lY, lineWidth, Scale);
                    l -= 2;
                }
                canvas.Color = color;
            }

            if (HasBottomOverflow)
            {
                var color = canvas.Color;
                canvas.Color = Renderer.ScoreRenderer.RenderingResources.StaveLineColor;
                var l = 12;
                while (l <= MaxNote.Line)
                {
                    var lY = cy + scoreRenderer.GetScoreY(l);
                    canvas.FillRect(cx - linePadding, lY, lineWidth, Scale);
                    l += 2;
                }
                canvas.Color = color;
            }

            if (_tremoloPicking != null)
            {
                _tremoloPicking.Paint(cx, cy, canvas);
            }

            var infos = _infos;
            var x = cx + _noteHeadPadding;
            foreach (var g in infos)
            {
                g.Glyph.Renderer = Renderer;
                g.Glyph.Paint(x, cy, canvas);
            }
        }
    }
}
