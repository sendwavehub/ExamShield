import 'package:flutter_test/flutter_test.dart';
import 'package:examshield_mobile/domain/models/auth_token.dart';

void main() {
  group('AuthToken.fromJson', () {
    test('maps token and role fields', () {
      final token = AuthToken.fromJson({
        'token': 'jwt-abc',
        'role': 'Invigilator',
      });

      expect(token.accessToken, 'jwt-abc');
      expect(token.role, 'Invigilator');
    });

    test('defaults requiresMfa to false when absent', () {
      final token = AuthToken.fromJson({'token': 't', 'role': 'r'});

      expect(token.requiresMfa, isFalse);
    });

    test('defaults mfaSetupRequired to false when absent', () {
      final token = AuthToken.fromJson({'token': 't', 'role': 'r'});

      expect(token.mfaSetupRequired, isFalse);
    });

    test('reads requiresMfa true', () {
      final token = AuthToken.fromJson(
          {'token': 't', 'role': 'r', 'requiresMfa': true});

      expect(token.requiresMfa, isTrue);
    });

    test('reads mfaSetupRequired true', () {
      final token = AuthToken.fromJson(
          {'token': 't', 'role': 'r', 'mfaSetupRequired': true});

      expect(token.mfaSetupRequired, isTrue);
    });

    test('returns empty strings for missing token and role', () {
      final token = AuthToken.fromJson({});

      expect(token.accessToken, '');
      expect(token.role, '');
    });
  });

  group('AuthToken.toJson', () {
    test('round-trips all fields', () {
      const original = AuthToken(
        accessToken: 'jwt-xyz',
        role: 'Administrator',
        requiresMfa: true,
        mfaSetupRequired: false,
      );

      final json = original.toJson();

      expect(json['token'], 'jwt-xyz');
      expect(json['role'], 'Administrator');
      expect(json['requiresMfa'], isTrue);
      expect(json['mfaSetupRequired'], isFalse);
    });

    test('toJson then fromJson preserves all values', () {
      const original = AuthToken(
        accessToken: 'tok',
        role: 'Operator',
        requiresMfa: false,
        mfaSetupRequired: true,
      );

      final restored = AuthToken.fromJson(original.toJson());

      expect(restored.accessToken, original.accessToken);
      expect(restored.role, original.role);
      expect(restored.requiresMfa, original.requiresMfa);
      expect(restored.mfaSetupRequired, original.mfaSetupRequired);
    });
  });

  group('AuthToken constructor defaults', () {
    test('requiresMfa defaults to false', () {
      const token = AuthToken(accessToken: 'tok', role: 'r');
      expect(token.requiresMfa, isFalse);
    });

    test('mfaSetupRequired defaults to false', () {
      const token = AuthToken(accessToken: 'tok', role: 'r');
      expect(token.mfaSetupRequired, isFalse);
    });
  });
}
