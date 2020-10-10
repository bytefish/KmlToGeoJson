// Copyright (c) Philipp Wagner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace KmlToGeoJson.Model
{
    public class LineString
    {
        [JsonPropertyName("type")]
        public string Type { get; private set; } = "LineString";

        [JsonPropertyName("coordinates")]
        public float[][] Coordinates { get; set; }
    }
}
