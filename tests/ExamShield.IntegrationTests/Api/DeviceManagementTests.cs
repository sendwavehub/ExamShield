using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class DeviceManagementTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() =>
        _client = await factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    private static byte[] NewPubKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return ecdsa.ExportSubjectPublicKeyInfo();
    }

    private async Task<Guid> RegisterDevice(string name = "Test Device")
    {
        var resp = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest(name, NewPubKey()));
        var body = await resp.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        return body!.DeviceId;
    }

    [Fact]
    public async Task GetDevices_ReturnsOk()
    {
        var response = await _client.GetAsync("/devices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDevices_AfterRegister_ContainsDevice()
    {
        var deviceId = await RegisterDevice("ListTest Device");

        var response = await _client.GetAsync("/devices");
        var body = await response.Content.ReadFromJsonAsync<DeviceListResponse>();

        body!.Devices.Should().Contain(d => d.DeviceId == deviceId);
    }

    [Fact]
    public async Task PutDisable_WithValidId_Returns204()
    {
        var deviceId = await RegisterDevice("Disable Device");

        var response = await _client.PutAsync($"/devices/{deviceId}/disable", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PutDisable_DeviceIsInactiveAfterward()
    {
        var deviceId = await RegisterDevice("Disable Check");
        await _client.PutAsync($"/devices/{deviceId}/disable", null);

        var listResp = await _client.GetAsync("/devices");
        var body = await listResp.Content.ReadFromJsonAsync<DeviceListResponse>();
        body!.Devices.Single(d => d.DeviceId == deviceId).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task PutEnable_AfterDisable_Returns204()
    {
        var deviceId = await RegisterDevice("Enable Device");
        await _client.PutAsync($"/devices/{deviceId}/disable", null);

        var response = await _client.PutAsync($"/devices/{deviceId}/enable", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PutDisable_WithUnknownId_Returns404()
    {
        var response = await _client.PutAsync($"/devices/{Guid.NewGuid()}/disable", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDevices_Unauthenticated_Returns401()
    {
        var anon = factory.CreateClient();
        var response = await anon.GetAsync("/devices");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
