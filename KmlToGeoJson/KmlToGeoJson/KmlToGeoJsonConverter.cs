// Copyright (c) Philipp Wagner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;
using KmlToGeoJson.Model;

namespace KmlToGeoJson
{
    /// <summary>
    /// This is a shameless copy from: https://github.com/tmcw/togeojson/blob/master/lib/kml.js, so there is 
    /// no need for taking additional dependencies on third-party libraries. Updates to kml.js should be 
    /// reflected here.
    /// 
    /// The Object Model is intentionally ugly (no abstract classes, no interfaces), because this would make 
    /// Deserialization with .NET Core System.Text.Json complicated or impossible, see the Open Issue at:
    /// https://github.com/dotnet/runtime/issues/30083.
    /// </summary>
    public partial class KmlToGeoJsonConverter
    {
        private static readonly XNamespace Kml = XNamespace.Get("http://www.opengis.net/kml/2.2");
        private static readonly XNamespace Ext = XNamespace.Get("http://www.google.com/kml/ext/2.2");

        private static readonly XName[] Geotypes = new[]
        {
            XName.Get("Polygon", Kml.NamespaceName),
            XName.Get("LineString", Kml.NamespaceName),
            XName.Get("Point", Kml.NamespaceName),
            XName.Get("Track", Kml.NamespaceName),
            XName.Get("Track", Ext.NamespaceName)
        };


        public static string FromKml(string xml)
        {
            var root = XDocument.Parse(xml);

            return FromKml(root);
        }

        public static string FromKml(XDocument document)
        {

            var styleMapIndex = new Dictionary<string, Dictionary<string, string>>();
            var styleByHash = new Dictionary<string, XElement>();
            var styleIndex = new Dictionary<string, string>();

            var root = document.Root;

            if (root.Element(Kml + "Document") != null)
            {
                root = root.Element(Kml + "Document");
            }

            var placemarks = root
                .XPathSelectElements("//*[local-name()='Placemark']")
                .ToList();

            var styles = root
                .XPathSelectElements("//*[local-name()='Style'][@id]")
                .ToList();

            var styleMaps = root
                .XPathSelectElements("//*[local-name()='StyleMap']")
                .ToList();

            foreach (var style in styles)
            {
                if (style.Attribute("id") == null)
                {
                    continue;
                }

                var hash = GetMD5Hash(style.ToString());

                styleIndex["#" + style.Attribute("id")?.Value] = hash;

                styleByHash[hash] = style;
            }

            foreach (var styleMap in styleMaps)
            {
                styleIndex['#' + styleMap.Attribute("id").Value] = GetMD5Hash(styleMap.ToString());

                var pairs = styleMap.Elements(Kml + "Pair").ToList();

                var pairsMap = new Dictionary<string, string>();

                foreach (var pair in pairs)
                {
                    pairsMap[pair.Element(Kml + "key").Value] = pair.Element(Kml + "styleUrl").Value;
                }

                styleMapIndex['#' + styleMap.Attribute("id").Value] = pairsMap;
            }

            List<Feature> features = new List<Feature>();

            foreach (var placemark in placemarks)
            {
                var feature = GetPlacemark(
                  placemark,
                  styleIndex,
                  styleMapIndex,
                  styleByHash
                );

                if (feature != null)
                {
                    features.Add(feature);
                }
            }

            var result = new
            {
                type = "FeatureCollection",
                features = features
            };

            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };

            return JsonSerializer.Serialize(result, options: options);
        }


        private static float[] GetCoordinates(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value
                .Split(new char[] { ' ', ',', '\n' })
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static float[][] GetCoordinatesArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value
                .Split(" ")
                .Select(x => x.Trim())
                .Select(x => GetCoordinates(x))
                .Where(x => x != null)
                .ToArray();
        }

