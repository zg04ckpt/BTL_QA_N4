import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

enum DioMethod { post, get, put, delete }

class APIService {
  APIService._singleton();

  static final APIService instance = APIService._singleton();

  String get baseUrl {
    if (kDebugMode) {
      return 'https://qa-test.hoangcn.com';
    }
    return 'https://qa-test.hoangcn.com';
  }

  // Lưu token vào SharedPreferences
  Future<void> saveToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('accessToken', token);
  }

  // Lấy token từ SharedPreferences
  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('accessToken');
  }

  // Hàm request với token được lấy từ SharedPreferences
  Future<Response> request(
    String endpoint,
    DioMethod method, {
    Map<String, dynamic>? param,
    String? contentType,
    formData,
  }) async {
    try {
      final token = await getToken(); // Lấy token từ SharedPreferences
      final dio = Dio(
        BaseOptions(
          baseUrl: baseUrl,
          method: method.name,
          contentType: contentType ?? 'application/json',
          headers: {
            HttpHeaders.authorizationHeader: token != null
                ? 'Bearer $token'
                : '', // Thêm header Authorization nếu token tồn tại
          },
        ),
      );
      (dio.httpClientAdapter as IOHttpClientAdapter).onHttpClientCreate =
          (HttpClient client) {
        client.badCertificateCallback =
            (X509Certificate cert, String host, int port) => true;
        return client;
      };
      switch (method) {
        case DioMethod.post:
          return dio.post(
            endpoint,
            data: param ?? formData,
          );
        case DioMethod.get:
          return dio.get(
            endpoint,
            queryParameters: param,
          );
        case DioMethod.put:
          return dio.put(
            endpoint,
            data: param ?? formData,
          );
        case DioMethod.delete:
          return dio.delete(
            endpoint,
            data: param ?? formData,
          );
        default:
          return dio.post(
            endpoint,
            data: param ?? formData,
          );
      }
    } catch (e) {
      throw Exception('Network error');
    }
  }

  Future<Response> uploadImage(File file) async {
    try {
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          file.path,
          filename: file.path.split('/').last,
          contentType: DioMediaType('image', 'png'), // Nếu cần chỉ định type
        ),
      });

      final response = await request(
        '/api/Photo/upload-image',
        DioMethod.post,
        contentType: 'multipart/form-data',
        formData: formData,
      );

      return response;
    } catch (e) {
      throw Exception('Upload image error: $e');
    }
  }
  
}
