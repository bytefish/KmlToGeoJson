// Copyright (c) Philipp Wagner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KmlToGeoJson.Model
{
    public class Feature
    {
        [JsonPropertyName("feature")]
        public string Type { get; private set; } = "Feature";

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("geometry")]
        public object Geometry { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }
}
