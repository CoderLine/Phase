///*
// * This file is part of alphaTab.
// * Copyright � 2017, Daniel Kuschny and Contributors, All rights reserved.
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
//using AlphaTab.Model;
//using AlphaTab.Rendering;
//using AlphaTab.Rendering.Utils;
//using AlphaTab.Util;
//using Bridge;
//using Bridge.Html5;

//namespace AlphaTab.Platform.JavaScript
//{
//    [External]
//    [Name("Worker")]
//    public class Worker : EventTarget
//    {
//        public extern Worker(string script);

//        [Name("postMessage")]
//        public virtual extern void PostMessage(object message);

//        [Name("terminate")]
//        public extern void Terminate();
//    }
//    [External]
//    [Name("URL")]
//    public class URL
//    {
//        [Name("createObjectURL")]
//        public static extern string CreateObjectURL(Blob blob);
//    }

//    public class WorkerScoreRenderer : IScoreRenderer
//    {
//        private readonly JsApi _api;
//        private readonly Worker _worker;

//        public BoundsLookup BoundsLookup { get; private set; }

//        public WorkerScoreRenderer(JsApi api, Settings settings)
//        {
//            _api = api;
//            try
//            {
//                _worker = new Worker(settings.ScriptFile);
//            }
//            catch
//            {
//                // fallback to blob worker 
//                try
//                {
//                    var script = "importScripts('" + settings.ScriptFile + "')";
//                    var blob = new Blob(new BlobDataObject[] { script });
//                    _worker = new Worker(URL.CreateObjectURL(blob));
//                }
//                catch (Exception e)
//                {
//                    Logger.Error("Rendering", "Failed to create WebWorker: " + e);
//                    // TODO: fallback to synchronous mode
//                }
//            }
//            _worker.PostMessage(new { cmd = "alphaTab.initialize", settings = settings.ToJson() });
//            _worker.AddEventListener("message", HandleWorkerMessage, false);
//        }

//        public void Destroy()
//        {
//            _worker.Terminate();
//        }

//        public void UpdateSettings(Settings settings)
//        {
//            _worker.PostMessage(new { cmd = "alphaTab.updateSettings", settings = settings.ToJson() });
//        }

//        public void Invalidate()
//        {
//            _worker.PostMessage(new { cmd = "alphaTab.invalidate" });
//        }

//        public void Resize(int width)
//        {
//            _worker.PostMessage(new { cmd = "alphaTab.resize", width = width });
//        }

//        private void HandleWorkerMessage(Event e)
//        {
//            dynamic data = e.As<MessageEvent>().Data;
//            string cmd = data.cmd;
//            switch (cmd)
//            {
//                case "alphaTab.preRender":
//                    OnPreRender(data.result);
//                    break;
//                case "alphaTab.partialRenderFinished":
//                    OnPartialRenderFinished(data.result);
//                    break;
//                case "alphaTab.renderFinished":
//                    OnRenderFinished(data.result);
//                    break;
//                case "alphaTab.postRenderFinished":
//                    BoundsLookup = BoundsLookup.FromJson(data.boundsLookup, _api.Score);
//                    OnPostRenderFinished();
//                    break;
//                case "alphaTab.error":
//                    OnError(data.type, data.detail);
//                    break;
//            }
//        }

//        public void Render(Score score, int[] trackIndexes)
//        {
//            var converter = new JsonConverter();
//            score = converter.ScoreToJsObject(score);
//            _worker.PostMessage(new { cmd = "alphaTab.render", score = score, trackIndexes = trackIndexes });
//        }

//        public event Action<RenderFinishedEventArgs> PreRender;
//        protected virtual void OnPreRender(RenderFinishedEventArgs obj)
//        {
//            var handler = PreRender;
//            if (handler != null) handler(obj);
//        }

//        public event Action<RenderFinishedEventArgs> PartialRenderFinished;
//        protected virtual void OnPartialRenderFinished(RenderFinishedEventArgs obj)
//        {
//            var handler = PartialRenderFinished;
//            if (handler != null) handler(obj);
//        }

//        public event Action<RenderFinishedEventArgs> RenderFinished;
//        protected virtual void OnRenderFinished(RenderFinishedEventArgs obj)
//        {
//            var handler = RenderFinished;
//            if (handler != null) handler(obj);
//        }

//        public event Action<string, Exception> Error;
//        protected virtual void OnError(string type, Exception details)
//        {
//            var handler = Error;
//            if (handler != null) handler(type, details);
//        }

//        public event Action PostRenderFinished;
//        protected virtual void OnPostRenderFinished()
//        {
//            var handler = PostRenderFinished;
//            if (handler != null) handler();
//        }
//    }
//}