import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:examshield_mobile/domain/models/device_keys.dart';

void main() {
  group('DeviceKeyPair', () {
    test('stores public key bytes', () {
      final pub = Uint8List.fromList([0x04, 0x01, 0x02]);
      final priv = Uint8List.fromList([0xFF, 0xFE]);

      final pair = DeviceKeyPair(publicKeyBytes: pub, privateKeyBytes: priv);

      expect(pair.publicKeyBytes, pub);
    });

    test('stores private key bytes', () {
      final pub = Uint8List.fromList([0x04]);
      final priv = Uint8List.fromList([0xAA, 0xBB, 0xCC]);

      final pair = DeviceKeyPair(publicKeyBytes: pub, privateKeyBytes: priv);

      expect(pair.privateKeyBytes, priv);
    });

    test('public and private key bytes are independent references', () {
      final pub = Uint8List.fromList([0x01]);
      final priv = Uint8List.fromList([0x02]);

      final pair = DeviceKeyPair(publicKeyBytes: pub, privateKeyBytes: priv);

      expect(identical(pair.publicKeyBytes, pair.privateKeyBytes), isFalse);
    });

    test('accepts empty key bytes', () {
      final pair = DeviceKeyPair(
        publicKeyBytes: Uint8List(0),
        privateKeyBytes: Uint8List(0),
      );

      expect(pair.publicKeyBytes.length, 0);
      expect(pair.privateKeyBytes.length, 0);
    });
  });
}
