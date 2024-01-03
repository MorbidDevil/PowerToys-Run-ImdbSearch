// Copyright (c) M0rb1dD3v1l. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace Community.PowerToys.Run.Plugin.ImdbSearch
{
    public class Image
    {
        public int? height { get; set; }

        public string? imageUrl { get; set; }

        public int? width { get; set; }
    }

    public class Video
    {
        public Image? i { get; set; }

        public string? id { get; set; }

        public string? l { get; set; }

        public string? s { get; set; }
    }

    public class Entry
    {
        public Image? i { get; set; }

        public string? id { get; set; }

        public string? l { get; set; }

        public string? q { get; set; }

        public string? qid { get; set; }

        public int? rank { get; set; }

        public string? s { get; set; }

        public List<Video>? v { get; set; }

        public int? vt { get; set; }

        public int? y { get; set; }

        public string? yr { get; set; }
    }

    public class ImdbResponse
    {
        public List<Entry>? d { get; set; }

        public string? q { get; set; }

        public int? v { get; set; }
    }
}
