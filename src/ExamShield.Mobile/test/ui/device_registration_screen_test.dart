import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';
import 'package:examshield_mobile/domain/services/device_registration_service.dart';
import 'package:examshield_mobile/domain/services/auth_service.dart';
import 'package:examshield_mobile/ui/screens/device_registration_screen.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';

class MockDeviceRegistrationService extends Mock
    implements DeviceRegistrationService {}

class MockAuthNotifier extends ChangeNotifier implements AuthNotifier {
  @override
  bool get isLoggedIn => true;
  @override
  String? get role => 'Invigilator';
  @override
  String? get error => null;
  @override
  Future<void> login({required String email, required String password}) async {}
  @override
  Future<void> logout() async {}
}

Widget buildSubject(DeviceRegistrationService service) => MaterialApp(
      home: ChangeNotifierProvider<AuthNotifier>.value(
        value: MockAuthNotifier(),
        child: Provider<DeviceRegistrationService>.value(
          value: service,
          child: const DeviceRegistrationScreen(),
        ),
      ),
    );

void main() {
  late MockDeviceRegistrationService mockService;

  setUp(() {
    mockService = MockDeviceRegistrationService();
    when(() => mockService.isRegistered()).thenAnswer((_) async => false);
  });

  testWidgets('shows device name field', (tester) async {
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(TextFormField, 'Device Name'), findsOneWidget);
  });

  testWidgets('shows Register Device button', (tester) async {
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(ElevatedButton, 'Register Device'), findsOneWidget);
  });

  testWidgets('shows already registered message when device is registered',
      (tester) async {
    when(() => mockService.isRegistered()).thenAnswer((_) async => true);
    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    expect(find.textContaining('already registered'), findsOneWidget);
  });

  testWidgets('calls service.register on button tap', (tester) async {
    when(() => mockService.register(
          name: any(named: 'name'),
          token: any(named: 'token'),
        )).thenAnswer((_) async => 'device-id-123');

    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    await tester.enterText(
        find.widgetWithText(TextFormField, 'Device Name'), 'My Phone');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Register Device'));
    await tester.pumpAndSettle();

    verify(() => mockService.register(
          name: 'My Phone',
          token: any(named: 'token'),
        )).called(1);
  });

  testWidgets('shows success message after registration', (tester) async {
    when(() => mockService.register(
          name: any(named: 'name'),
          token: any(named: 'token'),
        )).thenAnswer((_) async => 'device-id-123');

    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    await tester.enterText(
        find.widgetWithText(TextFormField, 'Device Name'), 'My Phone');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Register Device'));
    await tester.pumpAndSettle();

    expect(find.textContaining('registered'), findsWidgets);
  });

  testWidgets('shows error on ApiException', (tester) async {
    when(() => mockService.register(
          name: any(named: 'name'),
          token: any(named: 'token'),
        )).thenThrow(
            const ApiException(statusCode: 409, message: 'Device conflict'));

    await tester.pumpWidget(buildSubject(mockService));
    await tester.pumpAndSettle();
    await tester.enterText(
        find.widgetWithText(TextFormField, 'Device Name'), 'My Phone');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Register Device'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Device conflict'), findsOneWidget);
  });
}
