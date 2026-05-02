import 'dart:developer';

import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/data/repository/database_helper.dart';
import 'package:sqflite/sqflite.dart';

class UserDataRepository {
  static Future<void> saveUserData(UserData userData) async {
    final db = await DatabaseHelper().database;
    await db.delete('UserData');
    await db.insert(
      'UserData',
      userData.toMap(),
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  static Future<UserData?> fetchUserData() async {
    try {
      final db = await DatabaseHelper().database;
      final List<Map<String, dynamic>> maps = await db.query('UserData');
      if (maps.isEmpty) return null;
      return UserData.fromMap(maps.first);
    } catch (e, st) {
      log('fetchUserData failed (corrupt row or DB): $e', stackTrace: st);
      return null;
    }
  }
}
