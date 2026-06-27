using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class OcrQueueEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public OcrQueueEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetOcrQueue_Returns200WithItemsCollection()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/ocr/queue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OcrQueueResponse>();
        body!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOcrQueue_WithUploadedCapture_ReturnsItInQueue()
    {
        // Seed an Uploaded capture
        var capture = Capture.Create(
            new ExamId(Guid.NewGuid()), new StudentId(Guid.NewGuid()),
            new DeviceId(Guid.NewGuid()), new PageNumber(1),
            Hash.FromBytes(new byte[32]), new Signature(new byte[64]));
        capture.RecordUpload("storage/test-key");

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICaptureRepository>();
        await repo.AddAsync(capture);

        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/ocr/queue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OcrQueueResponse>();
        body!.Items.Should().Contain(i => i.CaptureId == capture.Id.Value);
    }

    [Fact]
    public async Task GetOcrQueue_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ocr/queue");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
