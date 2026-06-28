import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:examshield_mobile/domain/services/auth_service.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';
import 'package:examshield_mobile/ui/screens/mfa_setup_screen.dart';

class MockAuthNotifier extends ChangeNotifier implements AuthNotifier {
  @override bool get isLoggedIn => false;
  @override bool get requiresMfa => false;
  @override bool get mfaSetupRequired => true;
  @override String? get role => null;
  @override String? get error => null;
  @override Future<void> login({required String email, required String password}) async {}
  @override Future<void> logout() async {}
  @override Future<String?> currentToken() async => 'pending-token';
  @override Future<MfaSetupInfo?> beginMfaSetup() async =>
      const MfaSetupInfo(secret: 'JBSWY3DPEHPK3PXP', qrUri: 'otpauth://totp/ExamShield?secret=JBSWY3DPEHPK3PXP');
  @override Future<void> confirmMfaSetup({required String code}) async {}
}

Widget buildSubject(AuthNotifier auth) => MaterialApp(
      home: ChangeNotifierProvider<AuthNotifier>.value(
        value: auth as MockAuthNotifier,
        child: const MfaSetupScreen(),
      ),
    );

void main() {
  late MockAuthNotifier mockAuth;

  setUp(() => mockAuth = MockAuthNotifier());

  testWidgets('shows loading indicator initially', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });

  testWidgets('shows secret key after load', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.pumpAndSettle();
    expect(find.text('JBSWY3DPEHPK3PXP'), findsOneWidget);
  });

  testWidgets('shows TOTP code input field', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.pumpAndSettle();
    expect(find.byType(TextFormField), findsOneWidget);
  });

  testWidgets('shows Activate button', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(ElevatedButton, 'Activate'), findsOneWidget);
  });

  testWidgets('shows validation error when code is empty', (tester) async {
    await tester.pumpWidget(buildSubject(mockAuth));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Activate'));
    await tester.pumpAndSettle();
    expect(find.textContaining('6-digit'), findsOneWidget);
  });
}
