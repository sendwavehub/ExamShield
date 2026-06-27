import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:examshield_mobile/domain/services/auth_service.dart';
import 'package:examshield_mobile/ui/screens/login_screen.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';

class MockAuthService extends Mock implements AuthService {}

Widget buildSubject(AuthService auth) => MaterialApp(
      home: ChangeNotifierProvider<AuthNotifier>(
        create: (_) => AuthNotifier(auth),
        child: const LoginScreen(),
      ),
    );

void main() {
  late MockAuthService mockAuth;

  setUp(() => mockAuth = MockAuthService());

  testWidgets('shows email and password fields', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    expect(find.byType(TextFormField), findsNWidgets(2));
  });

  testWidgets('shows ExamShield branding', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    expect(find.textContaining('ExamShield'), findsOneWidget);
  });

  testWidgets('shows Sign In button', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    expect(find.widgetWithText(ElevatedButton, 'Sign In'), findsOneWidget);
  });

  testWidgets('shows validation error when fields are empty', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign In'));
    await tester.pump();
    expect(find.text('Email is required'), findsOneWidget);
  });

  testWidgets('shows invalid email error for bad format', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.enterText(find.byType(TextFormField).first, 'notanemail');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign In'));
    await tester.pump();
    expect(find.text('Enter a valid email'), findsOneWidget);
  });

  testWidgets('calls auth.login with entered credentials', (tester) async {
    when(() => mockAuth.login(email: 'inv@exam.com', password: 'secret'))
        .thenAnswer((_) async {});

    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.enterText(find.byType(TextFormField).first, 'inv@exam.com');
    await tester.enterText(find.byType(TextFormField).last, 'secret');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign In'));
    await tester.pump();

    verify(() => mockAuth.login(email: 'inv@exam.com', password: 'secret')).called(1);
  });

  testWidgets('shows error snackbar on ApiException', (tester) async {
    when(() => mockAuth.login(email: any(named: 'email'), password: any(named: 'password')))
        .thenThrow(ApiException(statusCode: 401, message: 'Invalid credentials'));

    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.enterText(find.byType(TextFormField).first, 'bad@exam.com');
    await tester.enterText(find.byType(TextFormField).last, 'wrong');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign In'));
    await tester.pumpAndSettle();

    expect(find.text('Invalid credentials'), findsOneWidget);
  });
}
