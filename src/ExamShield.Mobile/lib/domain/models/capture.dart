import 'dart:typed_data';

enum CaptureStatus { created, uploaded, verified, tampered }

class CaptureRequest {
  final String examId;
  final String studentId;
  final String deviceId;
  final int pageNumber;
  final String hashHex;
  final Uint8List signatureBytes;

  const CaptureRequest({
    required this.examId,
    required this.studentId,
    required this.deviceId,
    required this.pageNumber,
    required this.hashHex,
    required this.signatureBytes,
  });
}

class CaptureRecord {
  final String captureId;
  final String examId;
  final String studentId;
  final CaptureStatus status;
  final DateTime capturedAt;
  final String hashHex;

  const CaptureRecord({
    required this.captureId,
    required this.examId,
    required this.studentId,
    required this.status,
    required this.capturedAt,
    required this.hashHex,
  });

  factory CaptureRecord.fromJson(Map<String, dynamic> json) => CaptureRecord(
        captureId: json['captureId'] as String,
        examId: json['examId'] as String,
        studentId: json['studentId'] as String,
        status: CaptureStatus.values.firstWhere(
          (s) => s.name.toLowerCase() == (json['status'] as String).toLowerCase(),
          orElse: () => CaptureStatus.created,
        ),
        capturedAt: DateTime.parse(json['capturedAt'] as String),
        hashHex: json['hashHex'] as String? ?? '',
      );
}
