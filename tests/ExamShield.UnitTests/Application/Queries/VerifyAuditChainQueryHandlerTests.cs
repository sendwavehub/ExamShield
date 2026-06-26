using ExamShield.Application.Queries.VerifyAuditChain;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class VerifyAuditChainQueryHandlerTests
{
    private readonly IAuditLogRepository _repository = Substitute.For<IAuditLogRepository>();
    private readonly VerifyAuditChainQueryHandler _sut;

    public VerifyAuditChainQueryHandlerTests() =>
        _sut = new VerifyAuditChainQueryHandler(_repository);

    private static IReadOnlyList<AuditLog> BuildValidChain(CaptureId captureId, int count)
    {
        var entries = new List<AuditLog>();
        var previous = string.Empty;
        for (var i = 0; i < count; i++)
        {
            var entry = AuditLog.Record(AuditAction.ImageUploaded, captureId);
            var hash = AuditChainHasher.ComputeContentHash(entry, previous);
            entry.SetChainHashes(previous, hash);
            entries.Add(entry);
            previous = hash;
        }
        return entries;
    }

    [Fact]
    public async Task Handle_WhenChainIsValid_ReturnsIsValidTrue()
    {
        var captureId = new CaptureId(Guid.NewGuid());
        var chain = BuildValidChain(captureId, 3);
        _repository.GetChainAsync(captureId, Arg.Any<CancellationToken>()).Returns(chain);

        var result = await _sut.Handle(new VerifyAuditChainQuery(captureId.Value), default);

        result.IsValid.Should().BeTrue();
        result.EntryCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenEmptyChain_ReturnsValidWithZeroEntries()
    {
        var captureId = new CaptureId(Guid.NewGuid());
        _repository.GetChainAsync(captureId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AuditLog>());

        var result = await _sut.Handle(new VerifyAuditChainQuery(captureId.Value), default);

        result.IsValid.Should().BeTrue();
        result.EntryCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenContentHashTampered_ReturnsIsValidFalse()
    {
        var captureId = new CaptureId(Guid.NewGuid());
        var chain = BuildValidChain(captureId, 2).ToList();
        // Tamper: set a wrong ContentHash on second entry
        chain[1].SetChainHashes(chain[1].PreviousHash, "tampered_hash_value");
        _repository.GetChainAsync(captureId, Arg.Any<CancellationToken>()).Returns(chain);

        var result = await _sut.Handle(new VerifyAuditChainQuery(captureId.Value), default);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenPreviousHashBroken_ReturnsIsValidFalse()
    {
        var captureId = new CaptureId(Guid.NewGuid());
        var chain = BuildValidChain(captureId, 2).ToList();
        // Tamper: break the PreviousHash link of the second entry
        chain[1].SetChainHashes("wrong_previous_hash", chain[1].ContentHash);
        _repository.GetChainAsync(captureId, Arg.Any<CancellationToken>()).Returns(chain);

        var result = await _sut.Handle(new VerifyAuditChainQuery(captureId.Value), default);

        result.IsValid.Should().BeFalse();
    }
}
