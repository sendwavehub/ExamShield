import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../domain/models/auth_token.dart';

class SecureStorage {
  static const _tokenKey = 'exam_shield_token';
  static const _devicePrivKey = 'device_private_key';
  static const _devicePubKey = 'device_public_key';
  static const _deviceId = 'device_id';

  final FlutterSecureStorage _storage;

  const SecureStorage({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  Future<void> saveToken(AuthToken token) async =>
      _storage.write(key: _tokenKey, value: jsonEncode(token.toJson()));

  Future<AuthToken?> loadToken() async {
    final raw = await _storage.read(key: _tokenKey);
    if (raw == null) return null;
    return AuthToken.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  Future<void> clearToken() async => _storage.delete(key: _tokenKey);

  Future<void> saveDeviceKeys({
    required String privateKeyBase64,
    required String publicKeyBase64,
    required String deviceId,
  }) async {
    await Future.wait([
      _storage.write(key: _devicePrivKey, value: privateKeyBase64),
      _storage.write(key: _devicePubKey, value: publicKeyBase64),
      _storage.write(key: _deviceId, value: deviceId),
    ]);
  }

  Future<String?> loadPrivateKey() => _storage.read(key: _devicePrivKey);
  Future<String?> loadPublicKey() => _storage.read(key: _devicePubKey);
  Future<String?> loadDeviceId() => _storage.read(key: _deviceId);
}
