import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

import 'package:cp_restaurants/data/models/restaurant.dart';

class RestaurantHelper {
  static final RestaurantHelper _instance = RestaurantHelper._internal();
  factory RestaurantHelper() => _instance;
  static Database? _database;

  RestaurantHelper._internal();

  Future<Database> get database async {
    if (_database != null) return _database!;
    _database = await _initDatabase();
    return _database!;
  }

  Future<Database> _initDatabase() async {
    final dbPath = await getDatabasesPath();
    final path = join(dbPath, 'user_data_2.db');

    return openDatabase(
      path,
      version: 1,
      onCreate: (db, version) async {
        await db.execute('''
          CREATE TABLE RestaurantTable(
            id INTEGER PRIMARY KEY,
            name TEXT,
            status INTEGER,
            email TEXT,
            description TEXT, -- Thêm cột này
            phoneNumber TEXT,
            avtImage TEXT,
            cateId INTEGER,
            userId INTEGER,
            address TEXT, -- JSON string
            photoUrls TEXT, -- Comma-separated string
            averageScore REAL,
            totalReviews INTEGER,
            distance REAL,
            category TEXT
          )
        ''');
      },
    );
  }

  Future<void> close() async {
    final db = await database;
    db.close();
  }

  
  Future<void> insertRestaurant(Restaurant restaurant) async {
    final db = await database;
    await db.insert(
      'RestaurantTable',
      restaurant.toDB(),
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  
  Future<List<Restaurant>> fetchAllRestaurants() async {
    final db = await database;
    final maps = await db.query('RestaurantTable');

    if (maps.isEmpty) {
      return [];
    }

    return maps.map((map) => Restaurant.fromDB(map)).toList();
  }

  
  Future<void> deleteRestaurant(int id) async {
    final db = await database;
    await db.delete(
      'RestaurantTable',
      where: 'id = ?',
      whereArgs: [id],
    );
  }

  
  Future<void> clearAllRestaurants() async {
    final db = await database;
    await db.delete('RestaurantTable');
  }
}
