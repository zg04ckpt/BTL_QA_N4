import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:shared_preferences/shared_preferences.dart';

enum DioMethod { post, get, put, delete }

class APIService {
  APIService._singleton();

  static final APIService instance = APIService._singleton();

  String get baseUrl => 'https://qa-test.hoangcn.com';

  Future<void> saveToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('accessToken', token);
  }

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('accessToken');
  }

  /// GET/POST JSON hoặc multipart. Không nuốt [DioException] — để caller xử lý đúng như Postman.
  Future<Response> request(
    String endpoint,
    DioMethod method, {
    Map<String, dynamic>? param,
    String? contentType,
    dynamic formData,
  }) async {
    final token = await getToken();
    final headers = <String, dynamic>{};
    final t = token?.trim();
    if (t != null && t.isNotEmpty) {
      headers[HttpHeaders.authorizationHeader] = 'Bearer $t';
    }

    final dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: const Duration(seconds: 45),
        receiveTimeout: const Duration(seconds: 45),
        headers: headers,
      ),
    );

    (dio.httpClientAdapter as IOHttpClientAdapter).onHttpClientCreate =
        (HttpClient client) {
      client.badCertificateCallback =
          (X509Certificate cert, String host, int port) => true;
      return client;
    };

    final isMultipart = formData != null && formData is FormData;
    final options = Options(
      contentType: isMultipart
          ? null
          : (contentType ?? Headers.jsonContentType),
    );

    switch (method) {
      case DioMethod.post:
        return dio.post(
          endpoint,
          data: param ?? formData,
          options: options,
        );
      case DioMethod.get:
        return dio.get(
          endpoint,
          queryParameters: param,
          options: options,
        );
      case DioMethod.put:
        return dio.put(
          endpoint,
          data: param ?? formData,
          options: options,
        );
      case DioMethod.delete:
        return dio.delete(
          endpoint,
          data: param ?? formData,
          options: options,
        );
    }
  }

  Future<Response> uploadImage(File file) async {
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(
        file.path,
        filename: file.path.split(RegExp(r'[/\\]')).last,
      ),
    });

    return request(
      '/api/Photo/upload-image',
      DioMethod.post,
      formData: formData,
    );
  }
}
