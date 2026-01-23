import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:geolocator/geolocator.dart';

class GlobalData {
  GlobalData._privateConstructor();

  static final GlobalData instance = GlobalData._privateConstructor();

  bool isLogin = false;

  User? user;

  UserData? userData;

  Position? userPosition ;

  Future fetchUserData(String userId) async {
    userData = await getUserById(userId);
  }

  Future<UserData> getUserById(String id) async {
    // try {
      final response = await APIService.instance.request(
        '/api/User/GetUserById?id=$id', // URL cho API
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
    // } catch (e) {
    //   throw Exception('Error fetching user data: $e');
    // }
  }

  
}
