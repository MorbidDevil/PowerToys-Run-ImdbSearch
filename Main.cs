// Copyright (c) M0rb1dD3v1l. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

namespace Community.PowerToys.Run.Plugin.ImdbSearch
{
    public class Main : IPlugin, IPluginI18n, ISettingProvider, IReloadable, IDisposable
    {
        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        public static string PluginID => "0664cfc968a8473398c4124c2ba657b7";

        private const string ApiUrlTemplateAll = "https://v3.sg.media-imdb.com/suggestion/x/{0}.json?includeVideos=0";
        private const string ApiUrlTemplateOnlyMovies = "https://v3.sg.media-imdb.com/suggestion/titles/x/{0}.json?includeVideos=0";
        private const string ImdbUrlTitleTemplate = "https://www.imdb.com/title/{0}/";
        private const string ImdbUrlActorTemplate = "https://www.imdb.com/name/{0}/";
        private const string SearchOnlyTitles = nameof(SearchOnlyTitles);
        private PluginInitContext? _context;
        private static string? _icon_path;
        private bool _disposed;
        private bool _searchOnlyTitles;

        private static readonly CompositeFormat PluginSearchFailed = System.Text.CompositeFormat.Parse(Properties.Resources.plugin_search_failed);

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = SearchOnlyTitles,
                DisplayLabel = Properties.Resources.plugin_search_only_titles,
                Value = false,
            },
        };

        public List<Result> Query(Query query)
        {
            ArgumentNullException.ThrowIfNull(query);

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>();
            }

            string searchTerm = Uri.EscapeDataString(query.Search.Trim());

            try
            {
                var results = SearchImdb(searchTerm, _searchOnlyTitles).Result;
                return results;
            }
            catch (Exception e)
            {
                Log.Error(GetTranslatedPluginTitle() + ": " + e.Message, GetType());

                var results = new List<Result>();

                string errorMsgString = string.Format(CultureInfo.CurrentCulture, PluginSearchFailed, BrowserInfo.Name ?? BrowserInfo.MSEdgeName);
                results.Add(GetErrorResult(errorMsgString));
                return results;
            }
        }

        public void Init(PluginInitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _searchOnlyTitles = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == SearchOnlyTitles)?.Value ?? false;
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();
        }

        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.plugin_description;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_context != null && _context.API != null)
                    {
                        _context.API.ThemeChanged -= OnThemeChanged;
                    }

                    _disposed = true;
                }
            }
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private static void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _icon_path = "Images/imdb.light.png";
            }
            else
            {
                _icon_path = "Images/imdb.dark.png";
            }
        }

        private static async Task<List<Result>> SearchImdb(string search, bool searchOnlyTitles)
        {
            List<Result> results = new List<Result>();

            string url = string.Format(ApiUrlTemplateAll, search);
            if (searchOnlyTitles)
            {
                url = string.Format(ApiUrlTemplateOnlyMovies, search);
            }

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    results = ParseEntries(json);
                }
                else
                {
                    throw new Exception("response code " + response.StatusCode);
                }
            }

            return results;
        }

        private static List<Result> ParseEntries(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new Exception("json is empty");
            }

            // Allow trailing comma
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
            };

            var root = JsonSerializer.Deserialize<ImdbResponse>(json, options);

            if (root == null || root.d == null)
            {
                throw new Exception("parse error");
            }

            List<Entry> entries = root.d;

            var results = new List<Result>();
            foreach (Entry entry in entries)
            {
                if (entry.id == null)
                {
                    continue;
                }

                string[] prefixes = { "tt", "nm" };
                var isTrash = !prefixes.Any(prefix => entry.id.StartsWith(prefix));

                if (isTrash)
                {
                    continue;
                }

                var isActor = entry.qid == null;

                var result = new Result();

                string arguments = string.Format(ImdbUrlTitleTemplate, entry?.id);

                if (!isActor)
                {
                    result.Title = string.Format("{0} ({1})", entry?.l, entry?.yr != null ? entry.yr : entry?.y);
                    if (entry?.y == null && entry?.yr == null)
                    {
                        result.Title = string.Format("{0}", entry?.l);
                    }

                    result.SubTitle = string.Format("{0}", entry?.s);
                    result.QueryTextDisplay = root.q;
                    result.IcoPath = _icon_path;
                }
                else
                {
                    result.Title = string.Format("{0}", entry?.l);
                    result.SubTitle = string.Format("{0}", entry?.s);
                    result.QueryTextDisplay = root.q;
                    result.IcoPath = _icon_path;

                    arguments = string.Format(ImdbUrlActorTemplate, entry?.id);
                }

                result.ProgramArguments = arguments;
                result.Action = action =>
                {
                    if (!Helper.OpenCommandInShell(BrowserInfo.Path, BrowserInfo.ArgumentsPattern, arguments))
                    {
                        return false;
                    }

                    return true;
                };

                results.Add(result);
            }

            return results;
        }

        private Result GetErrorResult(string errorMessage)
        {
            return new Result
            {
                Title = "Error",
                SubTitle = errorMessage,
                IcoPath = _icon_path,
                Action = _ => { return true; },
            };
        }
    }
}
