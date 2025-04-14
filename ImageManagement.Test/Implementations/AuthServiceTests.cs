using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace ImageManagement.Infrastructure.Implementations.Tests;

[TestClass()]
public class AuthServiceTests
{

    private IConfiguration _config;
    private const string TestUsername = "testuser";
    private const string TestSecret = "this-is-a-very-long-secret-key-for-testing-1234567890";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";
    private const int TestExpiration = 30;
    private AuthService _authService;

    public AuthServiceTests()
    {
        _config = new ConfigurationBuilder()
           .AddInMemoryCollection(new Dictionary<string, string>
           {
               ["JWT:Secret"] = TestSecret,
               ["JWT:Issuer"] = TestIssuer,
               ["JWT:Audience"] = TestAudience,
               ["JWT:ExpirationTime"] = TestExpiration.ToString()
           })
           .Build();
        _authService = new AuthService();
    }
    [TestMethod]
    public async Task GenerateJwtToken_ValidInput_ReturnsValidToken()
    {
        // Act
        var token = await _authService.GenerateJwtToken(TestUsername, _config);

        // Assert
        Assert.IsNotNull(token);
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.AreEqual(TestIssuer, jwtToken.Issuer);
        Assert.AreEqual(TestAudience, jwtToken.Audiences.First());
        Assert.AreEqual(TestUsername, jwtToken.Claims.First(c => c.Type == "UserName").Value);
    }
    [TestMethod]
    public async Task GenerateJwtToken_MissingJwtSection_ThrowsException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<Exception>(
            () => _authService.GenerateJwtToken(TestUsername, emptyConfig));

        Assert.AreEqual("Error generating JWT token", ex.Message);
        Assert.IsInstanceOfType(ex.InnerException, typeof(NullReferenceException));
    }

    [TestMethod]
    public async Task GenerateJwtToken_MissingSecret_ThrowsException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JWT:Issuer"] = TestIssuer,
                ["JWT:Audience"] = TestAudience,
                ["JWT:ExpirationTime"] = TestExpiration.ToString()
            })
            .Build();

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<Exception>(
            () => _authService.GenerateJwtToken(TestUsername, invalidConfig));

        Assert.AreEqual("Error generating JWT token", ex.Message);
        Assert.IsInstanceOfType(ex.InnerException, typeof(System.ArgumentNullException));
    }

    [TestMethod]
    public async Task GenerateJwtToken_InvalidExpiration_ThrowsException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JWT:Secret"] = TestSecret,
                ["JWT:Issuer"] = TestIssuer,
                ["JWT:Audience"] = TestAudience,
                ["JWT:ExpirationTime"] = "not-an-integer"
            })
            .Build();

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<Exception>(
            () => _authService.GenerateJwtToken(TestUsername, invalidConfig));

        Assert.AreEqual("Error generating JWT token", ex.Message);
    }

}
