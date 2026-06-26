using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using ExamShield.Api.Contracts;
using FluentAssertions;

namespace ExamShield.IntegrationTests.Api;

public sealed class DeviceEndpointsTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public DeviceEndpointsTests(TestWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() =>
        _client = await _factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private static byte[] NewPublicKeyBytes()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return ecdsa.ExportSubjectPublicKeyInfo();
    }

    [Fact]
    public async Task PostDevices_WithValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest("Scanner-01", NewPublicKeyBytes()));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostDevices_WithValidRequest_ReturnsNonEmptyDeviceId()
    {
        var response = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest("Scanner-01", NewPublicKeyBytes()));
        var body = await response.Content.ReadFromJsonAsync<RegisterDeviceResponse>();

        body!.DeviceId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task PostDevices_WithEmptyPublicKey_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest("Scanner-01", Array.Empty<byte>()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostDevices_WithEmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest("", NewPublicKeyBytes()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
