import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:examshield_mobile/domain/services/capture_service.dart';
import 'package:examshield_mobile/domain/models/capture.dart';
import 'package:examshield_mobile/ui/screens/history_screen.dart';

class MockCaptureService extends Mock implements CaptureService {}

Widget buildSubject(CaptureService service) => MaterialApp(
      home: Provider<CaptureService>.value(
        value: service,
        child: const HistoryScreen(),
      ),
    );

void main() {
  late MockCaptureService mockService;

  setUp(() => mockService = MockCaptureService());

  testWidgets('shows Capture History title', (tester) async {
    when(() => mockService.myCaptures()).thenAnswer((_) async => []);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.text('Capture History'), findsOneWidget);
  });

  testWidgets('shows empty state when no captures', (tester) async {
    when(() => mockService.myCaptures()).thenAnswer((_) async => []);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.textContaining('No captures'), findsOneWidget);
  });

  testWidgets('shows capture list when captures exist', (tester) async {
    final record = CaptureRecord(
      captureId: 'abc-123',
      examId: 'exam-1',
      studentId: 'stu-1',
      pageNumber: 1,
      status: CaptureStatus.verified,
      capturedAt: DateTime.now(),
    );
    when(() => mockService.myCaptures()).thenAnswer((_) async => [record]);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.byType(ListTile), findsOneWidget);
  });

  testWidgets('shows loading indicator initially', (tester) async {
    when(() => mockService.myCaptures()).thenAnswer(
        (_) async => Future.delayed(const Duration(seconds: 1), () => []));
    await tester.pumpWidget(buildSubject(mockService));
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
