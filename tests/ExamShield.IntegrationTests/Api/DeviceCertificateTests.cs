using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.IssueCertificate;
using ExamShield.Application.Queries.GetDeviceCertificates;
using FluentAssertions;

namespace ExamShield.IntegrationTests.Api;

public sealed class DeviceCertificateTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private Guid _deviceId;

    public DeviceCertificateTests(TestWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
        var res = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest("CertTest-Scanner", NewPublicKeyBytes()));
        var body = await res.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        _deviceId = body!.DeviceId;
    }

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

    private static string NewPemKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return ecdsa.ExportSubjectPublicKeyInfoPem();
    }

    [Fact]
    public async Task PostCertificate_WithValidRequest_Returns201()
    {
        var res = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates",
            new IssueCertificateRequest(NewPemKey(), 365));

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostCertificate_Returns_CertificateId()
    {
        var res = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates",
            new IssueCertificateRequest(NewPemKey(), 90));
        var body = await res.Content.ReadFromJsonAsync<IssueCertificateResult>();

        body!.CertificateId.Should().NotBeEmpty();
        body.DeviceId.Should().Be(_deviceId);
    }

    [Fact]
    public async Task PostCertificate_UnknownDevice_Returns404()
    {
        var res = await _client.PostAsJsonAsync(
            $"/devices/{Guid.NewGuid()}/certificates",
            new IssueCertificateRequest(NewPemKey(), 365));

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCertificates_AfterIssue_ReturnsIssuedCert()
    {
        var pem = NewPemKey();
        await _client.PostAsJsonAsync($"/devices/{_deviceId}/certificates",
            new IssueCertificateRequest(pem, 180));

        var res = await _client.GetAsync($"/devices/{_deviceId}/certificates");
        var certs = await res.Content.ReadFromJsonAsync<List<DeviceCertificateDto>>();

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        certs.Should().NotBeEmpty();
        certs!.Should().Contain(c => !c.IsRevoked);
    }

    [Fact]
    public async Task RevokeCertificate_ValidCert_Returns204()
    {
        var issueRes = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates",
            new IssueCertificateRequest(NewPemKey(), 365));
        var cert = await issueRes.Content.ReadFromJsonAsync<IssueCertificateResult>();

        var revokeRes = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates/{cert!.CertificateId}/revoke",
            new RevokeCertificateRequest("Key rotation policy"));

        revokeRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokeCertificate_ThenList_ShowsRevoked()
    {
        var issueRes = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates",
            new IssueCertificateRequest(NewPemKey(), 365));
        var cert = await issueRes.Content.ReadFromJsonAsync<IssueCertificateResult>();

        await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates/{cert!.CertificateId}/revoke",
            new RevokeCertificateRequest("Security incident"));

        var listRes = await _client.GetAsync($"/devices/{_deviceId}/certificates");
        var certs = await listRes.Content.ReadFromJsonAsync<List<DeviceCertificateDto>>();

        certs!.First(c => c.Id == cert.CertificateId).IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeCertificate_AlreadyRevoked_Returns422()
    {
        var issueRes = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates",
            new IssueCertificateRequest(NewPemKey(), 365));
        var cert = await issueRes.Content.ReadFromJsonAsync<IssueCertificateResult>();

        await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates/{cert!.CertificateId}/revoke",
            new RevokeCertificateRequest("First reason"));

        var secondRevoke = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates/{cert.CertificateId}/revoke",
            new RevokeCertificateRequest("Second reason"));

        secondRevoke.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RevokeCertificate_UnknownCert_Returns404()
    {
        var res = await _client.PostAsJsonAsync(
            $"/devices/{_deviceId}/certificates/{Guid.NewGuid()}/revoke",
            new RevokeCertificateRequest("reason"));

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
