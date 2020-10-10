# KmlToGeoJson #

KmlToGeoJson is a small library to convert from KML to GeoJSON.

## Installing ##

To install KmlToGeoJson, run the following command in the Package Manager Console:

```
PM> Install-Package KmlToGeoJson
```

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

## License ##

KmlToGeoJson is licensed under the MIT License. See [LICENSE](LICENSE) for details.