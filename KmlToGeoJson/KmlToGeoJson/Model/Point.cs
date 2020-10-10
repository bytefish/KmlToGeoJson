// Copyright (c) Philipp Wagner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace KmlToGeoJson.Model
{
    public class Point
    {
        [JsonPropertyName("type")]
        public string Type { get; private set; } = "Point"; 

        [JsonPropertyName("coordinates")]
        public float[] Coordinates { get; set; }
    }
}