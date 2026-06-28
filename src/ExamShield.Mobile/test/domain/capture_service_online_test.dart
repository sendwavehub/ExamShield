import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:examshield_mobile/domain/models/auth_token.dart';
import 'package:examshield_mobile/domain/models/capture.dart';
import 'package:examshield_mobile/domain/services/capture_service.dart';
import 'package:examshield_mobile/domain/services/crypto_service.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';
import 'package:examshield_mobile/infrastructure/queue/offline_queue.dart';
import 'package:examshield_mobile/infrastructure/storage/secure_storage.dart';

class MockApiClient extends Mock implements ApiClient {}
class MockSecureStorage extends Mock implements SecureStorage {}
class MockCryptoService extends Mock implements CryptoService {}

class StubOfflineQueue implements OfflineQueue {
  @override Future<void> enqueue(PendingCapture c) async {}
  @override Future<List<PendingCapture>> listPending() async => [];
  @override Future<void> markDone(String id) async {}
  @override Future<void> incrementRetry(String id) async {}
  @override Future<int> pendingCount() async => 0;
}

void main() {
  late MockApiClient api;
  late MockSecureStorage storage;
  late MockCryptoService crypto;
  late CaptureService sut;

  final fakeImage = Uint8List.fromList([0xDE, 0xAD, 0xBE, 0xEF]);
  final fakeHashBytes = Uint8List.fromList(List.filled(32, 0xAA));
  final fakeSignature = Uint8List.fromList(List.filled(64, 0xBB));
  final fakePrivKeyB64 = base64Encode(Uint8List.fromList(List.filled(32, 0x01)));
  const fakeToken = AuthToken(accessToken: 'bearer-tok', role: 'Invigilator');

  setUp(() {
    api = MockApiClient();
    storage = MockSecureStorage();
    crypto = MockCryptoService();
    sut = CaptureService(
        api: api, storage: storage, crypto: crypto, offlineQueue: StubOfflineQueue());

    when(() => storage.loadToken()).thenAnswer((_) async => fakeToken);
    when(() => storage.loadPrivateKey()).thenAnswer((_) async => fakePrivKeyB64);
    when(() => storage.loadDeviceId()).thenAnswer((_) async => 'device-42');
    when(() => crypto.hashBytes(any())).thenReturn(fakeHashBytes);
    when(() => crypto.hashHex(any())).thenReturn('aabbccdd' * 8);
    when(() => crypto.sign(any(), any())).thenAnswer((_) async => fakeSignature);
  });

  group('hashSignAndRegister', () {
    test('returns capture id from API', () async {
      when(() => api.registerCapture(any(), any()))
          .thenAnswer((_) async => 'cap-uuid-123');

      final id = await sut.hashSignAndRegister(fakeImage);

      expect(id, 'cap-uuid-123');
    });

    test('calls crypto.hashHex with image bytes', () async {
      when(() => api.registerCapture(any(), any()))
          .thenAnswer((_) async => 'cap-1');

      await sut.hashSignAndRegister(fakeImage);

      verify(() => crypto.hashHex(fakeImage)).called(1);
    });

    test('calls crypto.sign with hash bytes and decoded private key', () async {
      when(() => api.registerCapture(any(), any()))
          .thenAnswer((_) async => 'cap-1');

      await sut.hashSignAndRegister(fakeImage);

      verify(() => crypto.sign(fakeHashBytes, any())).called(1);
    });

    test('calls registerCapture with bearer token', () async {
      when(() => api.registerCapture(any(), any()))
          .thenAnswer((_) async => 'cap-1');

      await sut.hashSignAndRegister(fakeImage);

      verify(() => api.registerCapture(any(), 'bearer-tok')).called(1);
    });

    test('throws 401 ApiException when no token stored', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => null);

      expect(
        () => sut.hashSignAndRegister(fakeImage),
        throwsA(isA<ApiException>().having((e) => e.statusCode, 'statusCode', 401)),
      );
    });

    test('throws when no private key stored', () async {
      when(() => storage.loadPrivateKey()).thenAnswer((_) async => null);

      expect(
        () => sut.hashSignAndRegister(fakeImage),
        throwsA(isA<Exception>()),
      );
    });
  });

  group('myCaptures', () {
    test('returns list from API when authenticated', () async {
      final records = [
        CaptureRecord(
          captureId: 'cap-1',
          examId: 'exam-1',
          studentId: 'stu-1',
          status: CaptureStatus.verified,
          capturedAt: DateTime.now(),
          hashHex: 'ff',
        ),
      ];
      when(() => api.getMyCaptures(any())).thenAnswer((_) async => records);

      final result = await sut.myCaptures();

      expect(result.length, 1);
      expect(result.first.captureId, 'cap-1');
    });

    test('returns empty list when no token stored', () async {
      when(() => storage.loadToken()).thenAnswer((_) async => null);

      final result = await sut.myCaptures();

      expect(result, isEmpty);
    });

    test('calls API with correct token', () async {
      when(() => api.getMyCaptures(any())).thenAnswer((_) async => []);

      await sut.myCaptures();

      verify(() => api.getMyCaptures('bearer-tok')).called(1);
    });
  });
}
