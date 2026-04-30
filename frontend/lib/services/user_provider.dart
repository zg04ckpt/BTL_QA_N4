// ignore_for_file: avoid_print

import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/cupertino.dart';

class UserProvider with ChangeNotifier {
  Future<UserData?> getUserById(int id) async {
    try {
      final response = await APIService.instance.request(
        '/api/User/GetUserById?id=$id',
        DioMethod.get,
      );

      if (response.statusCode == 200 && response.data != null) {
        final raw = response.data;
        if (raw is! Map) {
          return null;
        }
        final map = Map<String, dynamic>.from(raw as Map);
        final payload = map['data'] is Map<String, dynamic>
            ? Map<String, dynamic>.from(map['data'] as Map)
            : map;
        return UserData.fromJson(payload);
      }
      return null;
    } catch (_) {
      return null;
    }
  }
}
