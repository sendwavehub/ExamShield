import 'dart:typed_data';

class DeviceKeyPair {
  final Uint8List publicKeyBytes;
  final Uint8List privateKeyBytes;

  const DeviceKeyPair({required this.publicKeyBytes, required this.privateKeyBytes});
}
