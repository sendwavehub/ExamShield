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

  setUp(() {
    mockApi = MockApiClient();
    mockStorage = MockSecureStorage();
    sut = AuthService(api: mockApi, storage: mockStorage);
  });

  group('AuthService.login', () {
    test('stores token on successful login', () async {
      const token = AuthToken(accessToken: 'jwt-abc', role: 'Invigilator');
      when(() => mockApi.login(email: 'a@b.com', password: 'pass'))
          .thenAnswer((_) async => token);
      when(() => mockStorage.saveToken(token)).thenAnswer((_) async {});

      await sut.login(email: 'a@b.com', password: 'pass');

      verify(() => mockStorage.saveToken(token)).called(1);
    });

    test('isLoggedIn returns false when no token stored', () async {
      when(() => mockStorage.loadToken()).thenAnswer((_) async => null);
      expect(await sut.isLoggedIn(), isFalse);
    });

    test('isLoggedIn returns true when token is stored', () async {
      const token = AuthToken(accessToken: 'jwt-abc', role: 'Invigilator');
      when(() => mockStorage.loadToken()).thenAnswer((_) async => token);
      expect(await sut.isLoggedIn(), isTrue);
    });

    test('propagates ApiException on bad credentials', () async {
      when(() => mockApi.login(email: any(named: 'email'), password: any(named: 'password')))
          .thenThrow(ApiException(statusCode: 401, message: 'Unauthorized'));

      expect(
        () => sut.login(email: 'bad@example.com', password: 'wrong'),
        throwsA(isA<ApiException>()),
      );
    });
  });

  group('AuthService.logout', () {
    test('clears stored token', () async {
      when(() => mockStorage.clearToken()).thenAnswer((_) async {});
      await sut.logout();
      verify(() => mockStorage.clearToken()).called(1);
    });
  });

  group('AuthService.currentToken', () {
    test('returns null when not logged in', () async {
      when(() => mockStorage.loadToken()).thenAnswer((_) async => null);
      expect(await sut.currentToken(), isNull);
    });

    test('returns stored token when logged in', () async {
      const token = AuthToken(accessToken: 'jwt-xyz', role: 'Invigilator');
      when(() => mockStorage.loadToken()).thenAnswer((_) async => token);
      final result = await sut.currentToken();
      expect(result?.accessToken, equals('jwt-xyz'));
    });
  });
}
