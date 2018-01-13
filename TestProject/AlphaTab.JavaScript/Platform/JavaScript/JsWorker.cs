//using System;
//using AlphaTab.Model;
//using AlphaTab.Rendering;
//using AlphaTab.Util;
//using Bridge.Html5;

//namespace AlphaTab.Platform.JavaScript
//{
//    public class JsWorker
//    {
//        private ScoreRenderer _renderer;
//        private WindowInstance _main;

//        public JsWorker(WindowInstance main)
//        {
//            _main = main;
//            _main.AddEventListener("message", HandleMessage, false);
//        }

//        public static void Init()
//        {
//            if (!Global.Self.Document.As<bool>())
//            {
//                new JsWorker(Global.Self);
//            }
//        }


//        private void HandleMessage(Event e)
//        {
//            dynamic data = e.As<MessageEvent>().Data;
//            var cmd = data ? data.cmd : "";
//            switch (cmd)
//            {
//                case "alphaTab.initialize":
//                    var settings = Settings.FromJson(data.settings, null);
//                    _renderer = new ScoreRenderer(settings);
//                    _renderer.PartialRenderFinished += result => _main.PostMessage(new { cmd = "alphaTab.partialRenderFinished", result = Std.CleanFromBridgeMeta(result) }, null);
//                    _renderer.RenderFinished += result => _main.PostMessage(new { cmd = "alphaTab.renderFinished", result = Platform.CleanFromBridgeMeta(result) }, null);
//                    _renderer.PostRenderFinished += () => _main.PostMessage(new { cmd = "alphaTab.postRenderFinished", boundsLookup = _renderer.BoundsLookup.ToJson() }, null);
//                    _renderer.PreRender += result => _main.PostMessage(new { cmd = "alphaTab.preRender", result = Platform.CleanFromBridgeMeta(result) }, null);
//                    _renderer.Error += Error;
//                    break;
//                case "alphaTab.invalidate":
//                    _renderer.Invalidate();
//                    break;
//                case "alphaTab.resize":
//                    _renderer.Resize(data.width);
//                    break;
//                case "alphaTab.render":
//                    var converter = new JsonConverter();
//                    var score = converter.JsObjectToScore(data.score);
//                    RenderMultiple(score, data.trackIndexes);
//                    break;
//                case "alphaTab.updateSettings":
//                    UpdateSettings(data.settings);
//                    break;
//            }
//        }

//        private void UpdateSettings(object settings)
//        {
//            _renderer.UpdateSettings(Settings.FromJson(settings, null));
//        }

//        private void RenderMultiple(Score score, int[] trackIndexes)
//        {
//            try
//            {
//                _renderer.Render(score, trackIndexes);
//            }
//            catch (Exception e)
//            {
//                Error("render", e);
//            }
//        }

//        private void Error(string type, Exception e)
//        {
//            Logger.Error(type, "An unexpected error occurred in worker", e);

//            dynamic error = JSON.Parse(JSON.Stringify(e));
//            if (e["message"].As<bool>())
//            {
//                error.message = e["message"];
//            }
//            if (e["stack"].As<bool>())
//            {
//                error.stack = e["stack"];
//            }
//            if (e["constructor"].As<bool>() && e["constructor"]["name"].As<bool>())
//            {
//                error.type = e["constructor"]["name"];
//            }
//            _main.PostMessage(new { cmd = "alphaTab.error", error = new { type = type, detail = Platform.CleanFromBridgeMeta(error) } }, null);
//        }
//    }
//}