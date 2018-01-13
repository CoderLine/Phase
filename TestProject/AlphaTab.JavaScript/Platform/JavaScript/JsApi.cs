﻿///*
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
//using System.Collections.Generic;
//using AlphaTab.Collections;
//using AlphaTab.Importer;
//using AlphaTab.IO;
//using AlphaTab.Model;
//using AlphaTab.Rendering;
//using AlphaTab.Util;
//using Bridge;
//using Bridge.Html5;

//namespace AlphaTab.Platform.JavaScript
//{
//    public class ResizeEventArgs
//    {
//        public int OldWidth { get; set; }
//        public int NewWidth { get; set; }
//        public Settings Settings { get; set; }
//    }

//    public class JsApi
//    {
//        private readonly HTMLElement _element;
//        private readonly HTMLElement _canvasElement;
//        private readonly Settings _settings;
//        private int _visibilityCheckerInterval;
//        private int _visibilityCheckerIntervalId;

//        private FastList<RenderFinishedEventArgs> _renderResults;
//        private int _totalResultCount;

//        protected bool IsElementVisible
//        {
//            get { return !!(_element.OffsetWidth.As<bool>() || _element.OffsetHeight.As<bool>() || _element.GetClientRects().Length.As<bool>()); }
//        }

//        public IScoreRenderer Renderer { get; private set; }
//        public Score Score { get; set; }
//        public int[] TrackIndexes { get; set; }
//        public Track[] Tracks
//        {
//            get
//            {
//                var tracks = TrackIndexesToTracks(TrackIndexes);

//                if (tracks.Count == 0 && Score.Tracks.Count > 0)
//                {
//                    tracks.Add(Score.Tracks[0]);
//                }

//                return tracks.ToArray();
//            }
//        }

//        protected JsApi(HTMLElement element, dynamic options)
//        {
//            _element = element;

//            _element.ClassList.Add("alphaTab");

//            // load settings
//            var dataAttributes = GetDataAttributes();
//            var settings = _settings = Settings.FromJson(options, dataAttributes);
//            var autoSize = settings.Width < 0;

//            #region build tracks array

//            // get track data to parse
//            dynamic tracksData;
//            if (options != null && options.tracks)
//            {
//                tracksData = options.tracks;
//            }
//            else
//            {
//                if (dataAttributes.ContainsKey("tracks"))
//                {
//                    tracksData = dataAttributes["tracks"];
//                }
//                else
//                {
//                    tracksData = 0;
//                }
//            }

//            SetTracks(tracksData, false);

//            #endregion

//            string contents = "";
//            if (element != null)
//            {
//                // get load contents

//                if (dataAttributes.ContainsKey("tex") && element["innerText"].As<bool>())
//                {
//                    contents = (element.InnerHTML.As<string>()).Trim();
//                    element.InnerHTML = "";
//                }

//                #region Create context elements (wrapper, canvas etc)

//                _canvasElement = Document.CreateElement("div");

//                _canvasElement.ClassName = "alphaTabSurface";
//                _canvasElement.Style.FontSize = "0";
//                _canvasElement.Style.Overflow = "hidden";
//                _canvasElement.Style.LineHeight = "0";
//                element.AppendChild(_canvasElement);

//                #endregion

//                #region Setup scroll and resize handlers for lazy-loading

//                if (settings.Engine == "default" || settings.Engine == "svg")
//                {
//                    Window.AddEventListener("scroll", () =>
//                    {
//                        ShowSvgsInViewPort();
//                    }, true);
//                    Window.AddEventListener("resize", () =>
//                    {
//                        ShowSvgsInViewPort();
//                    }, true);
//                }

//                #endregion

//                #region Auto Sizing

//                if (autoSize)
//                {
//                    settings.Width = element.OffsetWidth;
//                    int timeoutId = 0;
//                    Window.AddEventListener("resize", () =>
//                    {
//                        Window.ClearTimeout(timeoutId);
//                        timeoutId = Window.SetTimeout(() =>
//                        {
//                            if (element.OffsetWidth != settings.Width)
//                            {
//                                TriggerResize();
//                            }
//                        }, 1);
//                    });
//                }

