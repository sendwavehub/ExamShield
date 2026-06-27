import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:examshield_mobile/domain/services/auth_service.dart';
import 'package:examshield_mobile/infrastructure/api/api_client.dart';
import 'package:examshield_mobile/infrastructure/storage/secure_storage.dart';
import 'package:examshield_mobile/domain/models/auth_token.dart';

class MockApiClient extends Mock implements ApiClient {}
class MockSecureStorage extends Mock implements SecureStorage {}

void main() {
  late MockApiClient mockApi;
  late MockSecureStorage mockStorage;
  late AuthService sut;
  late AuthNotifier notifier;

  setUp(() {
    mockApi = MockApiClient();
    mockStorage = MockSecureStorage();
    sut = AuthService(api: mockApi, storage: mockStorage);
    notifier = AuthNotifier(sut);
  });

  const pendingToken = AuthToken(
    accessToken: '',
    role: '',
    requiresMfa: true,
  );
  const fullToken = AuthToken(
    accessToken: 'jwt-mfa-complete',
    role: 'Invigilator',
    requiresMfa: false,
  );

  group('AuthService MFA login', () {
    test('login returns token with requiresMfa=true when server signals MFA', () async {
      when(() => mockApi.login(email: 'mfa@test.com', password: 'secret'))
          .thenAnswer((_) async => pendingToken);

      final result = await sut.login(email: 'mfa@test.com', password: 'secret');

      expect(result.requiresMfa, isTrue);
    });

    test('login does not store token when requiresMfa=true', () async {
      when(() => mockApi.login(email: 'mfa@test.com', password: 'secret'))
          .thenAnswer((_) async => pendingToken);

      await sut.login(email: 'mfa@test.com', password: 'secret');

      verifyNever(() => mockStorage.saveToken(any()));
    });

    test('login stores token when requiresMfa=false', () async {
      when(() => mockApi.login(email: 'a@b.com', password: 'pass'))
          .thenAnswer((_) async => fullToken);
      when(() => mockStorage.saveToken(fullToken)).thenAnswer((_) async {});

      await sut.login(email: 'a@b.com', password: 'pass');

      verify(() => mockStorage.saveToken(fullToken)).called(1);
    });

    test('completeMfaLogin calls mfaLogin and stores token', () async {
      when(() => mockApi.mfaLogin(
            email: 'mfa@test.com',
            password: 'secret',
            code: '123456',
          )).thenAnswer((_) async => fullToken);
      when(() => mockStorage.saveToken(fullToken)).thenAnswer((_) async {});

      await sut.completeMfaLogin(
          email: 'mfa@test.com', password: 'secret', code: '123456');

      verify(() => mockStorage.saveToken(fullToken)).called(1);
    });

    test('completeMfaLogin propagates ApiException on wrong code', () async {
      when(() => mockApi.mfaLogin(
            email: 'mfa@test.com',
            password: 'secret',
            code: '000000',
          )).thenThrow(ApiException(statusCode: 401, message: 'Invalid code'));

      expect(
        () => sut.completeMfaLogin(
            email: 'mfa@test.com', password: 'secret', code: '000000'),
        throwsA(isA<ApiException>()),
      );
    });
  });

  group('AuthNotifier MFA flow', () {
    test('sets requiresMfa=true after login when server signals MFA', () async {
      when(() => mockApi.login(email: 'mfa@test.com', password: 'secret'))
          .thenAnswer((_) async => pendingToken);

      await notifier.login(email: 'mfa@test.com', password: 'secret');

      expect(notifier.requiresMfa, isTrue);
      expect(notifier.isLoggedIn, isFalse);
    });

    test('completeMfaLogin sets isLoggedIn=true and clears requiresMfa', () async {
      when(() => mockApi.login(email: 'mfa@test.com', password: 'secret'))
          .thenAnswer((_) async => pendingToken);
      when(() => mockApi.mfaLogin(
            email: 'mfa@test.com',
            password: 'secret',
            code: '123456',
          )).thenAnswer((_) async => fullToken);
      when(() => mockStorage.saveToken(fullToken)).thenAnswer((_) async {});

      await notifier.login(email: 'mfa@test.com', password: 'secret');
      await notifier.completeMfaLogin(code: '123456');

      expect(notifier.isLoggedIn, isTrue);
      expect(notifier.requiresMfa, isFalse);
      expect(notifier.role, equals('Invigilator'));
    });

    test('completeMfaLogin sets error on wrong code', () async {
      when(() => mockApi.login(email: 'mfa@test.com', password: 'secret'))
          .thenAnswer((_) async => pendingToken);
      when(() => mockApi.mfaLogin(
            email: any(named: 'email'),
            password: any(named: 'password'),
            code: '000000',
          )).thenThrow(ApiException(statusCode: 401, message: 'Invalid code'));

      await notifier.login(email: 'mfa@test.com', password: 'secret');
      await notifier.completeMfaLogin(code: '000000');

      expect(notifier.isLoggedIn, isFalse);
      expect(notifier.requiresMfa, isTrue);
      expect(notifier.error, isNotNull);
    });
  });
}
