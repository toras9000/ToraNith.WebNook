using System.Security.Claims;
using System.Text.Encodings.Web;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ToraNith.WebNook.Tests;

[TestClass]
public class SimpleAuthHandlerTests
{
    [TestMethod]
    public async Task HandleAuthenticateAsync_Success()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer valid-token";

        var options = new SimpleAuthSchemeOptions();
        options.UseValidator((header) => new SimpleAuthSuccess("TestName", "TestRole"));

        var scheme = new AuthenticationScheme(name: "TestScheme", displayName: null, handlerType: typeof(SimpleAuthHandler));
        var handler = new SimpleAuthHandler(TestOptions.Create(options), NullLoggerFactory.Instance, UrlEncoder.Default);
        await handler.InitializeAsync(scheme, context);
        var result = await handler.AuthenticateAsync();

        result.Should().NotBeNull();
        result.Ticket.Should().NotBeNull();
        var claims = result.Ticket.Principal.Identity.Should().NotBeNull().And.BeOfType<ClaimsIdentity>().Subject;
        var claimName = claims.FindFirst(ClaimTypes.Name);
        var claimRole = claims.FindFirst(ClaimTypes.Role);
        claimName.Should().NotBeNull();
        claimName.Value.Should().Be("TestName");
        claimRole.Should().NotBeNull();
        claimRole.Value.Should().Be("TestRole");
    }

    [TestMethod]
    public async Task HandleAuthenticateAsync_NoHeader()
    {
        var context = new DefaultHttpContext();

        var options = new SimpleAuthSchemeOptions();
        options.UseValidator((header) => new SimpleAuthSuccess("TestName", "TestRole"));

        var scheme = new AuthenticationScheme(name: "TestScheme", displayName: null, handlerType: typeof(SimpleAuthHandler));
        var handler = new SimpleAuthHandler(TestOptions.Create(options), NullLoggerFactory.Instance, UrlEncoder.Default);
        await handler.InitializeAsync(scheme, context);
        var result = await handler.AuthenticateAsync();

        result.Should().NotBeNull();
        result.Ticket.Should().BeNull();
    }

    [TestMethod]
    public async Task HandleAuthenticateAsync_NoDelegate()
    {
        var context = new DefaultHttpContext();

        var options = new SimpleAuthSchemeOptions();

        var scheme = new AuthenticationScheme(name: "TestScheme", displayName: null, handlerType: typeof(SimpleAuthHandler));
        var handler = new SimpleAuthHandler(TestOptions.Create(options), NullLoggerFactory.Instance, UrlEncoder.Default);
        await handler.InitializeAsync(scheme, context);
        var result = await handler.AuthenticateAsync();

        result.Should().NotBeNull();
        result.Ticket.Should().BeNull();
    }

}
