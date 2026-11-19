using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ToraNith.WebNook;

/// <summary>認証結果</summary>
/// <param name="Name">認証ユーザ名</param>
/// <param name="Role">認証ユーザロール</param>
public record SimpleAuthResult(string Name, string Role);

/// <summary>シンプル認証オプション</summary>
public class SimpleAuthSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>認証ハンドラ</summary>
    public Func<AuthenticationHeaderValue, SimpleAuthResult?>? SimpleAuth { get; set; }
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
        var auth = this.Options.SimpleAuth?.Invoke(authValue);
        if (auth == null)
        {
            // 有効な認証結果が無ければ失敗
            return AuthenticateResult.Fail("Invalid authentication");
        }

        // 認証結果から認証情報作成
        var claims = new[] {
            new Claim(ClaimTypes.Name, auth.Name),
            new Claim(ClaimTypes.Role, auth.Role),
        };
        var identity = new ClaimsIdentity(claims, this.Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

        // 認証成功を返却
        return AuthenticateResult.Success(ticket);
    }
}