        private static GeometryAndTimes GetGeometries(XElement root)
        {
            var geometries = new List<object>();
            var coordTimes = new List<string[]>();

            if (root.Element(Kml + "MultiGeometry") != null)
            {
                return GetGeometries(root.Element(Kml + "MultiGeometry"));
            }
            if (root.Element(Kml + "MultiTrack") != null)
            {
                return GetGeometries(root.Element(Kml + "MultiTrack"));
            }
            if (root.Element(Ext + "MultiTrack") != null)
            {
                return GetGeometries(root.Element(Ext + "MultiTrack"));
            }

            foreach (var geotype in Geotypes)
            {
                var geometryNodes = root
                    .Elements(geotype)
                    .ToList();

                if (geometryNodes.Count > 0)
                {
                    foreach (var geomNode in geometryNodes)
                    {
                        if (geotype == XName.Get("Point", Kml.NamespaceName))
                        {
                            var coordinates = GetCoordinates(geomNode.Element(Kml + "coordinates").Value);
                            if (coordinates != null)
                            {
                                var point = new Point { Coordinates = coordinates };

                                geometries.Add(point);
                            }
                        }
                        else if (geotype == XName.Get("LineString", Kml.NamespaceName))
                        {
                            var coordinates = GetCoordinatesArray(geomNode.Element(Kml + "coordinates").Value);

                            if (coordinates != null)
                            {
                                var lineString = new LineString { Coordinates = coordinates };

                                geometries.Add(lineString);
                            }
                        }
                        else if (geotype == XName.Get("Polygon", Kml.NamespaceName))
                        {
                            var rings = new List<XElement>();

                            // Get Inner Boundary Linear Ring:
                            var innerBoundaryNode = geomNode.Element(Kml + "innerBoundaryIs");

                            if (innerBoundaryNode != null)
                            {
                                var innerBoundaryRings = innerBoundaryNode.Elements(Kml + "LinearRing");

                                rings.AddRange(innerBoundaryRings);
                            }

                            var outerBoundaryNode = geomNode.Element(Kml + "outerBoundaryIs");

                            if (outerBoundaryNode != null)
                            {
                                var outerBoundaryRings = outerBoundaryNode.Elements(Kml + "LinearRing");

                                rings.AddRange(outerBoundaryRings);
                            }


                            var coordinates = new List<float[][]>();

                            foreach (var ring in rings)
                            {
                                var coordinatesNode = ring.Element(Kml + "coordinates")?.Value;

                                var coordinatesOfRing = GetCoordinatesArray(coordinatesNode);
                                if (coordinatesOfRing != null)
                                {
                                    coordinates.Add(coordinatesOfRing);
                                }
                            }

                            if (coordinates.Count > 0)
                            {
                                var polygon = new Polygon { Coordinates = coordinates };

                                geometries.Add(polygon);
                            }
                        }
                        else if (geotype == XName.Get("Track", Kml.NamespaceName) || geotype == XName.Get("Track", Ext.NamespaceName))
                        {
                            var track = GetGxCoords(geomNode);

                            if (track.Coordinates != null)
                            {
                                var lineString = new LineString { Coordinates = track.Coordinates };
                                geometries.Add(lineString);
                            }

                            var times = track.Times;

                            if (times != null)
                            {
                                coordTimes.Add(track.Times);
                            }
                        }
                    }
                }
            }

            return new GeometryAndTimes { GeometryNodes = geometries.ToArray(), Times = coordTimes.ToArray() };
        }

        private static GxCoords GetGxCoords(XElement root)
        {
            var elements = root
                .Elements(Kml + "coord")
                .ToList();

            var coordinates = new List<float[]>();
            var times = new List<string>();

            // Maybe this is a gx:coord node:
            if (elements.Count == 0)
            {
                elements = root
                    .Elements(Ext + "coord")
                    .ToList();
            }

            foreach (var element in elements)
            {
                var coordinatesForElement = element.Value
                    .Split(" ")
                    .Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture))
                    .ToArray();

