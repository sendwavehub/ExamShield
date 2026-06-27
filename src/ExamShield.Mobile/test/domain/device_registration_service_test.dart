import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:examshield_mobile/domain/services/device_registration_service.dart';
import 'package:examshield_mobile/domain/services/crypto_service.dart';
import 'package:examshield_mobile/domain/models/device_keys.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';
import 'package:examshield_mobile/infrastructure/storage/secure_storage.dart';

class MockCryptoService extends Mock implements CryptoService {}
class MockApiClient extends Mock implements ApiClient {}
class MockSecureStorage extends Mock implements SecureStorage {}

void main() {
  late MockCryptoService mockCrypto;
  late MockApiClient mockApi;
  late MockSecureStorage mockStorage;
  late DeviceRegistrationService sut;

  final fakeKeyPair = DeviceKeyPair(
    publicKeyBytes: Uint8List.fromList(List.filled(64, 1)),
    privateKeyBytes: Uint8List.fromList(List.filled(32, 2)),
  );

  setUp(() {
    mockCrypto = MockCryptoService();
    mockApi = MockApiClient();
    mockStorage = MockSecureStorage();
    sut = DeviceRegistrationService(
      crypto: mockCrypto, api: mockApi, storage: mockStorage);
  });

  group('DeviceRegistrationService.isRegistered', () {
    test('returns false when no device id stored', () async {
      when(() => mockStorage.loadDeviceId()).thenAnswer((_) async => null);
      expect(await sut.isRegistered(), isFalse);
    });

    test('returns true when device id is stored', () async {
      when(() => mockStorage.loadDeviceId())
          .thenAnswer((_) async => 'device-uuid-123');
      expect(await sut.isRegistered(), isTrue);
    });
  });

  group('DeviceRegistrationService.register', () {
    setUp(() {
      when(() => mockCrypto.generateKeyPair())
          .thenAnswer((_) async => fakeKeyPair);
      when(() => mockApi.registerDevice(
            name: any(named: 'name'),
            publicKeyBase64: any(named: 'publicKeyBase64'),
            token: any(named: 'token'),
          )).thenAnswer((_) async => 'new-device-id-abc');
      when(() => mockStorage.saveDeviceKeys(
            privateKeyBase64: any(named: 'privateKeyBase64'),
            publicKeyBase64: any(named: 'publicKeyBase64'),
            deviceId: any(named: 'deviceId'),
          )).thenAnswer((_) async {});
    });

    test('generates a new key pair on registration', () async {
      await sut.register(name: 'Test Device', token: 'jwt-abc');
      verify(() => mockCrypto.generateKeyPair()).called(1);
    });

    test('calls api.registerDevice with public key', () async {
      await sut.register(name: 'Test Device', token: 'jwt-abc');
      verify(() => mockApi.registerDevice(
            name: 'Test Device',
            publicKeyBase64: base64Encode(fakeKeyPair.publicKeyBytes),
            token: 'jwt-abc',
          )).called(1);
    });

    test('saves keys and device id to secure storage', () async {
      await sut.register(name: 'Test Device', token: 'jwt-abc');
      verify(() => mockStorage.saveDeviceKeys(
            privateKeyBase64: base64Encode(fakeKeyPair.privateKeyBytes),
            publicKeyBase64: base64Encode(fakeKeyPair.publicKeyBytes),
            deviceId: 'new-device-id-abc',
          )).called(1);
    });

    test('returns the device id from the server', () async {
      final id = await sut.register(name: 'Test Device', token: 'jwt-abc');
      expect(id, equals('new-device-id-abc'));
    });

    test('propagates ApiException when registration fails', () async {
      when(() => mockApi.registerDevice(
            name: any(named: 'name'),
            publicKeyBase64: any(named: 'publicKeyBase64'),
            token: any(named: 'token'),
          )).thenThrow(const ApiException(statusCode: 409, message: 'Already registered'));
      expect(
        () => sut.register(name: 'Test Device', token: 'jwt-abc'),
        throwsA(isA<ApiException>()),
      );
    });
  });
}
