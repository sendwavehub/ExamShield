import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../domain/services/capture_service.dart';
import '../../infrastructure/queue/offline_queue.dart';

class UploadScreen extends StatefulWidget {
  const UploadScreen({super.key});

  @override
  State<UploadScreen> createState() => _UploadScreenState();
}

class _UploadScreenState extends State<UploadScreen> {
  List<PendingCapture> _items = [];
  final Set<String> _inProgress = {};
  bool _syncingAll = false;
  String? _lastResult;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    final items = await context.read<CaptureService>().offlineQueue.listPending();
    if (mounted) setState(() => _items = items);
  }

  Future<void> _retryOne(PendingCapture capture) async {
    setState(() => _inProgress.add(capture.captureId));
    await context.read<CaptureService>().retrySingle(capture);
    if (mounted) setState(() => _inProgress.remove(capture.captureId));
    await _refresh();
  }

  Future<void> _cancelOne(PendingCapture capture) async {
    await context.read<CaptureService>().offlineQueue.markDone(capture.captureId);
    await _refresh();
  }

  Future<void> _syncAll() async {
    setState(() { _syncingAll = true; _lastResult = null; });
    final r = await context.read<CaptureService>().syncPendingUploads();
    if (mounted) {
      setState(() {
        _lastResult = '↑ ${r.uploaded} uploaded · ✗ ${r.failed} failed';
        _syncingAll = false;
      });
    }
    await _refresh();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: const Color(0xFF0D1117),
        appBar: AppBar(
          title: Text(_items.isEmpty ? 'Uploads' : 'Uploads (${_items.length})'),
          backgroundColor: const Color(0xFF161B22),
          foregroundColor: Colors.white,
        ),
        body: _items.isEmpty ? _buildEmpty() : _buildList(),
      );

  Widget _buildEmpty() => const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.cloud_done_outlined, size: 64, color: Color(0xFF3FB950)),
            SizedBox(height: 16),
            Text(
              'All uploads complete',
              style: TextStyle(color: Colors.white, fontSize: 18),
            ),
          ],
        ),
      );

  Widget _buildList() => Column(
        children: [
          Expanded(
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: _items.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (_, i) => _buildCard(_items[i]),
            ),
          ),
          _buildSyncBar(),
        ],
      );

  Widget _buildCard(PendingCapture capture) {
    final busy = _inProgress.contains(capture.captureId);
    final hasFailed = capture.retryCount > 0;
    final shortId = capture.captureId.length > 8
        ? '${capture.captureId.substring(0, 8)}…'
        : capture.captureId;
    final sizeKb = (capture.imageBytes.lengthInBytes / 1024).toStringAsFixed(1);
    final subtitle = hasFailed ? '$sizeKb KB · ${capture.retryCount} retries' : '$sizeKb KB';

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: const Color(0xFF161B22),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
            color: hasFailed ? const Color(0xFFFF6B6B) : const Color(0xFF30363D)),
      ),
      child: Row(
        children: [
          busy
              ? const SizedBox(
                  width: 24,
                  height: 24,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: Color(0xFF00BFFF)),
                )
              : Icon(
                  hasFailed ? Icons.error_outline : Icons.cloud_upload_outlined,
                  color: hasFailed
                      ? const Color(0xFFFF6B6B)
                      : const Color(0xFF8B949E),
                  size: 24,
                ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(shortId,
                    style: const TextStyle(
                        color: Colors.white, fontWeight: FontWeight.w600)),
                const SizedBox(height: 2),
                Text(subtitle,
                    style: const TextStyle(
                        color: Color(0xFF8B949E), fontSize: 12)),
              ],
            ),
          ),
          if (!busy) ...[
            TextButton(
              onPressed: () => _retryOne(capture),
              child: const Text('Retry',
                  style: TextStyle(color: Color(0xFF00BFFF))),
            ),
            TextButton(
              onPressed: () => _cancelOne(capture),
              child: const Text('Cancel',
                  style: TextStyle(color: Color(0xFFFF6B6B))),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildSyncBar() => Container(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 20),
        color: const Color(0xFF161B22),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (_lastResult != null) ...[
              Text(_lastResult!,
                  style: const TextStyle(
                      color: Color(0xFF00BFFF), fontSize: 13)),
              const SizedBox(height: 8),
            ],
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: _syncingAll ? null : _syncAll,
                icon: _syncingAll
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.black),
                      )
                    : const Icon(Icons.sync),
                label: Text(_syncingAll ? 'Syncing…' : 'Sync All'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF00BFFF),
                  foregroundColor: Colors.black,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8)),
                ),
              ),
            ),
          ],
        ),
      );
}