                coordinates.Add(coordinatesForElement);
            }

            var timeElems = root.Elements(Kml + "when");

            foreach (var timeElem in timeElems)
            {
                times.Add(timeElem.Value);
            }

            return new GxCoords { Coordinates = coordinates.ToArray(), Times = times.ToArray() };
        }

        public static void SetKmlColor(Dictionary<string, object> properties, XElement elem, string prefix)
        {
            string v = elem.Element(Kml + "color") != null ? elem.Element(Kml + "color").Value : "";

            string colorProp = (prefix == "stroke" || prefix == "fill") ? prefix : prefix + "-color";

            if (!string.IsNullOrWhiteSpace(v) && v.Substring(0, 1) == "#")
            {
                v = v.Substring(1);
            }

            if (v.Length == 6 || v.Length == 3)
            {
                properties[colorProp] = v;
            }
            else if (v.Length == 8)
            {
                properties[prefix + "-opacity"] = Convert.ToByte(v.Substring(0, 2), 16) / 255.0f;
                properties[colorProp] = "#" + v.Substring(6, 2) + v.Substring(4, 2) + v.Substring(2, 2);
            }
        }

        private static void SetNumericProperty(Dictionary<string, object> properties, XElement elem, string source, string target)
        {
            XElement node = elem.Element(Kml + source);

            if (node == null)
            {
                return;
            }

            var nodeValue = node.Value;

            if (string.IsNullOrWhiteSpace(nodeValue))
            {
                return;
            }

            if (float.TryParse(nodeValue, out float result))
            {
                if (!float.IsNaN(result))
                {
                    properties[target] = result;
                }
            }
        }

        private static Feature GetPlacemark(XElement root, Dictionary<string, string> styleIndex, Dictionary<string, Dictionary<string, string>> styleMapIndex, Dictionary<string, XElement> styleByHash)
        {
            var geomsAndTimes = GetGeometries(root);
            var properties = new Dictionary<string, object>();

            // Values:
            var name = root.Element(Kml + "name")?.Value;
            var address = root.Element(Kml + "address")?.Value;
            var styleUrl = root.Element(Kml + "styleUrl")?.Value;
            var description = root.Element(Kml + "description")?.Value;

            // Nodes:
            var timeSpan = root.Element(Kml + "TimeSpan");
            var timeStamp = root.Element(Kml + "TimeStamp");

            // Inline Styles:
            var iconStyle = root.Element(Kml + "Style")?.Element(Kml + "IconStyle");
            var labelStyle = root.Element(Kml + "Style")?.Element(Kml + "LabelStyle");
            var extendedData = root.Element(Kml + "Style")?.Element(Kml + "ExtendedData");
            var lineStyle = root.Element(Kml + "Style")?.Element(Kml + "LineStyle");
            var polyStyle = root.Element(Kml + "Style")?.Element(Kml + "PolyStyle");

            var visibility = root.Element(Kml + "visibility");

            if (!geomsAndTimes.GeometryNodes.Any())
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                properties["name"] = name;
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                properties["address"] = address;
            }

            if (!string.IsNullOrWhiteSpace(styleUrl))
            {
                if (styleUrl[0] != '#')
                {
                    styleUrl = '#' + styleUrl;
                }

                properties["styleUrl"] = styleUrl;

                if (styleIndex.ContainsKey(styleUrl))
                {
                    properties["styleHash"] = styleIndex[styleUrl];
                }

                if (styleMapIndex.ContainsKey(styleUrl))
                {
                    properties["styleMapHash"] = styleMapIndex[styleUrl];
                    properties["styleHash"] = styleIndex[styleMapIndex[styleUrl]["normal"]];
                }

                // Try to populate the lineStyle or polyStyle since we got the style hash
                if (styleByHash.ContainsKey(properties["styleHash"].ToString()))
                {
                    var style = styleByHash.GetValueOrDefault<string, XElement>(properties["styleHash"].ToString(), null);

                    if (iconStyle == null)
                    {
                        iconStyle = style.Element(Kml + "IconStyle");
                    }

                    if (labelStyle == null)
                    {
                        labelStyle = style.Element(Kml + "LabelStyle");
                    }

                    if (lineStyle == null)
                    {
                        lineStyle = style.Element(Kml + "LineStyle");
                    }

                    if (polyStyle == null)
                    {
                        polyStyle = style.Element(Kml + "PolyStyle");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                properties["description"] = description;
            }

            if (timeSpan != null)
            {
                var begin = timeSpan.Element(Kml + "begin").Value;
                var end = timeSpan.Element(Kml + "end").Value;

                properties["timespan"] = new Dictionary<string, string>() {
                    { "begin", begin },
                    { "end", end }
                };
            }

            if (timeStamp != null)
            {
                properties["timestamp"] = timeStamp.Element(Kml + "when").Value;
            }

            if (iconStyle != null)
            {
                SetKmlColor(properties, iconStyle, "icon");

                SetNumericProperty(properties, iconStyle, "scale", "icon-scale");
                SetNumericProperty(properties, iconStyle, "heading", "icon-heading");

                XElement hotspot = iconStyle.Element(Kml + "hotSpot");

                if (hotspot != null)
                {
                    float left = float.Parse(hotspot.Attribute("x").Value, System.Globalization.CultureInfo.InvariantCulture);
                    float top = float.Parse(hotspot.Attribute("y").Value, System.Globalization.CultureInfo.InvariantCulture);
                    if (!float.IsNaN(left) && !float.IsNaN(top))
                    {
                        properties["icon-offset"] = new float[] { left, top };
                    }
                }

                var icon = iconStyle.Element(Kml + "Icon");
                if (icon != null)
                {
                    var href = icon.Element(Kml + "href")?.Value;
                    if (!string.IsNullOrWhiteSpace(href))
                    {
                        properties["icon"] = href;
                    }
                }
            }

            if (labelStyle != null)
            {
                SetKmlColor(properties, labelStyle, "label");
                SetNumericProperty(properties, labelStyle, "scale", "label-scale");
            }

            if (lineStyle != null)
            {
                SetKmlColor(properties, lineStyle, "stroke");
                SetNumericProperty(properties, lineStyle, "width", "stroke-width");
            }

            if (polyStyle != null)
            {
                SetKmlColor(properties, polyStyle, "fill");

                var fill = polyStyle.Element(Kml + "fill")?.Value;
                var outline = polyStyle.Element(Kml + "outline")?.Value;

                if (!string.IsNullOrWhiteSpace(fill) && !properties.ContainsKey("fill-opacity"))
                {
                    properties["fill-opacity"] = fill == "1" ? 1 : 0;
                }

                if (!string.IsNullOrWhiteSpace(outline) && !properties.ContainsKey("stroke-opacity"))
                {
                    properties["stroke-opacity"] = outline == "1" ? 1 : 0;
                }
            }

            if (extendedData != null)
            {
                var datas = extendedData.Elements(Kml + "Data");

                foreach (var data in datas)
                {
                    properties[data.Attribute("name").Value] = data.Element(Kml + "value").Value;
                }

                // Also parse all Schema Datas:
                var schemaDatas = extendedData.Elements(Kml + "SchemaData");

                foreach (var schemaData in schemaDatas)
                {
                    var simpleDatas = schemaData.Elements(Kml + "SimpleData");

                    foreach (var simpleData in simpleDatas)
                    {
                        properties[simpleData.Attribute("name").Value] = simpleData.Value;
                    }
                }
            }

            if (visibility != null)
            {
                properties["visibility"] = visibility.Value;
            }

            if (geomsAndTimes.GeometryNodes.Length > 0)
            {
                if (geomsAndTimes.Times.Length > 0)
                {
                    if (geomsAndTimes.Times.Length == 1)
                    {
                        properties["coordTimes"] = geomsAndTimes.Times[0];
                    }
                    else
                    {
                        properties["coordTimes"] = geomsAndTimes.Times;
                    }
                }
            }

            var id = root.Attribute("id") != null ? root.Attribute("id").Value : default;

            if (geomsAndTimes.GeometryNodes.Length == 1)
            {
                return new Feature
                {
                    Id = id,
                    Geometry = geomsAndTimes.GeometryNodes[0],
                    Properties = properties
                };
            }

            var geometryCollection = new GeometryCollection
            {
                Geometries = geomsAndTimes.GeometryNodes
            };

            return new Feature
            {
                Id = id,
                Geometry = geometryCollection,
                Properties = properties
            };
        }

        private static string GetMD5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}