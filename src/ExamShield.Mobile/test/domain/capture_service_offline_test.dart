import 'dart:typed_data';
import 'package:mocktail/mocktail.dart';
import 'package:test/test.dart';
import 'package:examshield/domain/models/auth_token.dart';
import 'package:examshield/domain/models/capture.dart';
import 'package:examshield/domain/services/capture_service.dart';
import 'package:examshield/domain/services/crypto_service.dart';
import 'package:examshield/infrastructure/api/api_client.dart';
import 'package:examshield/infrastructure/queue/offline_queue.dart';
import 'package:examshield/infrastructure/storage/secure_storage.dart';

// Fakes
class MockApiClient extends Mock implements ApiClient {}
class MockSecureStorage extends Mock implements SecureStorage {}
class MockCryptoService extends Mock implements CryptoService {}

class InMemoryOfflineQueue implements OfflineQueue {
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
  late MockCryptoService crypto;
  late InMemoryOfflineQueue queue;
  late CaptureService sut;

  final fakeImage = Uint8List.fromList([1, 2, 3, 4]);
  final fakeHash = Uint8List.fromList(List.filled(32, 0));
  final fakeSignature = Uint8List.fromList(List.filled(64, 0));
  final fakeToken = const AuthToken(accessToken: 'tok', role: 'Operator');

  setUp(() {
    api = MockApiClient();
    storage = MockSecureStorage();
    crypto = MockCryptoService();
    queue = InMemoryOfflineQueue();
    sut = CaptureService(
        api: api, storage: storage, crypto: crypto, offlineQueue: queue);

    when(() => storage.loadToken()).thenAnswer((_) async => fakeToken);
    when(() => storage.loadPrivateKey()).thenAnswer((_) async => 'cHJpdktleQ==');
    when(() => storage.loadDeviceId()).thenAnswer((_) async => 'device-1');
    when(() => crypto.hashBytes(any())).thenReturn(fakeHash);
    when(() => crypto.hashHex(any())).thenReturn('aabbccdd' * 8);
    when(() => crypto.sign(any(), any())).thenAnswer((_) async => fakeSignature);
  });

  group('upload — online', () {
    test('upload succeeds when API is reachable', () async {
      when(() => api.uploadImage(any(), any(), any()))
          .thenAnswer((_) async => {});

      await sut.upload('cap-1', fakeImage);

      verify(() => api.uploadImage('cap-1', fakeImage, 'tok')).called(1);
    });

    test('upload does not enqueue when successful', () async {
      when(() => api.uploadImage(any(), any(), any()))
          .thenAnswer((_) async => {});

      await sut.upload('cap-1', fakeImage);

      expect(await queue.pendingCount(), 0);
    });
  });

  group('upload — offline', () {
    test('upload enqueues capture when API throws SocketException-like error', () async {
      when(() => api.uploadImage(any(), any(), any()))
          .thenThrow(const ApiException(statusCode: 0, message: 'No network'));

      await sut.uploadWithFallback('cap-1', fakeImage);

      expect(await queue.pendingCount(), 1);
    });

    test('queued item has correct captureId', () async {
      when(() => api.uploadImage(any(), any(), any()))
          .thenThrow(const ApiException(statusCode: 0, message: 'No network'));

      await sut.uploadWithFallback('cap-99', fakeImage);

      final pending = await queue.listPending();
      expect(pending.first.captureId, 'cap-99');
    });

    test('queued item has correct imageBytes', () async {
      when(() => api.uploadImage(any(), any(), any()))
          .thenThrow(const ApiException(statusCode: 0, message: 'No network'));

      await sut.uploadWithFallback('cap-1', fakeImage);

      final pending = await queue.listPending();
      expect(pending.first.imageBytes, fakeImage);
    });
  });

  group('syncPendingUploads', () {
    test('drains queue when all uploads succeed', () async {
      queue = InMemoryOfflineQueue();
      await queue.enqueue(PendingCapture(
          captureId: 'cap-1',
          imageBytes: fakeImage,
          createdAt: DateTime.now(),
          retryCount: 0));
      sut = CaptureService(
          api: api, storage: storage, crypto: crypto, offlineQueue: queue);

      when(() => api.uploadImage(any(), any(), any()))
          .thenAnswer((_) async => {});

      final result = await sut.syncPendingUploads();

      expect(result.uploaded, 1);
      expect(result.failed, 0);
      expect(await queue.pendingCount(), 0);
    });

    test('increments retry count on failure', () async {
      queue = InMemoryOfflineQueue();
      await queue.enqueue(PendingCapture(
          captureId: 'cap-1',
          imageBytes: fakeImage,
          createdAt: DateTime.now(),
          retryCount: 0));
      sut = CaptureService(
          api: api, storage: storage, crypto: crypto, offlineQueue: queue);

      when(() => api.uploadImage(any(), any(), any()))
          .thenThrow(const ApiException(statusCode: 500, message: 'Server error'));

      final result = await sut.syncPendingUploads();

      expect(result.failed, 1);
      final pending = await queue.listPending();
      expect(pending.first.retryCount, 1);
    });

    test('returns zero counts when queue is empty', () async {
      final result = await sut.syncPendingUploads();

      expect(result.uploaded, 0);
      expect(result.failed, 0);
    });
  });
}
