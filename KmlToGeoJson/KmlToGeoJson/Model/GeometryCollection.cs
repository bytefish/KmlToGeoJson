// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace KmlToGeoJson.Model
{
    public class GeometryCollection
    {
        [JsonPropertyName("type")]
        public string Type { get; private set; } = "GeometryCollection";

        [JsonPropertyName("geometries")]
        public object[] Geometries { get; set; }
    }
}
