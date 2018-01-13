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
using AlphaTab.Model;

namespace AlphaTab.Rendering.Glyphs
{
    public class BeatGlyphBase : GlyphGroup
    {
        public BeatContainerGlyph Container { get; set; }

        public BeatGlyphBase()
            : base(0, 0)
        {
        }

        public override void DoLayout()
        {
            // left to right layout
            var w = 0f;
            if (Glyphs != null)
            {
                for (int i = 0, j = Glyphs.Count; i < j; i++)
                {
                    var g = Glyphs[i];
                    g.X = w;
                    g.Renderer = Renderer;
                    g.DoLayout();
                    w += g.Width;
                }
            }
            Width = w;
        }

        protected void NoteLoop(Action<Note> action)
        {
            for (int i = Container.Beat.Notes.Count - 1; i >= 0; i--)
            {
                action(Container.Beat.Notes[i]);
            }
        }
    }
}
