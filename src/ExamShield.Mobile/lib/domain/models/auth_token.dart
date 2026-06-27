class AuthToken {
  final String accessToken;
  final String role;
  final bool requiresMfa;

  const AuthToken({
    required this.accessToken,
    required this.role,
    this.requiresMfa = false,
  });

  factory AuthToken.fromJson(Map<String, dynamic> json) => AuthToken(
        accessToken: json['token'] as String? ?? '',
        role: json['role'] as String? ?? '',
        requiresMfa: json['requiresMfa'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() =>
      {'token': accessToken, 'role': role, 'requiresMfa': requiresMfa};
}
