// Copyright (c) M0rb1dD3v1l. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.ImdbSearch
{
    internal class ImdbSearchResult
    {
        [JsonPropertyName("d")]
        public List<Entry>? ResultList { get; set; }

        [JsonPropertyName("q")]
        public string? Query { get; set; }

        [JsonPropertyName("v")]
        public int? V { get; set; }
    }

    internal class Image
    {
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }

    internal class Video
    {
        [JsonPropertyName("i")]
        public Image? Image { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("l")]
        public string? Title { get; set; }

        [JsonPropertyName("s")]
        public string? Runtime { get; set; }
    }

    internal class Entry
    {
        [JsonPropertyName("i")]
        public Image? Image { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("l")]
        public string? Title { get; set; }

        [JsonPropertyName("q")]
        public string? Type { get; set; }

        [JsonPropertyName("qid")]
        public string? TypeId { get; set; }

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }

        [JsonPropertyName("s")]
        public string? AdditionalInfo { get; set; }

        [JsonPropertyName("v")]
        public List<Video>? Videos { get; set; }

        [JsonPropertyName("vt")]
        public int? Vt { get; set; }

        [JsonPropertyName("y")]
        public int? Year { get; set; }

        [JsonPropertyName("yr")]
        public string? Years { get; set; }
    }
}
