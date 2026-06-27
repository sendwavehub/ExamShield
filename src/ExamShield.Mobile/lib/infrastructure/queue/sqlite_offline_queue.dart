import 'dart:typed_data';
import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';
import 'offline_queue.dart';

class SqliteOfflineQueue implements OfflineQueue {
  static const _db = 'examshield_queue.db';
  static const _table = 'pending_captures';
  static const _maxRetries = 5;

  Database? _database;

  Future<Database> get _db async {
    _database ??= await openDatabase(
      join(await getDatabasesPath(), _db),
      version: 1,
      onCreate: (db, _) => db.execute('''
        CREATE TABLE $_table (
          capture_id TEXT PRIMARY KEY,
          image_bytes BLOB NOT NULL,
          created_at INTEGER NOT NULL,
          retry_count INTEGER NOT NULL DEFAULT 0
        )
      '''),
    );
    return _database!;
  }

  @override
  Future<void> enqueue(PendingCapture capture) async {
    final db = await _db;
    await db.insert(
      _table,
      {
        'capture_id': capture.captureId,
        'image_bytes': capture.imageBytes,
        'created_at': capture.createdAt.millisecondsSinceEpoch,
        'retry_count': capture.retryCount,
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  @override
  Future<List<PendingCapture>> listPending() async {
    final db = await _db;
    final rows = await db.query(
      _table,
      where: 'retry_count < ?',
      whereArgs: [_maxRetries],
      orderBy: 'created_at ASC',
    );
    return rows
        .map((r) => PendingCapture(
              captureId: r['capture_id'] as String,
              imageBytes: r['image_bytes'] as Uint8List,
              createdAt: DateTime.fromMillisecondsSinceEpoch(
                  r['created_at'] as int),
              retryCount: r['retry_count'] as int,
            ))
        .toList();
  }

  @override
  Future<void> markDone(String captureId) async {
    final db = await _db;
    await db.delete(_table, where: 'capture_id = ?', whereArgs: [captureId]);
  }

  @override
  Future<void> incrementRetry(String captureId) async {
    final db = await _db;
    await db.rawUpdate(
      'UPDATE $_table SET retry_count = retry_count + 1 WHERE capture_id = ?',
      [captureId],
    );
  }

  @override
  Future<int> pendingCount() async {
    final db = await _db;
    return Sqflite.firstIntValue(
          await db.rawQuery('SELECT COUNT(*) FROM $_table WHERE retry_count < ?',
              [_maxRetries]),
        ) ??
        0;
  }
}
