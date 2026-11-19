using Microsoft.Extensions.Configuration;

namespace ToraNith.WebNook.Tests;

public static class TestExtensions
{
    public static IConfiguration ToConfiguration(this IEnumerable<KeyValuePair<string, string?>> values)
        => TestConfig.FromKeyValues(values);

    public static IConfiguration ToConfiguration(this string json)
        => TestConfig.FronJson(json);
}
