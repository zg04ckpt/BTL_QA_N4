import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/data/repository/database_helper.dart';
import 'package:sqflite/sqflite.dart';

class UserDataRepository {
  static Future<void> saveUserData(UserData userData) async {
    final db = await DatabaseHelper().database;
    await db.insert(
      'UserData',
      userData.toMap(),
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  static Future<UserData?> fetchUserData() async {
    final db = await DatabaseHelper().database;

    // Lấy tất cả dữ liệu (dự kiến chỉ có một bản ghi)
    final List<Map<String, dynamic>> maps = await db.query('UserData');

    if (maps.isNotEmpty) {
      return UserData.fromMap(maps.first); // Lấy bản ghi đầu tiên
    }
    return null; // Trả về null nếu không có dữ liệu
  }
}
