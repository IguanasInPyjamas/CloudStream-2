﻿using System;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
    class FilesClubProvider : BloatFreeMovieProvider
    {
        public override string Name => "123FilesClub";
        public override bool NullMetadata => true;
        public FilesClubProvider(CloudStreamCore _core) : base(_core) { }

        public static string GetFileFromEvalData(string d)
        {
            const string _lookFor = "eval(function";
            string code = "";
            while (d.Contains(_lookFor)) {
                code = _lookFor + FindHTML(d, _lookFor, "</script");
                if (code.Contains("jwplayer")) break;
                d = RemoveOne(d, _lookFor);
            }
            return FindHTML(GetEval(code), "file:\'", "\'");
        }

        public static string GetEval(string code)
        {
            Jint.Engine e = new Jint.Engine().SetValue("log_alert", new Action<string>((a) => {
                if (a.Contains("log_alert")) {
                    a = a.Replace("log_alert", "eval");
                }
                code = a;
            }));
            while (code.StartsWith("eval")) {
                code = code.Replace("eval", "log_alert");
                e.Execute(code);
            }

            return code;
        }

        public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
        {
            string imdbId = activeMovie.title.id;
            string random = rng.Next(0, 10000).ToString();
            string random2 = rng.Next(0, 10000).ToString();
            string bound = $"----WebKitFormBoundary{random}";
            string data = $"--{bound}\nContent-Disposition: form-data; name=\"referer\"\n\nffmovie.fun\n--{bound}\nContent-Disposition: form-data; name=\"SubmitButtoncolors\"\n\n{random2}\n--{bound}--";

            string year = "";
            try {
                year = $"&y={activeMovie.title.year[0..4]}";
            }
            catch (Exception) {

            }

            string request = (isMovie ? "https://123files.club/imdb/play/?id=" : "https://123files.club/imdb/tv/?id=") + imdbId + year + (isMovie ? "" : $"&s={season}&e={episode}");
            string d = core.PostRequest(request, request, data, tempThred, $"multipart/form-data; boundary={bound}");

            var _thread = core.CreateThread(3);
            core.StartThread(Name + " ExtraThread", () => {
                string __d = (string)d.Clone();
                const string _lookFor = "data-id=\"";
                while (__d.Contains(_lookFor)) {
                    string _ref = FindHTML(__d, _lookFor, "\"");

                    void EvalRef(string __ref)
                    {
                        string _ev = DownloadString("https://123files.club" + __ref, referer: request, tempThred: _thread);
                        if (_ev == "") return;

                        string evalData = GetFileFromEvalData(_ev);

                        if (evalData == "") {
                            string realSite = FindHTML(_ev, "src=\"", "\"");
                            if (realSite != "" && !realSite.StartsWith("http")) {
                                EvalRef(realSite);
                            }
                        }
                        else {
                            evalData = evalData.Replace("#", "");
                            if (evalData.Length > 10) {
                                AddPotentialLink(normalEpisode, evalData, "FilesClub", 5);
                            }
                        }
                    }
                    EvalRef(_ref);

                    __d = RemoveOne(__d, _lookFor);
                }
            });

            string _downloadLink = FindHTML(d, "<a href=\"/download/", "\"");
            if (_downloadLink != "") {
                _downloadLink = "https://123files.club/download/" + _downloadLink;
                string dSource = core.PostRequest(_downloadLink, _downloadLink, $"imdb={imdbId}&imdbGO=");
                const string lookFor = "<a href=\"";
                while (dSource.Contains(lookFor)) {
                    string link = FindHTML(dSource, lookFor, "\"");

                    // DONT USE mixdrop.co, THEY HAVE RECAPTCHA TO GET TOKEN
                    if (link.Contains("googleusercontent")) {
                        AddPotentialLink(normalEpisode, link, "GoogleVideo Files", 13);
                    }

                    dSource = RemoveOne(dSource, lookFor);
                }
            }
        }
    }
}