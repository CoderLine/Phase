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
using AlphaTab.Platform;

namespace AlphaTab.Rendering.Glyphs
{
    public class FadeInGlyph : EffectGlyph
    {
        public FadeInGlyph(float x, float y)
            : base(x, y)
        {
        }

        public override void DoLayout()
        {
            base.DoLayout();
            Height = 17 * Scale;
        }

        public override void Paint(float cx, float cy, ICanvas canvas)
        {
            var size = 6 * Scale;
            var width = Math.Max(Width, 14 * Scale);

            var offset = Height / 2;

            canvas.BeginPath();
            canvas.MoveTo(cx + X, cy + Y + offset);
            canvas.QuadraticCurveTo(cx + X + (width / 2), cy + Y + offset, cx + X + width, cy + Y + offset - size);
            canvas.MoveTo(cx + X, cy + Y + offset);
            canvas.QuadraticCurveTo(cx + X + (width / 2), cy + Y + offset, cx + X + width, cy + Y + offset + size);
            canvas.Stroke();
        }
    }
}
