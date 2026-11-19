using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using IPNetwork = System.Net.IPNetwork;

namespace ToraNith.WebNook.Tests;

[TestClass]
public sealed class ConfigureForwardedHeadersExtensionsTests
{
    [TestMethod]
    public void GetForwardedSettings_Value1()
    {
        var config = TestConfig.FromDictionary(new()
        {
            ["ClearDefaultNetworks"] = "true",
            ["ForwardedHeaders"] = "XForwardedFor | XForwardedHost",
            ["ForwardLimit"] = "30",
            ["KnownNetworks:0"] = "192.168.100.0/24",
            ["KnownNetworks:1"] = "172.17.0.0/12",
            ["KnownProxies:0"] = "192.168.100.99",
            ["KnownProxies:1"] = "172.17.0.2",
            ["AllowedHosts:0"] = "example.com",
            ["AllowedHosts:1"] = "*.myserver.localhost",
            ["RequireHeaderSymmetry"] = "true",
            ["ForwardedForHeaderName"] = "X-FWD-FOR",
            ["ForwardedHostHeaderName"] = "X-FWD-HOST",
            ["ForwardedProtoHeaderName"] = "X-FWD-PROTO",
            ["ForwardedPrefixHeaderName"] = "X-FWD-PREFIX",
        });

        var settings = config.GetForwardedSettings();
        settings.ClearDefaultNetworks.Should().Be(true);
        settings.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost);
        settings.ForwardLimit.Should().Be(30);
        settings.KnownNetworks.Should().BeEquivalentTo([IPNetwork.Parse("192.168.100.0/24"), IPNetwork.Parse("172.17.0.0/12")]);
        settings.KnownProxies.Should().BeEquivalentTo([IPAddress.Parse("192.168.100.99"), IPAddress.Parse("172.17.0.2")]);
        settings.AllowedHosts.Should().BeEquivalentTo(["example.com", "*.myserver.localhost"]);
        settings.RequireHeaderSymmetry.Should().Be(true);
        settings.ForwardedForHeaderName.Should().Be("X-FWD-FOR");
        settings.ForwardedHostHeaderName.Should().Be("X-FWD-HOST");
        settings.ForwardedProtoHeaderName.Should().Be("X-FWD-PROTO");
        settings.ForwardedPrefixHeaderName.Should().Be("X-FWD-PREFIX");
    }

    [TestMethod]
    public void GetForwardedSettings_Value2()
    {
        var config = TestConfig.FromDictionary(new()
        {
            ["ClearDefaultNetworks"] = "false",
            ["ForwardedHeaders"] = "XForwardedPrefix, XForwardedProto",
            ["ForwardLimit"] = "-1",
            ["RequireHeaderSymmetry"] = "false",
        });

        var settings = config.GetForwardedSettings();
        settings.ClearDefaultNetworks.Should().Be(false);
        settings.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedPrefix | ForwardedHeaders.XForwardedProto);
        settings.ForwardLimit.Should().Be(-1);
        settings.RequireHeaderSymmetry.Should().Be(false);
    }

    [TestMethod]
    public void GetForwardedSettings_NoKeys()
    {
        var config = TestConfig.FromDictionary(new() { });

        var settings = config.GetForwardedSettings();
        settings.ClearDefaultNetworks.Should().BeNull();
        settings.ForwardedHeaders.Should().BeNull();
        settings.ForwardLimit.Should().BeNull();
        settings.KnownNetworks.Should().BeNull();
        settings.KnownProxies.Should().BeNull();
        settings.AllowedHosts.Should().BeNull();
        settings.RequireHeaderSymmetry.Should().BeNull();
        settings.ForwardedForHeaderName.Should().BeNull();
        settings.ForwardedHostHeaderName.Should().BeNull();
        settings.ForwardedProtoHeaderName.Should().BeNull();
        settings.ForwardedPrefixHeaderName.Should().BeNull();
    }

    [TestMethod]
    public void GetForwardedSettings_Exception()
    {
        FluentActions.Invoking(() => TestConfig.FromDictionary(new() { ["ClearDefaultNetworks"] = "XXX", }).GetForwardedSettings())
            .Should().Throw<Exception>();

        FluentActions.Invoking(() => TestConfig.FromDictionary(new() { ["ForwardedHeaders"] = "XXX", }).GetForwardedSettings())
            .Should().Throw<Exception>();

        FluentActions.Invoking(() => TestConfig.FromDictionary(new() { ["ForwardLimit"] = "XXX", }).GetForwardedSettings())
            .Should().Throw<Exception>();

        FluentActions.Invoking(() => TestConfig.FromDictionary(new() { ["KnownNetworks:0"] = "XXX", }).GetForwardedSettings())
            .Should().Throw<Exception>();

        FluentActions.Invoking(() => TestConfig.FromDictionary(new() { ["KnownProxies:0"] = "XXX", }).GetForwardedSettings())
            .Should().Throw<Exception>();

        FluentActions.Invoking(() => TestConfig.FromDictionary(new() { ["RequireHeaderSymmetry"] = "XXX", }).GetForwardedSettings())
            .Should().Throw<Exception>();
    }

    [TestMethod]
    public void GetForwardedHeaders()
    {
        TestConfig.FromDictionary(new() { ["TEST"] = "XForwardedFor | XForwardedHost ,XForwardedPrefix, XForwardedProto" })
            .GetForwardedHeaders("TEST")
            .Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedPrefix | ForwardedHeaders.XForwardedProto);

        TestConfig.FromDictionary(new() { ["TEST"] = "All" })
            .GetForwardedHeaders("TEST")
            .Should().Be(ForwardedHeaders.All);

        TestConfig.FromDictionary(new() { ["TEST"] = "None" })
            .GetForwardedHeaders("TEST")
            .Should().Be(ForwardedHeaders.None);

        TestConfig.FromDictionary(new() { ["TEST"] = "" })
            .GetForwardedHeaders("TEST")
            .Should().BeNull();

        TestConfig.FromDictionary(new() { ["TEST"] = "XForwardedFor | XForwardedHost ,XForwardedPrefix, XForwardedProto" })
            .GetForwardedHeaders("OTHER")
            .Should().BeNull();
    }

    [TestMethod]
    public void GetForwardedHeaders_Exception()
    {
        FluentActions.Invoking(() => TestConfig.FromDictionary(new() { ["TEST"] = "XXX", }).GetForwardedHeaders("TEST"))
            .Should().Throw<Exception>();
    }

    [TestMethod]
    public void ConfigureForwardedSettings_Settings()
    {
        var settings = new ForwardedHeadersSettings();
        settings.ClearDefaultNetworks = true;
        settings.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost;
        settings.ForwardLimit = 30;
        settings.KnownNetworks = [
            IPNetwork.Parse("192.168.100.0/24"),
            IPNetwork.Parse("172.17.0.0/12"),
        ];
        settings.KnownProxies = [
            IPAddress.Parse("192.168.100.99"),
            IPAddress.Parse("172.17.0.2")
        ];
        settings.AllowedHosts = [
            "example.com",
            "*.myserver.localhost"
        ];
        settings.RequireHeaderSymmetry = true;
        settings.ForwardedForHeaderName = "X-FWD-FOR";
        settings.ForwardedHostHeaderName = "X-FWD-HOST";
        settings.ForwardedProtoHeaderName = "X-FWD-PROTO";
        settings.ForwardedPrefixHeaderName = "X-FWD-PREFIX";

        using var services = new ServiceCollection()
            .ConfigureForwardedSettings(settings)
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost);
        options.ForwardLimit.Should().Be(30);
        var a0 = options.KnownIPNetworks[0];
        var a1 = options.KnownIPNetworks[1];
        options.KnownIPNetworks.ToArray().Should().BeEquivalentTo([IPNetwork.Parse("192.168.100.0/24"), IPNetwork.Parse("172.17.0.0/12")]);
        options.KnownProxies.Should().BeEquivalentTo([IPAddress.Parse("192.168.100.99"), IPAddress.Parse("172.17.0.2")]);
        options.AllowedHosts.Should().BeEquivalentTo(["example.com", "*.myserver.localhost"]);
        options.RequireHeaderSymmetry.Should().Be(true);
        options.ForwardedForHeaderName.Should().Be("X-FWD-FOR");
        options.ForwardedHostHeaderName.Should().Be("X-FWD-HOST");
        options.ForwardedProtoHeaderName.Should().Be("X-FWD-PROTO");
        options.ForwardedPrefixHeaderName.Should().Be("X-FWD-PREFIX");
    }

    [TestMethod]
    public void ConfigureForwardedSettings_Configuration()
    {
        var config = TestConfig.FromDictionary(new()
        {
            ["ClearDefaultNetworks"] = "true",
            ["ForwardedHeaders"] = "XForwardedFor | XForwardedHost",
            ["ForwardLimit"] = "30",
            ["KnownNetworks:0"] = "192.168.100.0/24",
            ["KnownNetworks:1"] = "172.17.0.0/12",
            ["KnownProxies:0"] = "192.168.100.99",
            ["KnownProxies:1"] = "172.17.0.2",
            ["AllowedHosts:0"] = "example.com",
            ["AllowedHosts:1"] = "*.myserver.localhost",
            ["RequireHeaderSymmetry"] = "true",
            ["ForwardedForHeaderName"] = "X-FWD-FOR",
            ["ForwardedHostHeaderName"] = "X-FWD-HOST",
            ["ForwardedProtoHeaderName"] = "X-FWD-PROTO",
            ["ForwardedPrefixHeaderName"] = "X-FWD-PREFIX",
        });

        using var services = new ServiceCollection()
            .ConfigureForwardedSettings(config)
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost);
        options.ForwardLimit.Should().Be(30);
        var a0 = options.KnownIPNetworks[0];
        var a1 = options.KnownIPNetworks[1];
        options.KnownIPNetworks.ToArray().Should().BeEquivalentTo([IPNetwork.Parse("192.168.100.0/24"), IPNetwork.Parse("172.17.0.0/12")]);
        options.KnownProxies.Should().BeEquivalentTo([IPAddress.Parse("192.168.100.99"), IPAddress.Parse("172.17.0.2")]);
        options.AllowedHosts.Should().BeEquivalentTo(["example.com", "*.myserver.localhost"]);
        options.RequireHeaderSymmetry.Should().Be(true);
        options.ForwardedForHeaderName.Should().Be("X-FWD-FOR");
        options.ForwardedHostHeaderName.Should().Be("X-FWD-HOST");
        options.ForwardedProtoHeaderName.Should().Be("X-FWD-PROTO");
        options.ForwardedPrefixHeaderName.Should().Be("X-FWD-PREFIX");
    }

    [TestMethod]
    public void ConfigureForwardedSettings_NoConfig()
    {
        var defaultOptions = default(ForwardedHeadersOptions);
        {
            using var services = new ServiceCollection()
                .Configure<ForwardedHeadersOptions>(o => { })
                .BuildServiceProvider();

            defaultOptions = services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        }

        var emptyConfigOptions = default(ForwardedHeadersOptions);
        {
            var config = TestConfig.FromDictionary(new() { });

            using var services = new ServiceCollection()
                .ConfigureForwardedSettings(config)
                .BuildServiceProvider();

            emptyConfigOptions = services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        }

        emptyConfigOptions.Should().BeEquivalentTo(
            defaultOptions,
            options => options.Excluding(x => x.KnownIPNetworks)
        );
        emptyConfigOptions.KnownIPNetworks.ToArray().Should().BeEquivalentTo(defaultOptions.KnownIPNetworks);
    }
}
