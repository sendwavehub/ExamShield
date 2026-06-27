import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../domain/services/device_registration_service.dart';
import '../../domain/services/auth_service.dart';
import '../../infrastructure/api/api_client.dart';

class DeviceRegistrationScreen extends StatefulWidget {
  const DeviceRegistrationScreen({super.key});

  @override
  State<DeviceRegistrationScreen> createState() => _DeviceRegistrationScreenState();
}

class _DeviceRegistrationScreenState extends State<DeviceRegistrationScreen> {
  final _form = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  bool _loading = true;
  bool _registered = false;
  String? _registeredId;
  String? _error;

  @override
  void initState() {
    super.initState();
    _checkRegistration();
  }

  Future<void> _checkRegistration() async {
    final isReg = await context.read<DeviceRegistrationService>().isRegistered();
    if (mounted) setState(() { _registered = isReg; _loading = false; });
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    super.dispose();
  }

  Future<void> _register() async {
    if (!_form.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final token = await context.read<AuthNotifier>().currentToken();
      final id = await context.read<DeviceRegistrationService>().register(
            name: _nameCtrl.text.trim(),
            token: token ?? '',
          );
      if (mounted) {
        setState(() { _registered = true; _registeredId = id; });
      }
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0D1117),
      appBar: AppBar(
        title: const Text('Device Registration'),
        backgroundColor: const Color(0xFF161B22),
        foregroundColor: Colors.white,
      ),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _registered
                ? _SuccessView(deviceId: _registeredId)
                : _RegistrationForm(
                    formKey: _form,
                    nameCtrl: _nameCtrl,
                    error: _error,
                    onSubmit: _register,
                  ),
      ),
    );
  }
}

class _SuccessView extends StatelessWidget {
  final String? deviceId;
  const _SuccessView({this.deviceId});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.verified_rounded, size: 64, color: Color(0xFF3FB950)),
          const SizedBox(height: 16),
          const Text(
            'Device already registered',
            style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold),
          ),
          if (deviceId != null) ...[
            const SizedBox(height: 8),
            Text(
              deviceId!,
              style: const TextStyle(color: Color(0xFF8B949E), fontSize: 12, fontFamily: 'monospace'),
            ),
          ],
        ],
      ),
    );
  }
}

class _RegistrationForm extends StatelessWidget {
  final GlobalKey<FormState> formKey;
  final TextEditingController nameCtrl;
  final String? error;
  final VoidCallback onSubmit;

  const _RegistrationForm({
    required this.formKey,
    required this.nameCtrl,
    required this.error,
    required this.onSubmit,
  });

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Register this device to enable signed captures.',
            style: TextStyle(color: Color(0xFF8B949E), fontSize: 14),
          ),
          const SizedBox(height: 24),
          TextFormField(
            controller: nameCtrl,
            style: const TextStyle(color: Colors.white),
            decoration: InputDecoration(
              labelText: 'Device Name',
              labelStyle: const TextStyle(color: Color(0xFF8B949E)),
              filled: true,
              fillColor: const Color(0xFF161B22),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
            ),
            validator: (v) => (v == null || v.trim().isEmpty) ? 'Device name is required' : null,
          ),
          if (error != null) ...[
            const SizedBox(height: 12),
            Text(error!, style: const TextStyle(color: Color(0xFFFF6B6B), fontSize: 13)),
          ],
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: onSubmit,
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF00BFFF),
                foregroundColor: Colors.black,
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
              ),
              child: const Text('Register Device', style: TextStyle(fontWeight: FontWeight.bold)),
            ),
          ),
        ],
      ),
    );
  }
}
