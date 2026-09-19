using System.Xml.Linq;

namespace SecretSanta.Web.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void ResourceCulturesContainTheSameKeys()
    {
        var resources = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../SecretSanta.Web/Resources/Localization"));

        var english = ReadKeys(Path.Combine(resources, "SharedResource.en.resx"));
        var russian = ReadKeys(Path.Combine(resources, "SharedResource.ru.resx"));

        Assert.Equal(english, russian);
        Assert.NotEmpty(english);
    }

    [Theory]
    [InlineData("SharedResource.en.resx")]
    [InlineData("SharedResource.ru.resx")]
    public void ResourceValuesAreNotEmpty(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../SecretSanta.Web/Resources/Localization", fileName));
        var emptyKeys = XDocument.Load(path).Root!
            .Elements("data")
            .Where(item => string.IsNullOrWhiteSpace(item.Element("value")?.Value))
            .Select(item => item.Attribute("name")?.Value)
            .ToArray();

        Assert.Empty(emptyKeys);
    }

    private static string[] ReadKeys(string path) => XDocument.Load(path).Root!
        .Elements("data")
        .Select(item => item.Attribute("name")!.Value)
        .Order(StringComparer.Ordinal)
        .ToArray();
}
