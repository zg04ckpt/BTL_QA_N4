import 'dart:async';
import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';


class DatabaseHelper {
  static final DatabaseHelper _instance = DatabaseHelper._internal();
  factory DatabaseHelper() => _instance;
  static Database? _database;

  DatabaseHelper._internal();

  Future<Database> get database async {
    if (_database != null) return _database!;
    _database = await _initDatabase();
    return _database!;
  }

  Future<Database> _initDatabase() async {
    final dbPath = await getDatabasesPath();
    final path = join(dbPath, 'user_data.db');

    return openDatabase(
      path,
      version: 1,
      onCreate: (db, version) async {
        await db.execute('''
          CREATE TABLE UserData(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            userId INTEGER,
            email TEXT,
            phoneNumber TEXT,
            name TEXT,
            role TEXT,
            avtImage TEXT,
            status INTEGER,
            address TEXT,
            restaurantId TEXT,
            reports TEXT,
            reviews TEXT,
            restaurants TEXT
          )
        ''');
      },
    );
  }

  Future<void> close() async {
    final db = await database;
    db.close();
  }
}
