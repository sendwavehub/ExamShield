import 'dart:typed_data';

class PendingCapture {
  final String captureId;
  final Uint8List imageBytes;
  final DateTime createdAt;
  final int retryCount;

  const PendingCapture({
    required this.captureId,
    required this.imageBytes,
    required this.createdAt,
    required this.retryCount,
  });

  PendingCapture copyWith({
    String? captureId,
    Uint8List? imageBytes,
    DateTime? createdAt,
    int? retryCount,
  }) => PendingCapture(
        captureId: captureId ?? this.captureId,
        imageBytes: imageBytes ?? this.imageBytes,
        createdAt: createdAt ?? this.createdAt,
        retryCount: retryCount ?? this.retryCount,
      );
}

/// Port — implemented by SqliteOfflineQueue (production) and InMemoryOfflineQueue (tests).
abstract interface class OfflineQueue {
  Future<void> enqueue(PendingCapture capture);
  Future<List<PendingCapture>> listPending();
  Future<void> markDone(String captureId);
  Future<void> incrementRetry(String captureId);
  Future<int> pendingCount();
}
