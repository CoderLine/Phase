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

using AlphaTab.Model;

namespace AlphaTab.Rendering.Glyphs
{
    /// <summary>
    /// Effect-Glyphs implementing this public interface get notified
    /// as they are expanded over multiple beats.
    /// </summary>
    public class EffectGlyph : Glyph
    {
        /// <summary>
        /// Gets or sets the beat where the glyph belongs to.
        /// </summary>
        public Beat Beat { get; set; }

        /// <summary>
        /// Gets or sets the next glyph of the same type in case 
        /// the effect glyph is expanded when using <see cref="EffectBarGlyphSizing.GroupedOnBeat"/>.
        /// </summary>
        public EffectGlyph NextGlyph { get; set; }

        /// <summary>
        /// Gets or sets the previous glyph of the same type in case 
        /// the effect glyph is expanded when using <see cref="EffectBarGlyphSizing.GroupedOnBeat"/>.
        /// </summary>
        public EffectGlyph PreviousGlyph { get; set; }

        public float Height { get; set; }

        protected EffectGlyph(float x, float y)
            : base(x, y)
        {
        }
    }
}
