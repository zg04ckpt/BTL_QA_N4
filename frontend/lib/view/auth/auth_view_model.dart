// ignore_for_file: avoid_print


import 'package:cp_restaurants/data/models/address.dart';
import 'package:dio/dio.dart';
import 'package:flutter/cupertino.dart';

import '../../network/api_util.dart';

class AuthViewModel with ChangeNotifier {
  Future<void> register({
    required String email,
    required String password,
    required String name,
    required String phoneNumber,
    required Address address,
    Function(Response)? onSuccess,
    Function(String)? onError,
  }) async {
    const String url = '/api/User/register'; // Endpoint đăng ký
    final body = {
      "email": email,
      "password": password,
      "name": name,
      "phoneNumber": phoneNumber,
      "address":{
        "city": address.city,
        "district":address.district,
        "ward":address.ward,
        "detail":address.detail,
        "lon":address.lon,
        "lat":address.lat
      }
    };

    try {
      final response = await APIService.instance.request(
        url,
        DioMethod.post,
        param: body,
      );
      if (onSuccess != null) {
        onSuccess(response); // Gọi callback nếu thành công
      }
    } catch (e) {
      if (e is DioException && onError != null) {
        final message = e.response?.data['message'] ?? 'Đã có lỗi xảy ra, vui lòng thử lại';
        onError(message); // Gọi callback nếu thất bại
      } else {
        onError?.call('Network error'); // Lỗi mạng không xác định
      }
    }
  }

  Future<void> login({
    required String email,
    required String password,
    Function(String)? onSuccess,
    Function(String)? onError,
  }) async {
    const String url = '/api/User/login'; // Endpoint đăng nhập
    final body = {
      "email": email,
      "password": password,
    };

    try {
      // Gửi yêu cầu đăng nhập
      final response = await APIService.instance.request(
        url,
        DioMethod.post,
        param: body,
      );

      // Kiểm tra và lưu token vào SharedPreferences nếu đăng nhập thành công
      if (response.statusCode == 200) {
        final token = response.data['token'];
        if (token != null) {
          // String getJsonFromJWT(String splittedToken) {
           
          //   return utf8.decode(base64Url.decode(normalizedSource));
          // }

          await APIService.instance
              .saveToken(token); // Lưu token vào SharedPreferences
          if (onSuccess != null) {
            onSuccess(token); // Gọi callback thành công
          }
        } else {
          throw Exception('Token not found in response');
        }
      } else {
        throw Exception('Login failed');
      }
    } catch (e) {
      if (e is DioException && onError != null) {
        final message = e.response?.data['message'] ?? ' Đã có lỗi xảy ra, vui lòng thử lại';
        onError(message); // Gọi callback nếu có lỗi
      } else {
        onError?.call('Network error');
      }
    }
  }
}
