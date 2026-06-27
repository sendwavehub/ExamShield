import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:examshield_mobile/infrastructure/queue/offline_queue.dart';

/// In-memory implementation used only in tests.
class InMemoryOfflineQueue implements OfflineQueue {
  final List<PendingCapture> _store = [];

  @override
  Future<void> enqueue(PendingCapture capture) async => _store.add(capture);

  @override
  Future<List<PendingCapture>> listPending() async =>
      List.unmodifiable(_store.where((c) => c.retryCount < 5).toList());

  @override
  Future<void> markDone(String captureId) async =>
      _store.removeWhere((c) => c.captureId == captureId);

  @override
  Future<void> incrementRetry(String captureId) async {
    final idx = _store.indexWhere((c) => c.captureId == captureId);
    if (idx >= 0) {
      final c = _store[idx];
      _store[idx] = PendingCapture(
        captureId: c.captureId,
        imageBytes: c.imageBytes,
        createdAt: c.createdAt,
        retryCount: c.retryCount + 1,
      );
    }
  }

  @override
  Future<int> pendingCount() async => _store.length;
}

void main() {
  late InMemoryOfflineQueue queue;

  setUp(() => queue = InMemoryOfflineQueue());

  final sampleCapture = PendingCapture(
    captureId: 'cap-1',
    imageBytes: Uint8List.fromList([1, 2, 3]),
    createdAt: DateTime(2026, 6, 27),
    retryCount: 0,
  );

  group('OfflineQueue.enqueue', () {
    test('adds capture to queue', () async {
      await queue.enqueue(sampleCapture);
      expect(await queue.pendingCount(), equals(1));
    });

    test('allows multiple captures', () async {
      await queue.enqueue(sampleCapture);
      await queue.enqueue(sampleCapture.copyWith(captureId: 'cap-2'));
      expect(await queue.pendingCount(), equals(2));
    });
  });

  group('OfflineQueue.listPending', () {
    test('returns enqueued capture', () async {
      await queue.enqueue(sampleCapture);
      final pending = await queue.listPending();
      expect(pending, hasLength(1));
      expect(pending.first.captureId, equals('cap-1'));
    });

    test('returns empty list when queue is empty', () async {
      expect(await queue.listPending(), isEmpty);
    });
  });

  group('OfflineQueue.markDone', () {
    test('removes capture after successful upload', () async {
      await queue.enqueue(sampleCapture);
      await queue.markDone('cap-1');
      expect(await queue.pendingCount(), equals(0));
    });

    test('does not affect other captures', () async {
      await queue.enqueue(sampleCapture);
      await queue.enqueue(sampleCapture.copyWith(captureId: 'cap-2'));
      await queue.markDone('cap-1');
      expect(await queue.pendingCount(), equals(1));
    });
  });

  group('OfflineQueue.incrementRetry', () {
    test('increments retry count', () async {
      await queue.enqueue(sampleCapture);
      await queue.incrementRetry('cap-1');
      final pending = await queue.listPending();
      expect(pending.first.retryCount, equals(1));
    });

    test('capture with retryCount >= 5 excluded from listPending', () async {
      var cap = sampleCapture;
      await queue.enqueue(cap);
      for (var i = 0; i < 5; i++) {
        await queue.incrementRetry('cap-1');
      }
      expect(await queue.listPending(), isEmpty);
    });
  });
}
