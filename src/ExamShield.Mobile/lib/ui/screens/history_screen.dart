import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../domain/services/capture_service.dart';
import '../../domain/models/capture.dart';

const _statusColor = {
  CaptureStatus.verified: Color(0xFF3FB950),
  CaptureStatus.uploaded: Color(0xFF00BFFF),
  CaptureStatus.created: Color(0xFFD29922),
  CaptureStatus.tampered: Color(0xFFFF6B6B),
};

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({super.key});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  late Future<List<CaptureRecord>> _future;

  @override
  void initState() {
    super.initState();
    _future = context.read<CaptureService>().myCaptures();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0D1117),
      appBar: AppBar(
        title: const Text('Capture History'),
        backgroundColor: const Color(0xFF161B22),
        foregroundColor: Colors.white,
      ),
      body: FutureBuilder<List<CaptureRecord>>(
        future: _future,
        builder: (ctx, snap) {
          if (snap.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return Center(
              child: Text('Error: ${snap.error}',
                  style: const TextStyle(color: Color(0xFFFF6B6B))),
            );
          }
          final captures = snap.data ?? [];
          if (captures.isEmpty) {
            return const Center(
              child: Text('No captures yet.',
                  style: TextStyle(color: Color(0xFF8B949E))),
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: captures.length,
            separatorBuilder: (_, __) => const SizedBox(height: 8),
            itemBuilder: (_, i) {
              final c = captures[i];
              final color = _statusColor[c.status] ?? Colors.grey;
              return Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: const Color(0xFF161B22),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: const Color(0xFF30363D)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          c.captureId.substring(0, 8).toUpperCase(),
                          style: const TextStyle(
                              color: Colors.white, fontWeight: FontWeight.bold,
                              fontFamily: 'monospace'),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                          decoration: BoxDecoration(
                            color: color.withOpacity(0.15),
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: color.withOpacity(0.4)),
                          ),
                          child: Text(
                            c.status.name.toUpperCase(),
                            style: TextStyle(color: color, fontSize: 11,
                                fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Exam: ${c.examId}',
                      style: const TextStyle(color: Color(0xFF8B949E), fontSize: 12),
                    ),
                    Text(
                      c.capturedAt.toLocal().toString().substring(0, 19),
                      style: const TextStyle(color: Color(0xFF8B949E), fontSize: 12),
                    ),
                  ],
                ),
              );
            },
          );
        },
      ),
    );
  }
}
