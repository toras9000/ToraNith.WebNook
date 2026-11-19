using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ToraNith.WebNook.Tests;

public static class TestConfig
{
    public static IConfiguration FromDictionary(Dictionary<string, string?> values)
        => FromKeyValues(values);

    public static IConfiguration FromKeyValues(IEnumerable<KeyValuePair<string, string?>>? values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static IConfiguration FronJson(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
    }
}

public static class TestOptions
{
    public static IOptionsMonitor<T> Create<T>(T currentValue)
        => new TestOptionsMonitor<T>(currentValue);

    private sealed class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue => currentValue;
        public T Get(string? name) => currentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

}