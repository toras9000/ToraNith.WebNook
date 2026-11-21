using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ToraNith.WebNook;

/// <summary>シンプル認証成功結果</summary>
/// <param name="Name">認証ユーザ名</param>
/// <param name="Role">認証ユーザロール</param>
public record SimpleAuthSuccess(string Name, string Role);

/// <summary>シンプル認証失敗結果</summary>
/// <param name="Message"></param>
public record SimpleAuthFailed(string Message);

/// <summary>シンプル認証結果</summary>
public union SimpleAuthResult(SimpleAuthSuccess, SimpleAuthFailed);

/// <summary>シンプル認証リゾルバ</summary>
/// <param name="scheme">スキーマ名</param>
/// <param name="header">認証ヘッダ</param>
/// <returns>認証結果。null の場合は対象外。</returns>
public delegate AuthenticateResult? SimpleAuthResolver(string scheme, AuthenticationHeaderValue header);

/// <summary>シンプル認証バリデータ</summary>
/// <param name="header">認証ヘッダ</param>
/// <returns>シンプル認証結果。null の場合は対象外。</returns>
public delegate SimpleAuthResult? SimpleAuthValidator(AuthenticationHeaderValue header);

/// <summary>シンプル認証オプション</summary>
public class SimpleAuthSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>認証リゾルバ</summary>
    public SimpleAuthResolver? AuthResolver { get; set; }

    /// <summary>シンプル認証バリデータを使って認証リゾルバ(<see cref="AuthResolver"/>)を設定する</summary>
    /// <param name="validator">検証デリゲート</param>
    public void UseValidator(SimpleAuthValidator validator)
    {
        // バリデータを使って認証結果を作るハンドラにラップ
        this.AuthResolver = (scheme, header) =>
        {
            // バリデータで結果を取得
            var result = validator(header);
            if (result is SimpleAuthSuccess success)
            {
                // 認証結果から認証情報作成
                var claims = new[] {
                        new Claim(ClaimTypes.Name, success.Name),
                        new Claim(ClaimTypes.Role, success.Role),
                    };
                var identity = new ClaimsIdentity(claims, scheme);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, scheme);

                // 認証成功を返却
                return AuthenticateResult.Success(ticket);
            }
            else if (result is SimpleAuthFailed failed)
            {
                // 有効な認証結果が無ければ失敗
                return AuthenticateResult.Fail(failed.Message);
            }

            return default;
        };
    }
}

/// <summary>シンプル認証ハンドラ</summary>
/// <param name="options">シンプル認証オプション</param>
/// <param name="logger">ロガー</param>
/// <param name="encoder">URLエンコーダ</param>
public class SimpleAuthHandler(IOptionsMonitor<SimpleAuthSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<SimpleAuthSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Authorization ヘッダをパース
        var authHeader = this.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authHeader, out var authValue))
        {
            // 有効なヘッダがなければ対象外
            return AuthenticateResult.NoResult();
        }

        // 認証ハンドラで検証
        var result = this.Options.AuthResolver?.Invoke(this.Scheme.Name, authValue);
        if (result == null)
        {
            // 結果が無ければ対象外
            return AuthenticateResult.NoResult();
        }

        return result;
    }
}
