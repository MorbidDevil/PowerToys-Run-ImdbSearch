// Copyright (c) M0rb1dD3v1l. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

        private const string ApiUrlTemplateAll =
            "https://v3.sg.media-imdb.com/suggestion/x/{0}.json?includeVideos=0";
        private const string ApiUrlTemplateOnlyMovies =
            "https://v3.sg.media-imdb.com/suggestion/titles/x/{0}.json?includeVideos=0";
        private const string ImdbUrlTitleTemplate = "https://www.imdb.com/title/{0}/";
        private const string ImdbUrlActorTemplate = "https://www.imdb.com/name/{0}/";
        private const string SearchOnlyTitles = nameof(SearchOnlyTitles);
        private PluginInitContext? _context;
        private static string? _icon_path;
        private bool _disposed;
        private bool _searchOnlyTitles;

        private static readonly CompositeFormat PluginSearchFailed =
            System.Text.CompositeFormat.Parse(Properties.Resources.plugin_search_failed);

        public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
            new List<PluginAdditionalOption>()
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
            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>();
            }

            string searchTerm = Uri.EscapeDataString(query.Search.Trim());

            var searchResult = new ImdbSearchResult();

            try
            {
                searchResult = ImdbSearchApi
                    .QueryImdbSearchAsync(searchTerm, _searchOnlyTitles)
                    .Result;
            }
            catch (Exception e)
            {
                string errorMsgString = string.Format(
                    CultureInfo.CurrentCulture,
                    Properties.Resources.plugin_search_failed,
                    e.Message
                );
                Log.Error(errorMsgString, GetType());
                return new List<Result>([GetErrorResult(e.Message)]);
            }

            if (searchResult?.ResultList == null)
            {
                return new List<Result>();
            }

            // Define the prefixes to search for
            string[] prefixes = { "tt", "nm" };

            // Filter the search results based on the prefixes
            var results = searchResult
                .ResultList.Where(
                    entry => entry.Id != null && prefixes.Any(prefix => entry.Id.StartsWith(prefix))
                )
                .ToList();

            var result = new List<Result>();
            foreach (var entry in results)
            {
                bool isActor = entry.TypeId == null;

                var resultItem = new Result
                {
                    Title = string.Format("{0}", entry?.Title),
                    SubTitle = string.Format("{0}", entry?.AdditionalInfo),
                    QueryTextDisplay = searchResult.Query,
                    IcoPath = _icon_path,
                };

                if (!isActor)
                {
                    resultItem.Title = string.Format(
                        "{0} ({1})",
                        entry?.Title,
                        entry?.Years != null ? entry.Years : entry?.Year
                    );
                    if (entry?.Year == null && entry?.Years == null)
                    {
                        resultItem.Title = string.Format("{0}", entry?.Title);
                    }
                }

                resultItem.Action = action =>
                {
                    if (
                        !Helper.OpenCommandInShell(
                            BrowserInfo.Path,
                            BrowserInfo.ArgumentsPattern,
                            isActor
                                ? string.Format(ImdbUrlActorTemplate, entry?.Id)
                                : string.Format(ImdbUrlTitleTemplate, entry?.Id)
                        )
                    )
                    {
                        return false;
                    }

                    return true;
                };

                result.Add(resultItem);
            }

            return result;
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
            _searchOnlyTitles =
                settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == SearchOnlyTitles)?.Value
                ?? false;
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

        private static Result GetErrorResult(string errorMessage)
        {
            return new Result
            {
                Title = "Error",
                SubTitle = errorMessage,
                IcoPath = _icon_path,
                Action = _ =>
                {
                    return true;
                },
            };
        }
    }
}
