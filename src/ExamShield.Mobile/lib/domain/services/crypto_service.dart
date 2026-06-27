import 'dart:convert';
import 'dart:typed_data';
import 'package:crypto/crypto.dart' as pkg_crypto;
import 'package:cryptography/cryptography.dart' as pkg_ecdsa;
import '../models/device_keys.dart';

class CryptoService {
  static final _algo = pkg_ecdsa.Ecdsa.p256(pkg_ecdsa.Sha256());

  Uint8List hashBytes(Uint8List data) {
    final digest = pkg_crypto.sha256.convert(data);
    return Uint8List.fromList(digest.bytes);
  }

  String hashHex(Uint8List data) {
    final digest = pkg_crypto.sha256.convert(data);
    return digest.toString();
  }

  Future<DeviceKeyPair> generateKeyPair() async {
    final pair = await _algo.newKeyPair();
    final pub = await pair.extractPublicKey();
    final priv = await pair.extractPrivateKeyBytes();
    return DeviceKeyPair(
      publicKeyBytes: Uint8List.fromList([...pub.x, ...pub.y]),
      privateKeyBytes: Uint8List.fromList(priv),
    );
  }

  Future<Uint8List> sign(Uint8List data, Uint8List privateKeyBytes) async {
    final privKey = pkg_ecdsa.EcKeyPairData(
      d: privateKeyBytes,
      x: const [],
      y: const [],
      type: pkg_ecdsa.KeyPairType.p256,
    );
    final sig = await _algo.sign(data, keyPair: privKey);
    return Uint8List.fromList(sig.bytes);
  }

  Future<bool> verify(
      Uint8List data, Uint8List signature, Uint8List publicKeyBytes) async {
    try {
      final half = publicKeyBytes.length ~/ 2;
      final pubKey = pkg_ecdsa.EcPublicKey(
        x: publicKeyBytes.sublist(0, half),
        y: publicKeyBytes.sublist(half),
        type: pkg_ecdsa.KeyPairType.p256,
      );
      final sig = pkg_ecdsa.EcdsaSignature(signature, publicKey: pubKey);
      return await _algo.verify(data, signature: sig);
    } catch (_) {
      return false;
    }
  }
}