//                #endregion
//            }

//            #region Renderer Setup

//            CreateStyleElement(settings);
            
//            if (element != null && autoSize)
//            {
//                var initialResizeEventInfo = new ResizeEventArgs();
//                initialResizeEventInfo.OldWidth = 0;
//                initialResizeEventInfo.NewWidth = element.OffsetWidth;
//                initialResizeEventInfo.Settings = settings;
//                TriggerEvent("resize", initialResizeEventInfo);
//                settings.Width = initialResizeEventInfo.NewWidth;
//            }

//            var workersUnsupported = !Window.Instance["Worker"].As<bool>();
//            if (settings.UseWebWorker && !workersUnsupported && settings.Engine != "html5")
//            {
//                Renderer = new WorkerScoreRenderer(this, settings);
//            }
//            else
//            {
//                Renderer = new ScoreRenderer(settings);
//            }
//            Renderer.RenderFinished += o => TriggerEvent("rendered");
//            Renderer.PostRenderFinished += () =>
//            {
//                _element.ClassList.Remove("loading");
//                _element.ClassList.Remove("rendering");
//                TriggerEvent("postRendered");
//            };
//            Renderer.PreRender += result =>
//            {
//                _renderResults = new FastList<RenderFinishedEventArgs>();
//                _totalResultCount = 0;
//                AppendRenderResult(result);
//            };
//            Renderer.PartialRenderFinished += AppendRenderResult;
//            Renderer.RenderFinished += r =>
//            {
//                AppendRenderResult(r);
//                AppendRenderResult(null); // marks last element
//            };
//            Renderer.Error += Error;

//            #endregion

//            #region Load Default Data

//            Action initialRender = () =>
//            {
//                // rendering was possibly delayed due to invisible element
//                // in this case we need the correct width for autosize
//                if (autoSize)
//                {
//                    _settings.Width = _element.OffsetWidth;
//                    Renderer.UpdateSettings(settings);
//                }

//                if (!string.IsNullOrEmpty(contents))
//                {
//                    Tex(contents);
//                }
//                else if (options && options.file)
//                {
//                    Load(options.file);
//                }
//                else if (dataAttributes.ContainsKey("file"))
//                {
//                    Load(dataAttributes["file"]);
//                }
//            };

//            _visibilityCheckerInterval = options && options.visibilityCheckInterval || 500;
//            if (IsElementVisible)
//            {
//                // element is visible, so we start rendering
//                initialRender();
//            }
//            else
//            {
//                // if the alphaTab element is not visible, we postpone the rendering
//                // we check in a regular interval whether it became available. 
//                Logger.Warning("Rendering", "AlphaTab container is invisible, checking for element visibility in " + _visibilityCheckerInterval + "ms intervals");
//                _visibilityCheckerIntervalId = Window.SetInterval(() =>
//                {
//                    if (IsElementVisible)
//                    {
//                        Logger.Info("Rendering", "AlphaTab container became visible, triggering initial rendering");
//                        initialRender();
//                        Window.ClearInterval(_visibilityCheckerIntervalId);
//                        _visibilityCheckerIntervalId = 0;
//                    }
//                }, _visibilityCheckerInterval);
//            }

//            #endregion
//        }

//        private FastDictionary<string, object> GetDataAttributes()
//        {
//            var dataAttributes = new FastDictionary<string, object>();

//            if (_element.Dataset.As<bool>())
//            {
//                foreach (var key in Platform.JsonKeys(_element.Dataset))
//                {
//                    object value = _element.Dataset[key];
//                    try
//                    {
//                        value = JSON.Parse(value.As<string>());
//                    }
//                    catch
//                    {
//                        if (value == "")
//                        {
//                            value = null;
//                        }
//                    }
//                    dataAttributes[key] = value;
//                }
//            }
//            else
//            {
//                for (var i = 0; i < _element.Attributes.Length; i++)
//                {
//                    var attr = _element.Attributes[i];
//                    if (attr.NodeName.As<string>().StartsWith("data-"))
//                    {
//                        var keyParts = attr.NodeName.Substring(5).Split("-");
//                        var key = keyParts[0];
//                        for (int j = 1; j < keyParts.Length; j++)
//                        {
//                            key += keyParts[j].Substring(0, 1).ToUpperCase() + keyParts[j].Substr(1);
//                        }

