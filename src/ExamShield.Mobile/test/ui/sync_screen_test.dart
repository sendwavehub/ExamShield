import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:examshield_mobile/domain/services/capture_service.dart';
import 'package:examshield_mobile/domain/services/crypto_service.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';
import 'package:examshield_mobile/infrastructure/queue/offline_queue.dart';
import 'package:examshield_mobile/infrastructure/storage/secure_storage.dart';
import 'package:examshield_mobile/ui/screens/sync_screen.dart';

class MockOfflineQueue extends Mock implements OfflineQueue {}
class MockApiClient extends Mock implements ApiClient {}

Widget buildSubject(CaptureService service) => MaterialApp(
      home: Provider<CaptureService>.value(
        value: service,
        child: const SyncScreen(),
      ),
    );

CaptureService makeService(MockOfflineQueue queue) => CaptureService(
      api: MockApiClient(),
      storage: const SecureStorage(),
      crypto: CryptoService(),
      offlineQueue: queue,
    );

void main() {
  late MockOfflineQueue mockQueue;

  setUp(() {
    mockQueue = MockOfflineQueue();
    when(() => mockQueue.pendingCount()).thenAnswer((_) async => 0);
  });

  testWidgets('shows Offline Sync title', (tester) async {
    await tester.pumpWidget(buildSubject(makeService(mockQueue)));
    await tester.pumpAndSettle();
    expect(find.text('Offline Sync'), findsOneWidget);
  });

  testWidgets('shows pending count when items queued', (tester) async {
    when(() => mockQueue.pendingCount()).thenAnswer((_) async => 3);
    await tester.pumpWidget(buildSubject(makeService(mockQueue)));
    await tester.pumpAndSettle();
    expect(find.textContaining('3'), findsAtLeastNWidgets(1));
  });

  testWidgets('shows Sync Now button', (tester) async {
    await tester.pumpWidget(buildSubject(makeService(mockQueue)));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(ElevatedButton, 'Sync Now'), findsOneWidget);
  });

  testWidgets('shows zero pending message when queue is empty', (tester) async {
    when(() => mockQueue.pendingCount()).thenAnswer((_) async => 0);
    await tester.pumpWidget(buildSubject(makeService(mockQueue)));
    await tester.pumpAndSettle();
    expect(find.textContaining('0'), findsAtLeastNWidgets(1));
  });
}
