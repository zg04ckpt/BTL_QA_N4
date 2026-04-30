// ignore_for_file: avoid_print
import 'package:cp_restaurants/common/login_session_log.dart';
import 'package:cp_restaurants/data/models/address.dart';
import 'package:dio/dio.dart';
import 'package:flutter/cupertino.dart';

import '../../network/api_util.dart';

/// Đọc JWT từ body đăng nhập thành công.
///
/// Backend [UserController.Login] khi HTTP 200 trả:
/// `{ "success": true, "message": "Login successfully", "data": { "token": "<jwt>" } }`.
/// Token **chỉ** nằm trong `data.token`, không có `token` ở root.
String? _readLoginTokenFromResponse(dynamic data) {
  if (data == null || data is! Map) return null;
  final map = Map<String, dynamic>.from(data);

  final ok = map['success'];
  if (ok != null && ok != true) {
    loginSessionLog(
      'AuthViewModel: login body có success=$ok (không phải true), bỏ qua token',
    );
    return null;
  }

  final payload = map['data'];
  if (payload is Map) {
    final inner = Map<String, dynamic>.from(payload);
    final t = inner['token'];
    if (t != null && t.toString().trim().isNotEmpty) {
      return t.toString();
    }
  }

  // Tương thích nếu có proxy/client cũ đặt token ở root (không phải contract backend hiện tại).
  final root = map['token'];
  if (root != null && root.toString().trim().isNotEmpty) {
    loginSessionLog(
      'AuthViewModel: dùng token ở root (không khớp API UserController)',
    );
    return root.toString();
  }

  return null;
}

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
        final data = response.data;
        final token = _readLoginTokenFromResponse(data);
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
          loginSessionLog(
            'AuthViewModel.login: không đọc được token. status=200, bodyType=${data.runtimeType}, body=$data',
          );
          throw Exception('Không tìm thấy token đăng nhập trong phản hồi.');
        }
      } else {
        throw Exception('Đăng nhập thất bại.');
      }
    } catch (e) {
      if (e is DioException && onError != null) {
        final rd = e.response?.data;
        String message = 'Đã có lỗi xảy ra, vui lòng thử lại';
        if (rd is Map) {
          final m = Map<String, dynamic>.from(rd);
          message = m['message']?.toString() ??
              m['Message']?.toString() ??
              message;
        }
        loginSessionLog(
          'AuthViewModel.login DioException status=${e.response?.statusCode} message=$message',
        );
        onError(message);
      } else {
        onError?.call(
            e.toString().replaceFirst('Exception: ', '').trim().isEmpty
                ? 'Network error'
                : e.toString().replaceFirst('Exception: ', '').trim());
      }
    }
  }
}