//                        object value = attr.NodeValue;
//                        try
//                        {
//                            value = JSON.Parse(value.As<string>());
//                        }
//                        catch
//                        {
//                            if (value == "")
//                            {
//                                value = null;
//                            }
//                        }
//                        dataAttributes[key] = value;
//                    }
//                }
//            }

//            return dataAttributes;
//        }


//        private void TriggerResize()
//        {
//            // if the element is visible, perfect, we do the update
//            if (IsElementVisible)
//            {
//                if (_visibilityCheckerIntervalId != 0)
//                {
//                    Logger.Info("Rendering", "AlphaTab container became visible again, doing autosizing");
//                    Window.ClearInterval(_visibilityCheckerIntervalId);
//                    _visibilityCheckerIntervalId = 0;
//                }

//                var resizeEventInfo = new ResizeEventArgs();
//                resizeEventInfo.OldWidth = _settings.Width;
//                resizeEventInfo.NewWidth = _element.OffsetWidth;
//                resizeEventInfo.Settings = _settings;
//                TriggerEvent("resize", resizeEventInfo);
//                _settings.Width = resizeEventInfo.NewWidth;
//                Renderer.UpdateSettings(_settings);
//                Renderer.Resize(_element.OffsetWidth);
//            }
//            // if there is no "invisibility timer" we set up one, if there is already a timer scheduled, it will trigger the proper rendering. 
//            else if (_visibilityCheckerIntervalId == 0)
//            {
//                Logger.Warning("Rendering", "AlphaTab container was invisible while autosizing, checking for element visibility in " + _visibilityCheckerInterval + "ms intervals");
//                _visibilityCheckerIntervalId = Window.SetInterval(TriggerResize, _visibilityCheckerInterval);
//            }
//        }

//        private void ShowSvgsInViewPort()
//        {
//            var placeholders = _canvasElement.QuerySelectorAll("[data-lazy=true]");
//            foreach (var x in placeholders)
//            {
//                var placeholder = x.As<HTMLElement>();
//                if (IsElementInViewPort(placeholder))
//                {
//                    placeholder.OuterHTML = placeholder.As<dynamic>().svg;
//                }
//            }
//        }

//        private static bool IsElementInViewPort(Element el)
//        {
//            var rect = el.GetBoundingClientRect();
//            return
//                (
//                    rect.Top + rect.Height >= 0 && rect.Top <= Window.InnerHeight &&
//                    rect.Left + rect.Width >= 0 && rect.Left <= Window.InnerWidth
//                );
//        }

//        public void Print(string width)
//        {
//            // prepare a popup window for printing (a4 width, window height, centered)

//            var preview = Window.Open("", "", "width=0,height=0");
//            var a4 = preview.Document.CreateElement<HTMLDivElement>("div");
//            if (!string.IsNullOrEmpty(width))
//            {
//                a4.Style.Width = width;
//            }
//            else
//            {
//                a4.Style.Width = "210mm";
//            }
//            preview.Document.Write("<!DOCTYPE html><html></head><body></body></html>");
//            preview.Document.Body.AppendChild(a4);
            
