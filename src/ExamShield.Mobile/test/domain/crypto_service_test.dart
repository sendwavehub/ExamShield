import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:examshield_mobile/domain/services/crypto_service.dart';

void main() {
  late CryptoService sut;

  setUp(() => sut = CryptoService());

  group('CryptoService.hashBytes', () {
    test('returns 32-byte SHA-256 digest', () {
      final bytes = Uint8List.fromList([1, 2, 3, 4]);
      final hash = sut.hashBytes(bytes);
      expect(hash, hasLength(32));
    });

    test('same input produces same hash', () {
      final bytes = Uint8List.fromList([10, 20, 30]);
      expect(sut.hashBytes(bytes), equals(sut.hashBytes(bytes)));
    });

    test('different input produces different hash', () {
      final a = sut.hashBytes(Uint8List.fromList([1]));
      final b = sut.hashBytes(Uint8List.fromList([2]));
      expect(a, isNot(equals(b)));
    });

    test('returns hex string of length 64', () {
      final bytes = Uint8List.fromList([0, 255, 128]);
      final hex = sut.hashHex(bytes);
      expect(hex, hasLength(64));
      expect(RegExp(r'^[0-9a-f]+$').hasMatch(hex), isTrue);
    });

    test('empty input hashes to known SHA-256 value', () {
      final hex = sut.hashHex(Uint8List(0));
      expect(
        hex,
        equals('e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'),
      );
    });
  });

  group('CryptoService key generation', () {
    test('generateKeyPair returns non-null public and private keys', () async {
      final pair = await sut.generateKeyPair();
      expect(pair.publicKeyBytes, isNotEmpty);
      expect(pair.privateKeyBytes, isNotEmpty);
    });

    test('public key is different from private key', () async {
      final pair = await sut.generateKeyPair();
      expect(pair.publicKeyBytes, isNot(equals(pair.privateKeyBytes)));
    });

    test('each call generates a different key pair', () async {
      final a = await sut.generateKeyPair();
      final b = await sut.generateKeyPair();
      expect(a.publicKeyBytes, isNot(equals(b.publicKeyBytes)));
    });
  });

  group('CryptoService.sign', () {
    test('sign returns non-empty signature bytes', () async {
      final pair = await sut.generateKeyPair();
      final data = Uint8List.fromList([1, 2, 3]);
      final sig = await sut.sign(data, pair.privateKeyBytes);
      expect(sig, isNotEmpty);
    });

    test('signature verifies against public key', () async {
      final pair = await sut.generateKeyPair();
      final data = Uint8List.fromList([1, 2, 3, 4, 5]);
      final sig = await sut.sign(data, pair.privateKeyBytes);
      final valid = await sut.verify(data, sig, pair.publicKeyBytes);
      expect(valid, isTrue);
    });

    test('tampered data fails verification', () async {
      final pair = await sut.generateKeyPair();
      final data = Uint8List.fromList([1, 2, 3]);
      final sig = await sut.sign(data, pair.privateKeyBytes);
      final tampered = Uint8List.fromList([1, 2, 4]); // last byte changed
      final valid = await sut.verify(tampered, sig, pair.publicKeyBytes);
      expect(valid, isFalse);
    });
  });
}
