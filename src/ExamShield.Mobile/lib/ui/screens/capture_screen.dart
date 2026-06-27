import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:camera/camera.dart';
import 'package:provider/provider.dart';
import '../../domain/services/capture_service.dart';

class CaptureScreen extends StatefulWidget {
  const CaptureScreen({super.key});

  @override
  State<CaptureScreen> createState() => _CaptureScreenState();
}

class _CaptureScreenState extends State<CaptureScreen> {
  CameraController? _controller;
  bool _capturing = false;
  String? _status;
  List<CameraDescription>? _cameras;

  @override
  void initState() {
    super.initState();
    _initCamera();
  }

  Future<void> _initCamera() async {
    final cameras = await availableCameras();
    if (cameras.isEmpty) return;
    _cameras = cameras;
    _controller = CameraController(
      cameras.first,
      ResolutionPreset.high,
      enableAudio: false,
    );
    await _controller!.initialize();
    if (mounted) setState(() {});
  }

  @override
  void dispose() {
    _controller?.dispose();
    super.dispose();
  }

  Future<void> _capture() async {
    if (_controller == null || !_controller!.value.isInitialized || _capturing) return;
    setState(() {
      _capturing = true;
      _status = 'Capturing…';
    });
    try {
      final file = await _controller!.takePicture();
      final bytes = await file.readAsBytes();
      setState(() => _status = 'Hashing & signing…');
      final service = context.read<CaptureService>();
      final captureId = await service.hashSignAndRegister(Uint8List.fromList(bytes));
      setState(() => _status = 'Uploading…');
      await service.uploadWithFallback(captureId, Uint8List.fromList(bytes));
      final pending = await service.offlineQueue.pendingCount();
      setState(() => _status = pending > 0
          ? 'Queued offline — $pending pending upload(s)'
          : 'Done — capture ID: $captureId');
    } catch (e) {
      setState(() => _status = 'Error: $e');
    } finally {
      setState(() => _capturing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0D1117),
      appBar: AppBar(
        title: const Text('Capture Answer Sheet'),
        backgroundColor: const Color(0xFF161B22),
        foregroundColor: Colors.white,
      ),
      body: Column(
        children: [
          Expanded(
            child: _controller?.value.isInitialized == true
                ? CameraPreview(_controller!)
                : const Center(
                    child: Text('Initializing camera…',
                        style: TextStyle(color: Colors.white54)),
                  ),
          ),
          if (_status != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: const Color(0xFF161B22),
              child: Text(
                _status!,
                style: const TextStyle(color: Color(0xFF00BFFF), fontSize: 13),
                textAlign: TextAlign.center,
              ),
            ),
          Padding(
            padding: const EdgeInsets.all(24),
            child: ElevatedButton.icon(
              onPressed: _capturing ? null : _capture,
              icon: _capturing
                  ? const SizedBox(
                      width: 18, height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.black),
                    )
                  : const Icon(Icons.camera),
              label: Text(_capturing ? 'Processing…' : 'Capture'),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF00BFFF),
                foregroundColor: Colors.black,
                minimumSize: const Size(double.infinity, 52),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
