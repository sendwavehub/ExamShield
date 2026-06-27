import 'package:flutter/foundation.dart';
import '../../infrastructure/api/api_client.dart';
import '../../infrastructure/storage/secure_storage.dart';
import '../models/auth_token.dart';

class AuthService {
  final ApiClient api;
  final SecureStorage storage;

  const AuthService({required this.api, required this.storage});

  Future<AuthToken> login({required String email, required String password}) async {
    final token = await api.login(email: email, password: password);
    if (!token.requiresMfa) {
      await storage.saveToken(token);
    }
    return token;
  }

  Future<AuthToken> completeMfaLogin({
    required String email,
    required String password,
    required String code,
  }) async {
    final token = await api.mfaLogin(email: email, password: password, code: code);
    await storage.saveToken(token);
    return token;
  }

  Future<void> logout() => storage.clearToken();

  Future<bool> isLoggedIn() async => (await storage.loadToken()) != null;

  Future<AuthToken?> currentToken() => storage.loadToken();
}

class AuthNotifier extends ChangeNotifier {
  final AuthService _auth;
  bool _isLoggedIn = false;
  bool _requiresMfa = false;
  String? _role;
  String? _error;
  String? _pendingEmail;
  String? _pendingPassword;

  bool get isLoggedIn => _isLoggedIn;
  bool get requiresMfa => _requiresMfa;
  String? get role => _role;
  String? get error => _error;

  AuthNotifier(this._auth);

  Future<void> login({required String email, required String password}) async {
    _error = null;
    try {
      final token = await _auth.login(email: email, password: password);
      if (token.requiresMfa) {
        _requiresMfa = true;
        _pendingEmail = email;
        _pendingPassword = password;
      } else {
        _role = token.role;
        _isLoggedIn = true;
        _requiresMfa = false;
        _pendingEmail = null;
        _pendingPassword = null;
      }
    } on ApiException catch (e) {
      _error = e.message;
    } finally {
      notifyListeners();
    }
  }

  Future<void> completeMfaLogin({required String code}) async {
    _error = null;
    try {
      final token = await _auth.completeMfaLogin(
        email: _pendingEmail!,
        password: _pendingPassword!,
        code: code,
      );
      _role = token.role;
      _isLoggedIn = true;
      _requiresMfa = false;
      _pendingEmail = null;
      _pendingPassword = null;
    } on ApiException catch (e) {
      _error = e.message;
    } finally {
      notifyListeners();
    }
  }

  Future<void> logout() async {
    await _auth.logout();
    _isLoggedIn = false;
    _requiresMfa = false;
    _role = null;
    _pendingEmail = null;
    _pendingPassword = null;
    notifyListeners();
  }

  Future<String?> currentToken() async {
    final token = await _auth.currentToken();
    return token?.accessToken;
  }
}
