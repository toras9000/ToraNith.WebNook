# ToraNith.WebNook

[![NugetShield]][NugetPackage]

[NugetPackage]: https://www.nuget.org/packages/ToraNith.WebNook
[NugetShield]: https://img.shields.io/nuget/v/ToraNith.WebNook

## Examples

### ConfigureForwardedSettings

Example Configuration in `appsettings.json`.

```json
{
    // …
    "ForwardedSettings": {
        "ClearDefaultNetworks": true,
        "ForwardedHeaders": "XForwardedFor, XForwardedProto",
        "ForwardLimit": 1,
        "KnownNetworks": [ "192.168.1.0/24" ],
        "KnownProxies": [ "192.168.1.100" ],
        "AllowedHosts": [ "*.myserver.localhost" ],
        "RequireHeaderSymmetry": true,
        "ForwardedForHeaderName": "X-Forwarded-For",
        "ForwardedHostHeaderName": "X-Forwarded-Host",
        "ForwardedProtoHeaderName": "X-Forwarded-Proto",
        "ForwardedPrefixHeaderName": "X-Forwarded-Prefix"
    },
    // …
}
```

DI container configuration.

```csharp
builder.Services.ConfigureForwardedSettings(
    builder.Configuration.GetSection("ForwardedSettings")
);
```

### SimpleAuthHandler

Simple authentication configuration.

```csharp
builder.Services.AddAuthentication(nameof(SimpleAuthHandler))
    .AddScheme<SimpleAuthSchemeOptions, SimpleAuthHandler>(nameof(SimpleAuthHandler), default);
builder.Services.AddOptions<SimpleAuthSchemeOptions>(nameof(SimpleAuthHandler))
    .Configure(options =>
    {
        options.UseValidator(header =>
        {
            if (header.Scheme != "Bearer") return null;

            return header.Parameter == "secret-keyword"
                    ? new SimpleAuthSuccess("AuthName", "AuthRole")
                    : new SimpleAuthFailed("");
        });
    });
```