//            var dualScreenLeft = Script.TypeOf(Window.Instance["ScreenLeft"]) != "undefined"
//                ? Window.Instance["ScreenLeft"].As<int>()
//                : Window.Screen["left"].As<int>();
//            var dualScreenTop = Script.TypeOf(Window.Instance["ScreeTop"]) != "undefined"
//                ? Window.Instance["ScreeTop"].As<int>()
//                : Window.Screen["top"].As<int>();
//            var screenWidth = Script.TypeOf(Window.InnerWidth) != "undefined"
//                ? Window.InnerWidth
//                : Script.TypeOf(Document.DocumentElement.ClientWidth) != "undefined"
//                    ? Document.DocumentElement.ClientWidth
//                    : Window.Screen["width"].As<int>();
//            var screenHeight = Script.TypeOf(Window.InnerHeight) != "undefined"
//                ? Window.InnerHeight
//                : Script.TypeOf(Document.DocumentElement.ClientHeight) != "undefined"
//                    ? Document.DocumentElement.ClientHeight
//                    : Window.Screen["height"].As<int>();

//            var w = a4.OffsetWidth + 50;
//            var h = Window.InnerHeight;
//            var left = ((screenWidth / 2) - (w / 2)) + dualScreenLeft;
//            var top = ((screenHeight / 2) - (h / 2)) + dualScreenTop;
//            preview.ResizeTo(w, h);
//            preview.MoveTo(left, top);

//            preview.Focus();

//            // render alphaTab
//            var settings = Settings.Defaults;
//            settings.ScriptFile = _settings.ScriptFile;
//            settings.FontDirectory = _settings.FontDirectory;
//            settings.Scale = 0.8f;
//            settings.StretchForce = 0.8f;
//            settings.DisableLazyLoading = true;
//            settings.UseWebWorker = false;

//            var alphaTab = new JsApi(a4, settings);
//            alphaTab.Renderer.PostRenderFinished += () =>
//            {
//                alphaTab._canvasElement.Style.Height = "100%";
//                preview.Print();
//            };
//            alphaTab.SetTracks(Tracks);
//        }

//        private void AppendRenderResult(RenderFinishedEventArgs result)
//        {
//            if (result != null)
//            {
//                _canvasElement.Style.Width = result.TotalWidth + "px";
//                _canvasElement.Style.Height = result.TotalHeight + "px";
//            }


//            if (result == null || result.RenderResult != null)
//            {
//                // the queue/dequeue like mechanism used here is to maintain the order within the setTimeout. 
//                // setTimeout allows to decouple the rendering from the JS processing a bit which makes the overall display faster. 
//                _renderResults.Add(result);

//                Window.SetTimeout(() =>
//                {
//                    while (_renderResults.Count > 0)
//                    {
//                        var renderResult = _renderResults[0];
//                        _renderResults.RemoveAt(0);

//                        // null result indicates that the rendering finished
//                        if (renderResult == null)
//                        {
//                            // so we remove elements that might be from a previous render session
//                            while (_canvasElement.ChildElementCount > _totalResultCount)
//                            {
//                                _canvasElement.RemoveChild(_canvasElement.LastChild);
//                            }
//                        }
//                        // NOTE: here we try to replace existing children 
//                        else
//                        {
//                            var body = renderResult.RenderResult;
//                            if (Script.TypeOf(body) == "string")
//                            {
//                                HTMLElement placeholder;
//                                if (_totalResultCount < _canvasElement.ChildElementCount)
//                                {
//                                    placeholder = _canvasElement.Children[_totalResultCount].As<HTMLElement>();
//                                }
//                                else
//                                {
//                                    placeholder = Document.CreateElement("div");
//                                    _canvasElement.AppendChild(placeholder);
//                                }

//                                placeholder.Style.Width = renderResult.Width + "px";
//                                placeholder.Style.Height = renderResult.Height + "px";
//                                placeholder.Style.Display = "inline-block";

//                                if (IsElementInViewPort(placeholder) || _settings.DisableLazyLoading)
//                                {
//                                    placeholder.OuterHTML = body.As<string>();
//                                }
//                                else
//                                {
//                                    placeholder.As<dynamic>().svg = body;
//                                    placeholder.SetAttribute("data-lazy", "true");
//                                }
//                            }
//                            else
//                            {
//                                if (_totalResultCount < _canvasElement.ChildElementCount)
//                                {
//                                    _canvasElement.ReplaceChild(renderResult.RenderResult.As<Node>(), _canvasElement.Children[_totalResultCount]);
//                                }
//                                else
//                                {
//                                    _canvasElement.AppendChild(renderResult.RenderResult.As<Node>());
//                                }
//                            }
//                            _totalResultCount++;
//                        }
//                    }
//                }, 1);

