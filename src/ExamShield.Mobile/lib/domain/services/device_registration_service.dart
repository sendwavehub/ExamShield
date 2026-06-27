import 'dart:convert';
import '../../infrastructure/api/api_client.dart';
import '../../infrastructure/storage/secure_storage.dart';
import 'crypto_service.dart';

class DeviceRegistrationService {
  final CryptoService crypto;
  final ApiClient api;
  final SecureStorage storage;

  const DeviceRegistrationService({
    required this.crypto,
    required this.api,
    required this.storage,
  });

  Future<bool> isRegistered() async =>
      (await storage.loadDeviceId()) != null;

  Future<String> register({required String name, required String token}) async {
    final pair = await crypto.generateKeyPair();
    final deviceId = await api.registerDevice(
      name: name,
      publicKeyBase64: base64Encode(pair.publicKeyBytes),
      token: token,
    );
    await storage.saveDeviceKeys(
      privateKeyBase64: base64Encode(pair.privateKeyBytes),
      publicKeyBase64: base64Encode(pair.publicKeyBytes),
      deviceId: deviceId,
    );
    return deviceId;
  }
}
