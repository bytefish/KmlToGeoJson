// Copyright (c) Philipp Wagner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KmlToGeoJson.Model
{
    public class Polygon
    {
        [JsonPropertyName("type")]
        public string Type { get; private set; } = "Polygon";

        [JsonPropertyName("coordinates")]
        public List<float[][]> Coordinates { get; set; }
    }
}
