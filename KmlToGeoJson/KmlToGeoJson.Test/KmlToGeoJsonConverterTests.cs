// Copyright (c) Philipp Wagner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NUnit.Framework;
using System;
using System.IO;

namespace KmlToGeoJson.Test
{
    public class GeoJsonConverterTests
    {
        private System.Globalization.CultureInfo _defaultCulture;
        private readonly string[] testFileList = new[]
        {
            "addresses.kml",
            "cdata.kml",
            "extended_data.kml",
            "gxmultitrack.kml",
            "gxtrack.kml",
            "inline_style.kml",
            "linestring.kml",
            "literal_color.kml",
            "multigeometry.kml",
            "multigeometry_discrete.kml",
            "multitrack.kml",
            "nogeomplacemark.kml",
            "non_gx_multitrack.kml",
            "noname.kml",
            "opacity_override.kml",
            "point.kml",
            "point_id.kml",
            "polygon.kml",
            "selfclosing.kml",
            "simpledata.kml",
            "style.kml",
            "style_url.kml",
            "timespan.kml",
            "twopoints.kml"
        };
        [OneTimeSetUp]
        public void Setup()
        {
            _defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        }
        [TearDown]
        public void TearDown()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = _defaultCulture;
        }
        [Test]
        public void RunFiles()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var filename in testFileList)
            {
                var filePath = Path.Combine(basePath, "Resources", "Kml", filename);

                var xml = File.ReadAllText(filePath);

                Assert.DoesNotThrow(() => KmlToGeoJsonConverter.FromKml(xml));
            }
        }
        [Test]
        public void RunFiles_Culture_With_Comma_As_Decimal_Separator()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("nb-NO");
            foreach (var filename in testFileList)
            {
                var filePath = Path.Combine(basePath, "Resources", "Kml", filename);

                var xml = File.ReadAllText(filePath);

                Assert.DoesNotThrow(() => KmlToGeoJsonConverter.FromKml(xml));
            }
        }

    }
}