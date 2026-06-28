import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:examshield_mobile/domain/models/auth_token.dart';
import 'package:examshield_mobile/domain/services/sync_service.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';
import 'package:examshield_mobile/infrastructure/queue/offline_queue.dart';
import 'package:examshield_mobile/infrastructure/storage/secure_storage.dart';

class MockApiClient extends Mock implements ApiClient {}
class MockSecureStorage extends Mock implements SecureStorage {}

class InMemorySyncQueue implements OfflineQueue {
  final List<PendingCapture> _items = [];

  @override
  Future<void> enqueue(PendingCapture capture) async => _items.add(capture);

  @override
  Future<List<PendingCapture>> listPending() async => List.of(_items);

  @override
  Future<void> markDone(String captureId) async =>
      _items.removeWhere((c) => c.captureId == captureId);

  @override
  Future<void> incrementRetry(String captureId) async {
    final i = _items.indexWhere((c) => c.captureId == captureId);
    if (i >= 0) {
      _items[i] = _items[i].copyWith(retryCount: _items[i].retryCount + 1);
    }
  }

  @override
  Future<int> pendingCount() async => _items.length;
}

void main() {
  late MockApiClient api;
  late MockSecureStorage storage;
  late InMemorySyncQueue queue;
  late SyncService sut;

  final fakeImage = Uint8List.fromList([1, 2, 3]);
  const fakeToken = AuthToken(accessToken: 'tok', role: 'Invigilator');

  PendingCapture pendingCapture(String id) => PendingCapture(
        captureId: id,
        imageBytes: fakeImage,
        createdAt: DateTime.now(),
        retryCount: 0,
      );

  setUp(() {
    api = MockApiClient();
    storage = MockSecureStorage();
    queue = InMemorySyncQueue();
    sut = SyncService(queue: queue, api: api, storage: storage);
  });

  group('SyncService.syncPending', () {
    test('returns zero counts when queue is empty', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => fakeToken);

      final result = await sut.syncPending();

      expect(result.uploaded, 0);
      expect(result.failed, 0);
    });

    test('returns zero counts when no token is stored', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => null);
      await queue.enqueue(pendingCapture('cap-1'));

      final result = await sut.syncPending();

      expect(result.uploaded, 0);
      expect(result.failed, 0);
    });

    test('uploads all pending captures and drains queue', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => fakeToken);
      when(() => api.uploadImage(any(), any(), any()))
          .thenAnswer((_) async => {});
      await queue.enqueue(pendingCapture('cap-1'));
      await queue.enqueue(pendingCapture('cap-2'));

      final result = await sut.syncPending();

      expect(result.uploaded, 2);
      expect(result.failed, 0);
      expect(await queue.pendingCount(), 0);
    });

    test('counts failed uploads and increments retry', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => fakeToken);
      when(() => api.uploadImage(any(), any(), any()))
          .thenThrow(const ApiException(statusCode: 500, message: 'Server error'));
      await queue.enqueue(pendingCapture('cap-1'));

      final result = await sut.syncPending();

      expect(result.uploaded, 0);
      expect(result.failed, 1);
      final pending = await queue.listPending();
      expect(pending.first.retryCount, 1);
    });

    test('partial success — some upload, some fail', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => fakeToken);
      when(() => api.uploadImage('cap-ok', any(), any()))
          .thenAnswer((_) async => {});
      when(() => api.uploadImage('cap-fail', any(), any()))
          .thenThrow(const ApiException(statusCode: 503, message: 'Unavailable'));

      await queue.enqueue(pendingCapture('cap-ok'));
      await queue.enqueue(pendingCapture('cap-fail'));

      final result = await sut.syncPending();

      expect(result.uploaded, 1);
      expect(result.failed, 1);
      expect(await queue.pendingCount(), 1);
    });

    test('calls uploadImage with correct token', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => fakeToken);
      when(() => api.uploadImage(any(), any(), any()))
          .thenAnswer((_) async => {});
      await queue.enqueue(pendingCapture('cap-1'));

      await sut.syncPending();

      verify(() => api.uploadImage('cap-1', fakeImage, 'tok')).called(1);
    });
  });
}
