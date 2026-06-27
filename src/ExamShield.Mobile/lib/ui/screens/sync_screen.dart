import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../domain/services/capture_service.dart';

class SyncScreen extends StatefulWidget {
  const SyncScreen({super.key});

  @override
  State<SyncScreen> createState() => _SyncScreenState();
}

class _SyncScreenState extends State<SyncScreen> {
  int _pendingCount = 0;
  bool _syncing = false;
  String? _result;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    final count = await context.read<CaptureService>().offlineQueue.pendingCount();
    if (mounted) setState(() => _pendingCount = count);
  }

  Future<void> _sync() async {
    setState(() { _syncing = true; _result = null; });
    try {
      final result = await context.read<CaptureService>().syncPendingUploads();
      if (mounted) {
        setState(() => _result = '↑ ${result.uploaded} uploaded · ✗ ${result.failed} failed');
        await _refresh();
      }
    } finally {
      if (mounted) setState(() => _syncing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0D1117),
      appBar: AppBar(
        title: const Text('Offline Sync'),
        backgroundColor: const Color(0xFF161B22),
        foregroundColor: Colors.white,
      ),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF161B22),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFF30363D)),
              ),
              child: Row(
                children: [
                  Icon(
                    _pendingCount > 0 ? Icons.cloud_upload_outlined : Icons.cloud_done_outlined,
                    color: _pendingCount > 0 ? const Color(0xFFFFAA00) : const Color(0xFF3FB950),
                    size: 32,
                  ),
                  const SizedBox(width: 16),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '$_pendingCount pending upload${_pendingCount == 1 ? '' : 's'}',
                        style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                      ),
                      Text(
                        _pendingCount > 0 ? 'Captured offline — tap Sync Now' : 'All captures uploaded',
                        style: const TextStyle(color: Color(0xFF8B949E), fontSize: 13),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            if (_result != null) ...[
              const SizedBox(height: 16),
              Text(_result!, style: const TextStyle(color: Color(0xFF00BFFF), fontSize: 14)),
            ],
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: _pendingCount == 0 || _syncing ? null : _sync,
                icon: _syncing
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.black))
                    : const Icon(Icons.sync),
                label: Text(_syncing ? 'Syncing…' : 'Sync Now'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF00BFFF),
                  foregroundColor: Colors.black,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
