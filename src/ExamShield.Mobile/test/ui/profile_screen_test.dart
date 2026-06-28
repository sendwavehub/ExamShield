import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:examshield_mobile/domain/services/auth_service.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';
import 'package:examshield_mobile/ui/screens/profile_screen.dart';

class MockApiClient extends Mock implements ApiClient {}

class FakeLoggedInAuthNotifier extends ChangeNotifier implements AuthNotifier {
  @override bool get isLoggedIn => true;
  @override bool get requiresMfa => false;
  @override bool get mfaSetupRequired => false;
  @override String? get role => 'Invigilator';
  @override String? get error => null;
  @override Future<void> login({required String email, required String password}) async {}
  @override Future<void> logout() async {}
  @override Future<MfaSetupInfo?> beginMfaSetup() async => null;
  @override Future<void> confirmMfaSetup({required String code}) async {}
  @override Future<String?> currentToken() async => 'test-token';
}

Widget buildSubject(ApiClient api, AuthNotifier auth) => MaterialApp(
      home: MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: api),
          ChangeNotifierProvider<AuthNotifier>.value(value: auth as FakeLoggedInAuthNotifier),
        ],
        child: const ProfileScreen(),
      ),
    );

void main() {
  late MockApiClient mockApi;
  late FakeLoggedInAuthNotifier fakeAuth;

  setUp(() {
    mockApi = MockApiClient();
    fakeAuth = FakeLoggedInAuthNotifier();
    when(() => mockApi.getProfile(any()))
        .thenAnswer((_) async => UserProfile.fromJson({
              'email': 'alice@exam.io',
              'role': 'Invigilator',
              'mfaEnabled': false,
            }));
  });

  testWidgets('shows user email after load', (tester) async {
    await tester.pumpWidget(buildSubject(mockApi, fakeAuth));
    await tester.pumpAndSettle();
    expect(find.text('alice@exam.io'), findsOneWidget);
  });

  testWidgets('shows role badge', (tester) async {
    await tester.pumpWidget(buildSubject(mockApi, fakeAuth));
    await tester.pumpAndSettle();
    expect(find.text('Invigilator'), findsAtLeastNWidgets(1));
  });

  testWidgets('shows Sign Out button', (tester) async {
    await tester.pumpWidget(buildSubject(mockApi, fakeAuth));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(ElevatedButton, 'Sign Out'), findsOneWidget);
  });

  testWidgets('shows MFA disabled indicator when mfaEnabled is false', (tester) async {
    await tester.pumpWidget(buildSubject(mockApi, fakeAuth));
    await tester.pumpAndSettle();
    expect(find.textContaining('Disabled'), findsOneWidget);
  });
}
