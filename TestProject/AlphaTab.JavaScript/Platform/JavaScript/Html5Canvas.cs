///*
// * This file is part of alphaTab.
// * Copyright © 2017, Daniel Kuschny and Contributors, All rights reserved.
// * 
// * This library is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 3.0 of the License, or at your option any later version.
// * 
// * This library is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// * 
// * You should have received a copy of the GNU Lesser General Public
// * License along with this library.
// */
//using System;
//using AlphaTab.Platform.Model;
//using AlphaTab.Rendering;
//using AlphaTab.Rendering.Glyphs;
//using TextAlign = AlphaTab.Platform.Model.TextAlign;

//namespace AlphaTab.Platform.JavaScript
//{
//    /// <summary>
//    /// A canvas implementation for HTML5 canvas
//    /// </summary>
//    public class Html5Canvas : ICanvas
//    {
//        private HTMLCanvasElement _canvas;
//        private CanvasRenderingContext2D _context;
//        private Color _color;
//        private Font _font;
//        private Font _musicFont;

//        public RenderingResources Resources { get; set; }

//        public Html5Canvas()
//        {
//            _color = new Color(0, 0, 0);
//            var fontElement = Document.CreateElement("span");
//            fontElement.ClassList.Add("at");
//            Document.Body.AppendChild(fontElement);
//            var style = Window.GetComputedStyle(fontElement, null);
//            _musicFont = new Font(style.FontFamily, Platform.ParseFloat(style.FontSize));
//        }

//        public virtual object OnPreRender()
//        {
//            // nothing to do
//            return null;
//        }

//        public virtual object OnRenderFinished()
//        {
//            // nothing to do
//            return null;
//        }
//        public void BeginRender(float width, float height)
//        {
//            _canvas = Document.CreateElement<HTMLCanvasElement>("canvas");
//            _canvas.Width = (int) width;
//            _canvas.Height = (int) height;
//            _canvas.Style.Width = width + "px";
//            _canvas.Style.Height = height + "px";
//            _context = (CanvasRenderingContext2D)_canvas.GetContext("2d");
//            _context.TextBaseline = CanvasTypes.CanvasTextBaselineAlign.Top;
//        }

//        public object EndRender()
//        {
//            var result = _canvas;
//            _canvas = null;
//            return result;
//        }

//        public Color Color
//        {
//            get
//            {
//                return _color;
//            }
//            set
//            {
//                if (_color.RGBA == value.RGBA) return;
//                _color = value;
//                _context.StrokeStyle = value.RGBA;
//                _context.FillStyle = value.RGBA;
//            }
//        }

//        public float LineWidth
//        {
//            get
//            {
//                return (float)_context.LineWidth;
//            }
//            set
//            {
//                _context.LineWidth = value;
//            }
//        }

//        public void FillRect(float x, float y, float w, float h)
//        {
//            if (w > 0)
//            {
//                _context.FillRect(((int)x - 0.5).As<int>(), ((int)y - 0.5).As<int>(), w.As<int>(), h.As<int>());
//            }
//        }

//        public void StrokeRect(float x, float y, float w, float h)
//        {
//            _context.StrokeRect((x - 0.5).As<int>(), (y - 0.5).As<int>(), w.As<int>(), h.As<int>());
//        }

//        public void BeginPath()
//        {
//            _context.BeginPath();
//        }

//        public void ClosePath()
//        {
//            _context.ClosePath();
//        }

//        public void MoveTo(float x, float y)
//        {
//            _context.MoveTo(x - 0.5, y - 0.5);
//        }

//        public void LineTo(float x, float y)
//        {
//            _context.LineTo(x - 0.5, y - 0.5);
//        }

//        public void QuadraticCurveTo(float cpx, float cpy, float x, float y)
//        {
//            _context.QuadraticCurveTo(cpx, cpy, x, y);
//        }

//        public void BezierCurveTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y)
//        {
//            _context.BezierCurveTo(cp1x, cp1y, cp2x, cp2y, x, y);
//        }

//        public void FillCircle(float x, float y, float radius)
//        {
//            _context.BeginPath();
//            _context.Arc(x, y, radius, 0, Math.PI * 2, true);
//            Fill();
//        }

//        public void Fill()
//        {
//            _context.Fill();
//        }

//        public void Stroke()
//        {
//            _context.Stroke();
//        }

//        public Font Font
//        {
//            get { return _font; }
//            set
//            {
//                _font = value;
//                _context.Font = value.ToCssString();
//            }
//        }

//        public TextAlign TextAlign
//        {
//            get
//            {
//                switch (_context.TextAlign)
//                {
//                    case CanvasTypes.CanvasTextAlign.Left:
//                        return TextAlign.Left;
//                    case CanvasTypes.CanvasTextAlign.Center:
//                        return TextAlign.Center;
//                    case CanvasTypes.CanvasTextAlign.Right:
//                        return TextAlign.Right;
//                    default:
//                        return TextAlign.Left;
//                }
//            }
//            set
//            {
//                switch (value)
//                {
//                    case TextAlign.Left:
//                        _context.TextAlign = CanvasTypes.CanvasTextAlign.Left;
//                        break;
//                    case TextAlign.Center:
//                        _context.TextAlign = CanvasTypes.CanvasTextAlign.Center;
//                        break;
//                    case TextAlign.Right:
//                        _context.TextAlign = CanvasTypes.CanvasTextAlign.Right;
//                        break;
//                }
//            }
//        }

//        public TextBaseline TextBaseline
//        {
//            get
//            {
//                switch (_context.TextBaseline)
//                {
//                    case CanvasTypes.CanvasTextBaselineAlign.Top:
//                        return TextBaseline.Top;
//                    case CanvasTypes.CanvasTextBaselineAlign.Middle:
//                        return TextBaseline.Middle;
//                    case CanvasTypes.CanvasTextBaselineAlign.Bottom:
//                        return TextBaseline.Bottom;
//                    default:
//                        return TextBaseline.Top;
//                }
//            }
//            set
//            {
//                switch (value)
//                {
//                    case TextBaseline.Top:
//                        _context.TextBaseline = CanvasTypes.CanvasTextBaselineAlign.Top;
//                        break;
//                    case TextBaseline.Middle:
//                        _context.TextBaseline = CanvasTypes.CanvasTextBaselineAlign.Middle;
//                        break;
//                    case TextBaseline.Bottom:
//                        _context.TextBaseline = CanvasTypes.CanvasTextBaselineAlign.Bottom;
//                        break;
//                }
//            }
//        }

//        public void BeginGroup(string identifier)
//        {
//        }

//        public void EndGroup()
//        {
//        }

//        public void FillText(string text, float x, float y)
//        {
//            _context.FillText(text, x.As<int>(), y.As<int>());
//        }

//        public float MeasureText(string text)
//        {
//            return (float)_context.MeasureText(text).Width;
//        }

//        public void FillMusicFontSymbol(float x, float y, float scale, MusicFontSymbol symbol)
//        {
//            if (symbol == MusicFontSymbol.None)
//            {
//                return;
//            }
//            var baseLine = _context.TextBaseline;
//            var font = _context.Font;
//            _context.Font = _musicFont.ToCssString(scale);
//            _context.TextBaseline = CanvasTypes.CanvasTextBaselineAlign.Middle;
//            _context.FillText(Platform.StringFromCharCode((int) symbol), x.As<int>(), y.As<int>());
//            _context.TextBaseline = baseLine;
//            _context.Font = font;
//        }
//    }
//}