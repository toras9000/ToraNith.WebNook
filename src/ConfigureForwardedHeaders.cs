using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IPNetwork = System.Net.IPNetwork;

namespace ToraNith.WebNook;

/// <summary>転送ヘッダ設定</summary>
/// <remarks>
/// このクラスは ForwardedHeadersOptions を設定するための情報を表すものとなる。
/// これは直接利用することを想定したものではなく、構成情報(IConfiguration)から設定を読み取る構造を表現することを主な目的としている。
/// (可能であれば ForwardedHeadersOptions を直接設定する方が容易である)
/// 構成情報からはこのクラスのメンバ名と同じプロパティ値を参照するため、この型を構成のサンプルとして見ることができる。
/// 各メンバの値が null である場合(構成情報にプロパティが無い場合)、その設定を変更しないことを意味する。
/// </remarks>
public class ForwardedHeadersSettings
{
    /// <summary>デフォルトの Known ネットワークをクリアするか否か</summary>
    public bool? ClearDefaultNetworks { get; set; }

    /// <summary>利用するフォワードヘッダ種別</summary>
    public ForwardedHeaders? ForwardedHeaders { get; set; }

    /// <summary>処理するフォワードエントリ数</summary>
    /// <remarks>この値が負値である場合、無制限を意味する。</remarks>
    public int? ForwardLimit { get; set; }

    /// <summary>転送ヘッダを受け入れる既知のプロキシアドレス範囲</summary>
    public IReadOnlyList<IPNetwork>? KnownNetworks { get; set; }

    /// <summary>転送ヘッダを受け入れる既知のプロキシアドレス</summary>
    public IReadOnlyList<IPAddress>? KnownProxies { get; set; }

    /// <summary>ホスト名の転送ヘッダで許容されるホスト名</summary>
    public IReadOnlyList<string>? AllowedHosts { get; set; }

    /// <summary>フォワードヘッダ間でエントリ数が一致している必要があるか否か</summary>
    public bool? RequireHeaderSymmetry { get; set; }

    /// <summary>元のクライアントIPアドレスを転送するHTTPヘッダー名</summary>
    public string? ForwardedForHeaderName { get; set; }

    /// <summary>元のホスト名を転送するHTTPヘッダー名</summary>
    public string? ForwardedHostHeaderName { get; set; }

    /// <summary>元の通信プロトコルを転送するHTTPヘッダー名</summary>
    public string? ForwardedProtoHeaderName { get; set; }

    /// <summary>元のスプレフィックスを転送するHTTPヘッダー名</summary>
    public string? ForwardedPrefixHeaderName { get; set; }
}

/// <summary>ForwardedHeadersOptions の構成用拡張メソッド</summary>
public static class ConfigureForwardedHeadersExtensions
{
    /// <summary>構成情報から ForwardedHeadersSettings を読み取る</summary>
    /// <param name="config">読み取り元の構成セクション</param>
    /// <returns>読み取った設定</returns>
    public static ForwardedHeadersSettings GetForwardedSettings(this IConfiguration config)
    {
        // 構成を読み取って設定内容を準備
        var settings = new ForwardedHeadersSettings();
        settings.ClearDefaultNetworks = config.GetValue<bool?>(nameof(ForwardedHeadersSettings.ClearDefaultNetworks));
        settings.ForwardedHeaders = config.GetForwardedHeaders(nameof(ForwardedHeadersSettings.ForwardedHeaders));
        settings.ForwardLimit = config.GetValue<int?>(nameof(ForwardedHeadersSettings.ForwardLimit));
        if (config.GetSection(nameof(ForwardedHeadersSettings.KnownNetworks)).Get<string[]>() is { } allowNetworks)
        {
            settings.KnownNetworks = allowNetworks
                .Where(allow => !string.IsNullOrWhiteSpace(allow))
                .Select(allow => IPNetwork.TryParse(allow, out var nw) ? nw : throw new FormatException($"Illegal KnownNetworks format"))
                .ToArray();
        }
        if (config.GetSection(nameof(ForwardedHeadersSettings.KnownProxies)).Get<string[]>() is { } allowProxies)
        {
            settings.KnownProxies = allowProxies
                .Where(allow => !string.IsNullOrWhiteSpace(allow))
                .Select(allow => IPAddress.TryParse(allow, out var ip) ? ip : throw new FormatException($"Illegal KnownProxies format"))
                .ToArray();
        }
        if (config.GetSection(nameof(ForwardedHeadersSettings.AllowedHosts)).Get<string[]>() is { } allowHosts)
        {
            settings.AllowedHosts = allowHosts
                .Where(allow => !string.IsNullOrWhiteSpace(allow))
                .ToArray();
        }
        settings.RequireHeaderSymmetry = config.GetValue<bool?>(nameof(ForwardedHeadersSettings.RequireHeaderSymmetry));
        settings.ForwardedForHeaderName = config[nameof(ForwardedHeadersSettings.ForwardedForHeaderName)];
        settings.ForwardedHostHeaderName = config[nameof(ForwardedHeadersSettings.ForwardedHostHeaderName)];
        settings.ForwardedProtoHeaderName = config[nameof(ForwardedHeadersSettings.ForwardedProtoHeaderName)];
        settings.ForwardedPrefixHeaderName = config[nameof(ForwardedHeadersSettings.ForwardedPrefixHeaderName)];

        return settings;
    }

