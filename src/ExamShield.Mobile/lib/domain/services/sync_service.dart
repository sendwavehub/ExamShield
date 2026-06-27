import 'package:connectivity_plus/connectivity_plus.dart';
import '../../infrastructure/api/api_client.dart';
import '../../infrastructure/queue/offline_queue.dart';
import '../../infrastructure/storage/secure_storage.dart';

class SyncService {
  final OfflineQueue queue;
  final ApiClient api;
  final SecureStorage storage;

  const SyncService({
    required this.queue,
    required this.api,
    required this.storage,
  });

  /// Call this when connectivity is restored.
  Future<SyncResult> syncPending() async {
    final token = await storage.loadToken();
    if (token == null) return const SyncResult(uploaded: 0, failed: 0);

    final pending = await queue.listPending();
    int uploaded = 0;
    int failed = 0;

    for (final capture in pending) {
      try {
        await api.uploadImage(capture.captureId, capture.imageBytes, token.accessToken);
        await queue.markDone(capture.captureId);
        uploaded++;
      } catch (_) {
        await queue.incrementRetry(capture.captureId);
        failed++;
      }
    }

    return SyncResult(uploaded: uploaded, failed: failed);
  }

  /// Listen for connectivity changes and trigger sync automatically.
  Stream<SyncResult> watchAndSync() async* {
    await for (final result in Connectivity().onConnectivityChanged) {
      final isOnline = result.any((r) => r != ConnectivityResult.none);
      if (isOnline) {
        yield await syncPending();
      }
    }
  }
}

class SyncResult {
  final int uploaded;
  final int failed;
  const SyncResult({required this.uploaded, required this.failed});
}
