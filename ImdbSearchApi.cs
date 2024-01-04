// Copyright (c) M0rb1dD3v1l. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Community.PowerToys.Run.Plugin.ImdbSearch
{
    internal static class ImdbSearchApi
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly JsonSerializerOptions JSOptions;
        private static readonly string UrlTemplateAll =
            "https://v3.sg.media-imdb.com/suggestion/x/{0}.json?includeVideos=0";
        private static readonly string UrlTemplateOnlyTitles =
            "https://v3.sg.media-imdb.com/suggestion/titles/x/{0}.json?includeVideos=0";

        static ImdbSearchApi()
        {
            Client = new HttpClient { Timeout = TimeSpan.FromSeconds(10), };

            // Allow trailing comma
            JSOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
            };
        }

        public static async Task<ImdbSearchResult> QueryImdbSearchAsync(
            string search,
            bool titlesOnly
        )
        {
            var url = string.Format(UrlTemplateAll, search);
            if (titlesOnly)
            {
                url = string.Format(UrlTemplateOnlyTitles, search);
            }

            var response = await Client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                string errorMsg = string.Format(
                    "Failed to query IMDB search! StatusCode: {0}",
                    response.StatusCode
                );
                throw new Exception(errorMsg);
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return new ImdbSearchResult();
            }

            var result = JsonSerializer.Deserialize<ImdbSearchResult>(content, JSOptions);
            if (result == null)
            {
                throw new Exception("Failed to deserialize IMDB search result");
            }

            return result;
        }
    }
}
