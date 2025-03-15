// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;

namespace KmlToGeoJson.Test
{
    [TestClass]
    public class GeoJsonConverterTests
    {
        private readonly string[] testFileList =
        [
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
        ];

        [TestMethod]
        public void RunFiles()
        {            
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var filename in testFileList)
            {
                var filePath = Path.Combine(basePath, "Resources", "Kml", filename);

                var xml = File.ReadAllText(filePath);

                Exception? caught = null;

                try
                {
                    KmlToGeoJsonConverter.FromKml(xml);
                } 
                catch (Exception ex)
                {
                    caught = ex;
                }

                Assert.IsNull(caught);
            }
        }

        [TestMethod]
        public void RunFiles_Culture_With_Comma_As_Decimal_Separator()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("nb-NO");

            foreach (var filename in testFileList)
            {
                var filePath = Path.Combine(basePath, "Resources", "Kml", filename);

                var xml = File.ReadAllText(filePath);

                Exception? caught = null;

                try
                {
                    KmlToGeoJsonConverter.FromKml(xml);
                }
                catch (Exception ex)
                {
                    caught = ex;
                }

                Assert.IsNull(caught);
            }
        }

    }
}