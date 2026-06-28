import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:examshield_mobile/domain/models/capture.dart';

void main() {
  group('CaptureRecord.fromJson', () {
    Map<String, dynamic> validJson({String status = 'created'}) => {
          'captureId': 'cap-1',
          'examId': 'exam-1',
          'studentId': 'stu-1',
          'status': status,
          'capturedAt': '2026-06-28T10:00:00.000Z',
          'hashHex': 'aabb',
        };

    test('parses all fields correctly', () {
      final record = CaptureRecord.fromJson(validJson());

      expect(record.captureId, 'cap-1');
      expect(record.examId, 'exam-1');
      expect(record.studentId, 'stu-1');
      expect(record.hashHex, 'aabb');
    });

    test('parses capturedAt as DateTime', () {
      final record = CaptureRecord.fromJson(validJson());

      expect(record.capturedAt, DateTime.parse('2026-06-28T10:00:00.000Z'));
    });

    test('parses status created', () {
      final record = CaptureRecord.fromJson(validJson(status: 'created'));
      expect(record.status, CaptureStatus.created);
    });

    test('parses status uploaded (case-insensitive)', () {
      final record = CaptureRecord.fromJson(validJson(status: 'Uploaded'));
      expect(record.status, CaptureStatus.uploaded);
    });

    test('parses status verified', () {
      final record = CaptureRecord.fromJson(validJson(status: 'verified'));
      expect(record.status, CaptureStatus.verified);
    });

    test('parses status tampered', () {
      final record = CaptureRecord.fromJson(validJson(status: 'tampered'));
      expect(record.status, CaptureStatus.tampered);
    });

    test('unknown status falls back to created', () {
      final record = CaptureRecord.fromJson(validJson(status: 'unknown_xyz'));
      expect(record.status, CaptureStatus.created);
    });

    test('missing hashHex defaults to empty string', () {
      final json = validJson()..remove('hashHex');
      final record = CaptureRecord.fromJson(json);
      expect(record.hashHex, '');
    });
  });

  group('CaptureRequest', () {
    test('stores all provided fields', () {
      final sig = Uint8List.fromList([0x01, 0x02]);
      const request = CaptureRequest(
        examId: 'exam-1',
        studentId: 'stu-1',
        deviceId: 'dev-1',
        pageNumber: 2,
        hashHex: 'deadbeef',
        signatureBytes: Uint8List.fromList([0x01, 0x02]),
      );

      expect(request.examId, 'exam-1');
      expect(request.studentId, 'stu-1');
      expect(request.deviceId, 'dev-1');
      expect(request.pageNumber, 2);
      expect(request.hashHex, 'deadbeef');
    });

    test('stores signature bytes reference', () {
      final sig = Uint8List.fromList([0xAA, 0xBB, 0xCC]);
      final request = CaptureRequest(
        examId: 'e',
        studentId: 's',
        deviceId: 'd',
        pageNumber: 1,
        hashHex: 'ff',
        signatureBytes: sig,
      );

      expect(request.signatureBytes, sig);
    });
  });

  group('CaptureStatus enum', () {
    test('has four values', () {
      expect(CaptureStatus.values.length, 4);
    });

    test('names are correct', () {
      expect(CaptureStatus.created.name, 'created');
      expect(CaptureStatus.uploaded.name, 'uploaded');
      expect(CaptureStatus.verified.name, 'verified');
      expect(CaptureStatus.tampered.name, 'tampered');
    });
  });
}
