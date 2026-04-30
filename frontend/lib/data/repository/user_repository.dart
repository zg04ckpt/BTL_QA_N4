import 'dart:convert';

import 'package:cp_restaurants/common/login_session_log.dart';
import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/data/repository/database_helper.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:sqflite/sqflite.dart';

class UserDataRepository {
  static const String _prefsUserIdKey = 'cp_cached_user_id';
  static const String _prefsUserJsonKey = 'cp_cached_user_json';

  /// Saves session to **SharedPreferences first** (reliable on all platforms), then SQLite.
  static Future<void> saveUserData(UserData userData) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt(_prefsUserIdKey, userData.userId);
    try {
      await prefs.setString(
        _prefsUserJsonKey,
        jsonEncode(userData.toJson()),
      );
    } catch (e, st) {
      debugPrint('UserDataRepository.saveUserData prefs json: $e\n$st');
    }

    try {
      final db = await DatabaseHelper().database;
      await db.delete('UserData');
      await db.insert(
        'UserData',
        userData.toMap(),
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    } catch (e, st) {
      debugPrint('UserDataRepository.saveUserData sqlite: $e\n$st');
    }
  }

  /// Reads SQLite first; if missing or invalid `userId`, restores from SharedPreferences JSON.
  static Future<UserData?> fetchUserData() async {
    try {
      final db = await DatabaseHelper().database;
      final maps = await db.query(
        'UserData',
        orderBy: 'id DESC',
        limit: 1,
      );
      if (maps.isNotEmpty) {
        final fromDb = UserData.fromMap(maps.first);
        if (fromDb.userId > 0) {
          loginSessionLog(
            'UserDataRepository: đọc user từ SQLite userId=${fromDb.userId}',
          );
          return fromDb;
        }
        loginSessionLog(
          'UserDataRepository: SQLite có bản ghi nhưng userId<=0, thử prefs',
        );
      } else {
        loginSessionLog('UserDataRepository: SQLite không có UserData, thử prefs');
      }
    } catch (e, st) {
      loginSessionLog('UserDataRepository.fetchUserData sqlite', e, st);
    }

    final fromPrefs = await _loadUserFromPrefs();
    if (fromPrefs != null) {
      loginSessionLog(
        'UserDataRepository: đọc user từ prefs userId=${fromPrefs.userId}',
      );
    }
    return fromPrefs;
  }

  static Future<UserData?> _loadUserFromPrefs() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_prefsUserJsonKey);
    if (raw == null || raw.isEmpty) return null;
    try {
      final decoded = jsonDecode(raw);
      if (decoded is Map<String, dynamic>) {
        final u = UserData.fromJson(decoded);
        if (u.userId > 0) return u;
      }
    } catch (e, st) {
      debugPrint('UserDataRepository._loadUserFromPrefs: $e\n$st');
    }
    return null;
  }

  /// Removes JWT, prefs backup, and SQLite user row (call when logging out).
  static Future<void> clearSession() async {
    await APIService.instance.clearToken();
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_prefsUserIdKey);
    await prefs.remove(_prefsUserJsonKey);
    try {
      final db = await DatabaseHelper().database;
      await db.delete('UserData');
    } catch (e, st) {
      debugPrint('UserDataRepository.clearSession sqlite: $e\n$st');
    }
  }

  /// Last known user id from prefs (when JWT claims differ from expectations).
  static Future<int?> readCachedUserId() async {
    final prefs = await SharedPreferences.getInstance();
    final id = prefs.getInt(_prefsUserIdKey);
    if (id != null && id > 0) return id;
    return null;
  }
}
