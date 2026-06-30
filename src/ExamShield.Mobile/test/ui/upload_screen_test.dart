import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:examshield_mobile/domain/services/capture_service.dart';
import 'package:examshield_mobile/infrastructure/queue/offline_queue.dart';
import 'package:examshield_mobile/ui/screens/upload_screen.dart';

class MockCaptureService extends Mock implements CaptureService {}
class MockOfflineQueue extends Mock implements OfflineQueue {}

PendingCapture makePending({
  String captureId = 'cap-abc12345',
  int sizeBytes = 2048,
  int retryCount = 0,
}) =>
    PendingCapture(
      captureId: captureId,
      imageBytes: Uint8List(sizeBytes),
      createdAt: DateTime(2026, 6, 30),
      retryCount: retryCount,
    );

Widget buildSubject(MockCaptureService service) => MaterialApp(
      home: Provider<CaptureService>.value(
        value: service,
        child: const UploadScreen(),
      ),
    );

void main() {
  late MockCaptureService mockService;
  late MockOfflineQueue mockQueue;

  setUpAll(() {
    registerFallbackValue(makePending());
  });

  setUp(() {
    mockService = MockCaptureService();
    mockQueue = MockOfflineQueue();
    when(() => mockService.offlineQueue).thenReturn(mockQueue);
    when(() => mockQueue.listPending()).thenAnswer((_) async => []);
    when(() => mockQueue.markDone(any())).thenAnswer((_) async {});
    when(() => mockService.retrySingle(any()))
        .thenAnswer((_) async => true);
    when(() => mockService.syncPendingUploads())
        .thenAnswer((_) async => const SyncResult(uploaded: 0, failed: 0));
  });

  testWidgets('shows Uploads title when queue is empty', (tester) async {
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('Uploads'), findsOneWidget);
  });

  testWidgets('shows empty-state message when queue is empty', (tester) async {
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('All uploads complete'), findsOneWidget);
  });

  testWidgets('shows empty-state icon when queue is empty', (tester) async {
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.byIcon(Icons.cloud_done_outlined), findsAtLeastNWidgets(1));
  });

  testWidgets('shows item count in title when queue has items', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending(), makePending(captureId: 'cap-zzz99999')]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('Uploads (2)'), findsOneWidget);
  });

  testWidgets('renders a card for each pending item', (tester) async {
    when(() => mockQueue.listPending()).thenAnswer((_) async => [
          makePending(captureId: 'cap-aaa11111'),
          makePending(captureId: 'cap-bbb22222'),
        ]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('cap-aaa1…'), findsOneWidget);
    expect(find.text('cap-bbb2…'), findsOneWidget);
  });

  testWidgets('shows file size in KB on each card', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending(sizeBytes: 1024)]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.textContaining('1.0 KB'), findsOneWidget);
  });

  testWidgets('shows retry count when retryCount > 0', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending(retryCount: 3)]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.textContaining('3 retries'), findsOneWidget);
  });

  testWidgets('does not show retry count text when retryCount is 0', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending(retryCount: 0)]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.textContaining('retries'), findsNothing);
  });

  testWidgets('shows Retry button for each queued item', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending()]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('Retry'), findsOneWidget);
  });

  testWidgets('shows Cancel button for each queued item', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending()]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('Cancel'), findsOneWidget);
  });

  testWidgets('tapping Cancel calls markDone with captureId', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending(captureId: 'cap-abc12345')]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();

    when(() => mockQueue.listPending()).thenAnswer((_) async => []);
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();

    verify(() => mockQueue.markDone('cap-abc12345')).called(1);
  });

  testWidgets('tapping Retry calls retrySingle with the capture', (tester) async {
    final capture = makePending(captureId: 'cap-abc12345');
    when(() => mockQueue.listPending()).thenAnswer((_) async => [capture]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();

    when(() => mockQueue.listPending()).thenAnswer((_) async => []);
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();

    verify(() => mockService.retrySingle(any(
      that: predicate<PendingCapture>((c) => c.captureId == 'cap-abc12345'),
    ))).called(1);
  });

  testWidgets('shows Sync All button when items are present', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending()]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('Sync All'), findsOneWidget);
  });

  testWidgets('tapping Sync All calls syncPendingUploads', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending()]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Sync All'));
    await tester.pumpAndSettle();

    verify(() => mockService.syncPendingUploads()).called(1);
  });

  testWidgets('shows sync result after Sync All completes', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending()]);
    when(() => mockService.syncPendingUploads())
        .thenAnswer((_) async => const SyncResult(uploaded: 1, failed: 0));
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Sync All'));
    await tester.pumpAndSettle();

    expect(find.textContaining('1 uploaded'), findsOneWidget);
  });

  testWidgets('shows Syncing label while Sync All is in progress', (tester) async {
    when(() => mockQueue.listPending())
        .thenAnswer((_) async => [makePending()]);
    when(() => mockService.syncPendingUploads())
        .thenAnswer((_) async {
          await Future<void>.delayed(const Duration(milliseconds: 100));
          return const SyncResult(uploaded: 1, failed: 0);
        });
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Sync All'));
    await tester.pump(); // one frame — sync in progress

    expect(find.text('Syncing…'), findsOneWidget);
    await tester.pumpAndSettle();
  });
}