//            }
//        }

//        private void CreateStyleElement(Settings settings)
//        {
//            var elementDocument = _element.OwnerDocument;
//            var styleElement = elementDocument.GetElementById<HTMLStyleElement>("alphaTabStyle");
//            if (styleElement == null)
//            {
//                string fontDirectory = settings.FontDirectory;
//                styleElement = elementDocument.CreateElement<HTMLStyleElement>("style");
//                styleElement.Id = "alphaTabStyle";
//                styleElement.Type = "text/css";
//                var css = new StringBuilder();
//                css.AppendLine("@font-face {");
//                css.AppendLine("    font-family: 'alphaTab';");
//                css.AppendLine("     src: url('" + fontDirectory + "Bravura.eot');");
//                css.AppendLine("     src: url('" + fontDirectory + "Bravura.eot?#iefix') format('embedded-opentype')");
//                css.AppendLine("          , url('" + fontDirectory + "Bravura.woff') format('woff')");
//                css.AppendLine("          , url('" + fontDirectory + "Bravura.otf') format('opentype')");
//                css.AppendLine("          , url('" + fontDirectory + "Bravura.svg#Bravura') format('svg');");
//                css.AppendLine("     font-weight: normal;");
//                css.AppendLine("     font-style: normal;");
//                css.AppendLine("}");
//                css.AppendLine(".alphaTabSurface * {");
//                css.AppendLine("    cursor: default;");
//                css.AppendLine("}");
//                css.AppendLine(".at {");
//                css.AppendLine("     font-family: 'alphaTab';");
//                css.AppendLine("     speak: none;");
//                css.AppendLine("     font-style: normal;");
//                css.AppendLine("     font-weight: normal;");
//                css.AppendLine("     font-variant: normal;");
//                css.AppendLine("     text-transform: none;");
//                css.AppendLine("     line-height: 1;");
//                css.AppendLine("     line-height: 1;");
//                css.AppendLine("     -webkit-font-smoothing: antialiased;");
//                css.AppendLine("     -moz-osx-font-smoothing: grayscale;");
//                css.AppendLine("     font-size: 34px;");
//                css.AppendLine("     overflow: visible !important;");
//                css.AppendLine("}");
//                styleElement.InnerHTML = css.ToString();
//                elementDocument.GetElementsByTagName("head")[0].AppendChild(styleElement);
//            }
//        }


//        public virtual void Destroy()
//        {
//            _element.InnerHTML = "";
//            Renderer.Destroy();
//        }

//        public void Load(object data)
//        {
//            _element.ClassList.Add("loading");
//            try
//            {
//                if (Script.InstanceOf(data, typeof(ArrayBuffer)))
//                {
//                    ScoreLoaded(ScoreLoader.LoadScoreFromBytes(Platform.ArrayBufferToByteArray((ArrayBuffer)data)));
//                }
//                else if (Script.InstanceOf(data, typeof(Uint8Array)))
//                {
//                    ScoreLoaded(ScoreLoader.LoadScoreFromBytes((byte[])data));
//                }
//                else if (Script.TypeOf(data) == "string")
//                {
//                    ScoreLoader.LoadScoreAsync((string)data, s => ScoreLoaded(s), e =>
//                    {
//                        Error("import", e);
//                    });
//                }
//            }
//            catch (Exception e)
//            {
//                Error("import", e);
//            }
//        }


//        public void Tex(string contents)
//        {
//            _element.ClassList.Add("loading");
//            try
//            {
//                var parser = new AlphaTexImporter();
//                var data = ByteBuffer.FromBuffer(Platform.StringToByteArray(contents));
//                parser.Init(data);
//                ScoreLoaded(parser.ReadScore());
//            }
//            catch (Exception e)
//            {
//                Error("import", e);
//            }
//        }

