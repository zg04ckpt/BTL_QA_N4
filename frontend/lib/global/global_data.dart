import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:dio/dio.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';

class GlobalData {
  GlobalData._privateConstructor();

  static final GlobalData instance = GlobalData._privateConstructor();

  bool isLogin = false;

  User? user;

  UserData? userData;

  Position? userPosition;

  Future<void> fetchUserData(String userId) async {
    userData = await getUserById(userId);
  }

  /// GET api/User/GetUserById?id=
  Future<UserData?> getUserById(String userId) async {
    final idNum = int.tryParse(userId.trim());
    if (idNum == null) {
      debugPrint('getUserById: invalid id "$userId"');
      return null;
    }

    try {
      final response = await APIService.instance.request(
        '/api/User/GetUserById',
        DioMethod.get,
        param: {'id': idNum},
      );

      if (response.statusCode == 204) {
        return null;
      }
      if (response.statusCode != 200 || response.data == null) {
        return null;
      }

      final raw = response.data;
      if (raw is Map<String, dynamic>) {
        return UserData.fromJson(raw);
      }
      if (raw is Map) {
        return UserData.fromJson(Map<String, dynamic>.from(raw));
      }
      debugPrint('getUserById: unexpected response type ${raw.runtimeType}');
      return null;
    } on DioException catch (e, st) {
      debugPrint('getUserById DioException: $e\n$st');
      return null;
    } catch (e, st) {
      debugPrint('getUserById error: $e\n$st');
      return null;
    }
  }
}
