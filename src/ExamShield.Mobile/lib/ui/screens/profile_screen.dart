import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../domain/services/auth_service.dart';
import '../../infrastructure/api/api_client.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  UserProfile? _profile;
  bool _loading = true;
  bool _loggingOut = false;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    try {
      final auth = context.read<AuthNotifier>();
      final token = await auth.currentToken();
      if (token == null || !mounted) return;
      final api = context.read<ApiClient>();
      final profile = await api.getProfile(token);
      if (mounted) setState(() => _profile = profile);
    } catch (_) {
      // Profile load failures are non-fatal — show cached role from AuthNotifier.
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _logout() async {
    setState(() => _loggingOut = true);
    try {
      final auth = context.read<AuthNotifier>();
      final token = await auth.currentToken();
      if (token != null) {
        final api = context.read<ApiClient>();
        await api.logoutApi(token);
      }
      if (!mounted) return;
      await auth.logout();
    } finally {
      if (mounted) setState(() => _loggingOut = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthNotifier>();
    final displayRole = _profile?.role ?? auth.role ?? '—';
    final email = _profile?.email ?? '—';
    final mfaEnabled = _profile?.mfaEnabled ?? false;

    return Scaffold(
      backgroundColor: const Color(0xFF0D1117),
      appBar: AppBar(
        backgroundColor: const Color(0xFF161B22),
        title: const Text('Profile', style: TextStyle(color: Colors.white)),
        automaticallyImplyLeading: false,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF00BFFF)))
          : ListView(
              padding: const EdgeInsets.all(24),
              children: [
                _buildAvatar(email),
                const SizedBox(height: 24),
                _buildInfoCard(email: email, role: displayRole, mfaEnabled: mfaEnabled),
                const SizedBox(height: 16),
                _buildSecurityCard(mfaEnabled: mfaEnabled),
                const SizedBox(height: 32),
                _buildLogoutButton(),
              ],
            ),
    );
  }

  Widget _buildAvatar(String email) {
    final initials = email.isNotEmpty ? email[0].toUpperCase() : '?';
    return Center(
      child: Column(
        children: [
          CircleAvatar(
            radius: 40,
            backgroundColor: const Color(0xFF00BFFF22),
            child: Text(
              initials,
              style: const TextStyle(
                  color: Color(0xFF00BFFF),
                  fontSize: 32,
                  fontWeight: FontWeight.bold),
            ),
          ),
          const SizedBox(height: 12),
          Text(email,
              style: const TextStyle(
                  color: Colors.white, fontSize: 16, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }

  Widget _buildInfoCard({
    required String email,
    required String role,
    required bool mfaEnabled,
  }) {
    return _card(children: [
      const _SectionTitle('Account'),
      _row('Email', email, Icons.email_outlined),
      const Divider(color: Color(0xFF30363D), height: 1),
      _row('Role', role, Icons.badge_outlined),
    ]);
  }

  Widget _buildSecurityCard({required bool mfaEnabled}) {
    return _card(children: [
      const _SectionTitle('Security'),
      _row(
        'Two-Factor Auth',
        mfaEnabled ? 'Enabled' : 'Not enabled',
        mfaEnabled ? Icons.verified_user_outlined : Icons.security_outlined,
        trailingColor: mfaEnabled ? const Color(0xFF00FF88) : const Color(0xFFFFAA00),
      ),
    ]);
  }

  Widget _buildLogoutButton() {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        onPressed: _loggingOut ? null : _logout,
        icon: _loggingOut
            ? const SizedBox(
                height: 16,
                width: 16,
                child: CircularProgressIndicator(
                    strokeWidth: 2, color: Color(0xFFFF6B6B)))
            : const Icon(Icons.logout, color: Color(0xFFFF6B6B)),
        label: Text(
          _loggingOut ? 'Signing out…' : 'Sign Out',
          style: const TextStyle(color: Color(0xFFFF6B6B)),
        ),
        style: OutlinedButton.styleFrom(
          side: const BorderSide(color: Color(0xFFFF6B6B40)),
          padding: const EdgeInsets.symmetric(vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
      ),
    );
  }

  Widget _card({required List<Widget> children}) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF161B22),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFF30363D)),
      ),
      child: Column(children: children),
    );
  }

  Widget _row(String label, String value, IconData icon, {Color? trailingColor}) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        children: [
          Icon(icon, color: const Color(0xFF8B949E), size: 18),
          const SizedBox(width: 12),
          Text(label, style: const TextStyle(color: Color(0xFF8B949E), fontSize: 13)),
          const Spacer(),
          Text(value,
              style: TextStyle(
                  color: trailingColor ?? Colors.white,
                  fontSize: 13,
                  fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String title;
  const _SectionTitle(this.title);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Text(title,
          style: const TextStyle(
              color: Color(0xFF8B949E),
              fontSize: 11,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.8)),
    );
  }
}
