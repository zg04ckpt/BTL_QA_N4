// ignore_for_file: avoid_print

import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/cupertino.dart';

class UserProvider with ChangeNotifier {
  Future<UserData> getUserById(int id) async {
    try {
      final response = await APIService.instance.request(
        '/api/User/GetUserById?id=$id',  // URL cho API
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        final userJson = response.data;
        // Xử lý Address có thể là null
        final userData = UserData.fromJson(userJson);
        return userData;
      } else {
        throw Exception('Failed to load user data');
      }
    } catch (e) {
      throw Exception('Error fetching user data: $e');
    }
  }
}