//        public void SetTracks(dynamic tracksData, bool render = true)
//        {
//            if (tracksData.length && Script.TypeOf(tracksData[0].Index) == "number")
//            {
//                Score = tracksData[0].Score;
//            }
//            else if (Script.TypeOf(tracksData.Index) == "number")
//            {
//                Score = tracksData.Score;
//            }

//            TrackIndexes = ParseTracks(tracksData);

//            if (render)
//            {
//                Render();
//            }
//        }

//        private FastList<Track> TrackIndexesToTracks(int[] trackIndexes)
//        {
//            var tracks = new FastList<Track>();

//            foreach (var track in trackIndexes)
//            {
//                if (track >= 0 && track < Score.Tracks.Count)
//                {
//                    tracks.Add(Score.Tracks[track]);
//                }
//            }

//            return tracks;
//        }

//        public int[] ParseTracks(dynamic tracksData)
//        {
//            FastList<int> tracks = new FastList<int>();

//            // decode string
//            if (Script.TypeOf(tracksData) == "string")
//            {
//                try
//                {
//                    tracksData = JSON.Parse(tracksData.As<string>());
//                }
//                catch
//                {
//                    tracksData = new[] { 0 };
//                }
//            }

//            // decode array
//            if (Script.TypeOf(tracksData) == "number")
//            {
//                tracks.Add((int)tracksData);
//            }
//            else if (tracksData.length)
//            {
//                for (var i = 0; i < tracksData.length; i++)
//                {
//                    int value;
//                    if (Script.TypeOf(tracksData[i]) == "number")
//                    {
//                        value = (int)tracksData[i];
//                    }
//                    else if (Script.TypeOf(tracksData[i].Index) == "number")
//                    {
//                        Track track = tracksData[i];
//                        value = track.Index;
//                    }
//                    else
//                    {
//                        value = Platform.ParseInt(tracksData[i].ToString());
//                    }

//                    if (value >= 0)
//                    {
//                        tracks.Add(value);
//                    }
//                }
//            }
//            else if (Script.TypeOf(tracksData.Index) == "number")
//            {
//                tracks.Add(tracksData.Index.As<int>());
//            }

//            return tracks.ToArray();
//        }

//        protected void ScoreLoaded(Score score, bool render = true)
//        {
//            ModelUtils.ApplyPitchOffsets(_settings, score);

//            Score = score;

//            TriggerEvent("loaded", score);
//            if (render)
//            {
//                Render();
//            }
//        }


//        public void Error(string type, object details)
//        {
//            Logger.Error(type, "An unexpected error occurred", details);
//            TriggerEvent("error", new
//            {
//                type = type,
//                details = details
//            });
//        }

//        public void TriggerEvent(string name, object details = null)
//        {
//            if (_element != null)
//            {
//                name = "alphaTab." + name;
//                dynamic e = Document.CreateEvent("CustomEvent");
//                e.initCustomEvent(name, false, false, details);
//                _element.DispatchEvent(e);

//                if (Platform.JsonExists(Window.Instance, "jQuery"))
//                {
//                    dynamic jquery = Window.Instance["jQuery"];
//                    jquery(_element).trigger(name, details);
//                }
//            }
//        }

//        public void Render()
//        {
//            if (Renderer == null) return;
//            Action renderAction = null;
//            renderAction = () =>
//            {
//                // if font is not yet loaded, try again in 1 sec
//                if (!Environment.IsFontLoaded)
//                {
//                    Window.SetTimeout(() =>
//                    {
//                        renderAction();
//                    }, 1000);
//                }
//                else
//                {
//                    // when font is finally loaded, start rendering
//                    Renderer.Render(Score, TrackIndexes);
//                }
//            };
//            renderAction();
//        }

//        public void UpdateLayout(object json)
//        {
//            _settings.Layout = Settings.LayoutFromJson(json);
//            Renderer.UpdateSettings(_settings);
//            Renderer.Invalidate();
//        }
//    }
//}
