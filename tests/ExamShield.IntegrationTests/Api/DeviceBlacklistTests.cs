using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;

namespace ExamShield.IntegrationTests.Api;

public sealed class DeviceBlacklistTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() =>
        _client = await factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    private async Task<Guid> RegisterAndApproveAsync()
    {
        using var ecdsa = System.Security.Cryptography.ECDsa.Create();
        var devRes = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest("Blacklist Test Device", ecdsa.ExportSubjectPublicKeyInfo()));
        var device = await devRes.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        var id = device!.DeviceId;
        await _client.PutAsync($"/devices/{id}/approve", null);
        return id;
    }

    [Fact]
    public async Task Blacklist_ApprovedDevice_Returns204()
    {
        var id  = await RegisterAndApproveAsync();
        var res = await _client.PutAsJsonAsync($"/devices/{id}/blacklist",
            new BlacklistDeviceRequest("Unit tests compromised"));

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task Blacklist_DeviceAppearsBlacklistedInList()
    {
        var id = await RegisterAndApproveAsync();
        await _client.PutAsJsonAsync($"/devices/{id}/blacklist",
            new BlacklistDeviceRequest("stolen"));

        var list = await (await _client.GetAsync("/devices"))
            .Content.ReadFromJsonAsync<DeviceListResponse>();
        var device = list!.Devices.FirstOrDefault(d => d.DeviceId == id);

        Assert.Equal("Blacklisted", device?.Status);
        Assert.Equal("stolen", device?.BlacklistReason);
        Assert.False(device?.IsActive);
    }

    [Fact]
    public async Task Blacklist_AlreadyBlacklisted_Returns422()
    {
        var id = await RegisterAndApproveAsync();
        await _client.PutAsJsonAsync($"/devices/{id}/blacklist",
            new BlacklistDeviceRequest("first reason"));

        var res = await _client.PutAsJsonAsync($"/devices/{id}/blacklist",
            new BlacklistDeviceRequest("second reason"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Enable_BlacklistedDevice_Returns422()
    {
        var id = await RegisterAndApproveAsync();
        await _client.PutAsJsonAsync($"/devices/{id}/blacklist",
            new BlacklistDeviceRequest("compromised"));

        var res = await _client.PutAsync($"/devices/{id}/enable", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Blacklist_UnknownDevice_Returns404()
    {
        var res = await _client.PutAsJsonAsync($"/devices/{Guid.NewGuid()}/blacklist",
            new BlacklistDeviceRequest("reason"));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Blacklist_Unauthenticated_Returns401()
    {
        var id  = await RegisterAndApproveAsync();
        var res = await factory.CreateClient().PutAsJsonAsync($"/devices/{id}/blacklist",
            new BlacklistDeviceRequest("reason"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
