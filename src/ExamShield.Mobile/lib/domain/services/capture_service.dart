import 'dart:convert';
import 'dart:typed_data';
import '../../infrastructure/api/api_client.dart';
import '../../infrastructure/queue/offline_queue.dart';
import '../../infrastructure/storage/secure_storage.dart';
import '../models/capture.dart';
import 'crypto_service.dart';

class SyncResult {
  final int uploaded;
  final int failed;
  const SyncResult({required this.uploaded, required this.failed});
}

class CaptureService {
  final ApiClient api;
  final SecureStorage storage;
  final CryptoService crypto;
  final OfflineQueue offlineQueue; // exposed for UI to show pending count

  const CaptureService({
    required this.api,
    required this.storage,
    required this.crypto,
    required this.offlineQueue,
  });

  Future<String> hashSignAndRegister(Uint8List imageBytes) async {
    final hashHex = crypto.hashHex(imageBytes);
    final token = await storage.loadToken();
    if (token == null) throw const ApiException(statusCode: 401, message: 'Not authenticated');

    final privKeyB64 = await storage.loadPrivateKey();
    if (privKeyB64 == null) throw Exception('No device key found — register device first');
    final privKeyBytes = base64Decode(privKeyB64);

    final hashBytes = crypto.hashBytes(imageBytes);
    final signature = await crypto.sign(hashBytes, privKeyBytes);
    final deviceId = await storage.loadDeviceId() ?? '';

    final request = CaptureRequest(
      examId: '',
      studentId: '',
      deviceId: deviceId,
      pageNumber: 1,
      hashHex: hashHex,
      signatureBytes: signature,
    );
    return api.registerCapture(request, token.accessToken);
  }

  Future<void> upload(String captureId, Uint8List imageBytes) async {
    final token = await storage.loadToken();
    if (token == null) throw const ApiException(statusCode: 401, message: 'Not authenticated');
    await api.uploadImage(captureId, imageBytes, token.accessToken);
  }

  /// Uploads immediately; enqueues for later sync if the network is unavailable.
  Future<void> uploadWithFallback(String captureId, Uint8List imageBytes) async {
    try {
      await upload(captureId, imageBytes);
    } on ApiException catch (e) {
      if (e.statusCode == 0) {
        await offlineQueue.enqueue(PendingCapture(
          captureId: captureId,
          imageBytes: imageBytes,
          createdAt: DateTime.now(),
          retryCount: 0,
        ));
      } else {
        rethrow;
      }
    }
  }

  /// Drains the offline queue by uploading all pending captures.
  Future<SyncResult> syncPendingUploads() async {
    final token = await storage.loadToken();
    if (token == null) return const SyncResult(uploaded: 0, failed: 0);

    final pending = await offlineQueue.listPending();
    int uploaded = 0;
    int failed = 0;

    for (final capture in pending) {
      try {
        await api.uploadImage(capture.captureId, capture.imageBytes, token.accessToken);
        await offlineQueue.markDone(capture.captureId);
        uploaded++;
      } catch (_) {
        await offlineQueue.incrementRetry(capture.captureId);
        failed++;
      }
    }

    return SyncResult(uploaded: uploaded, failed: failed);
  }

  Future<List<CaptureRecord>> myCaptures() async {
    final token = await storage.loadToken();
    if (token == null) return [];
    return api.getMyCaptures(token.accessToken);
  }
}
