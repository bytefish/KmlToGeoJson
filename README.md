# KmlToGeoJson #

KmlToGeoJson is a small library to convert from KML to GeoJSON.

## Example Usage ##

```csharp
namespace KmlToGeoJson.Test
{
    public class GeoJsonConverterTests
    {
        [Test]
        public void Test_ConvertFromKmlToGeoJSON()
        {
            var xml = File.ReadAllText("style.kml");
            var json =  KmlToGeoJsonConverter.FromKml(xml);
            
            // Now you can work with the GeoJSON data ...
        }
    }
}
```