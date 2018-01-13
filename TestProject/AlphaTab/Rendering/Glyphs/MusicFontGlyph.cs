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
using AlphaTab.Platform;

namespace AlphaTab.Rendering.Glyphs
{
    public class MusicFontGlyph : EffectGlyph
    {
        protected readonly float GlyphScale;
        private readonly MusicFontSymbol _symbol;

        public MusicFontGlyph(float x, float y, float glyphScale, MusicFontSymbol symbol)
            : base(x, y)
        {
            GlyphScale = glyphScale;
            _symbol = symbol;
        }

        public override void Paint(float cx, float cy, ICanvas canvas)
        {
            canvas.FillMusicFontSymbol(cx + X, cy + Y, GlyphScale*Scale, _symbol);
        }
    }
}