    /// <summary>構成情報キーから ForwardedHeaders 設定を読み取る</summary>
    /// <param name="config">読み取り元の構成セクション</param>
    /// <param name="key">構成情報キー</param>
    /// <returns>読み取った設定</returns>
    public static ForwardedHeaders? GetForwardedHeaders(this IConfiguration config, string key)
    {
        // キーの値を取得。nullや空ならば結果を null 値とする。
        var value = config[key];
        if (string.IsNullOrWhiteSpace(value)) return null;

        // キーの値をパース。'|' と ',' で区切られた複数フラグをサポートする
        var headers = default(ForwardedHeaders);
        var valueSpan = value.AsSpan();
        var partRanges = valueSpan.SplitAny(['|', ',']);
        foreach (var range in partRanges)
        {
            // 分割パートをパース
            var part = valueSpan[range];
            if (!Enum.TryParse<ForwardedHeaders>(part, ignoreCase: true, out var header))
            {
                // もし空ならば無視する。そうでなければパースエラー。
                if (part.IsWhiteSpace()) continue;
                throw new FormatException($"Invalid ForwardedHeaders value '{part.ToString()}').");
            }

            // フラグ合成
            headers |= header;
        }

        return headers;
    }

    /// <summary>構成情報を元に転送ヘッダオプションを構成する</summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="config">構成情報</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection ConfigureForwardedSettings(this IServiceCollection services, IConfiguration config)
    {
        var settings = config.GetForwardedSettings();
        return services.ConfigureForwardedSettings(settings);
    }

    /// <summary>転送ヘッダ設定を元に転送ヘッダオプションを構成する</summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="settings">転送ヘッダ設定</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection ConfigureForwardedSettings(this IServiceCollection services, ForwardedHeadersSettings settings)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            if (settings.ClearDefaultNetworks == true)
            {
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            }
            if (settings.ForwardedHeaders != null)
            {
                options.ForwardedHeaders = settings.ForwardedHeaders.Value;
            }
            if (settings.ForwardLimit != null)
            {
                options.ForwardLimit = settings.ForwardLimit.Value < 0 ? null : settings.ForwardLimit.Value;
            }
            if (settings.KnownNetworks is { } networks)
            {
                foreach (var network in networks) options.KnownIPNetworks.Add(network);
            }
            if (settings.KnownProxies is { } proxies)
            {
                foreach (var proxy in proxies) options.KnownProxies.Add(proxy);
            }
            if (settings.AllowedHosts is { } hosts)
            {
                foreach (var host in hosts) options.AllowedHosts.Add(host);
            }
            if (settings.RequireHeaderSymmetry != null)
            {
                options.RequireHeaderSymmetry = settings.RequireHeaderSymmetry.Value;
            }
            if (settings.ForwardedForHeaderName is { } fwdRemote && !fwdRemote.IsWhiteSpace()) options.ForwardedForHeaderName = fwdRemote;
            if (settings.ForwardedHostHeaderName is { } fwdHost && !fwdHost.IsWhiteSpace()) options.ForwardedHostHeaderName = fwdHost;
            if (settings.ForwardedProtoHeaderName is { } fwdProto && !fwdProto.IsWhiteSpace()) options.ForwardedProtoHeaderName = fwdProto;
            if (settings.ForwardedPrefixHeaderName is { } fwdPrefix && !fwdPrefix.IsWhiteSpace()) options.ForwardedPrefixHeaderName = fwdPrefix;
        });

        return services;
    }
}
