import 'dart:convert';
import 'package:dio/dio.dart';

class CmtML {
  // Hàm kiểm tra bình luận
  static Future<String> checkComment(String comment) async {
    final Dio dio = Dio();

    var headers = {
      'Content-Type': 'application/json',
    };

    var data = json.encode({
      "comment": comment,
    });

    try {
      var response = await dio.post(
        'http://192.168.1.11:5000/predict', // Thay bằng API endpoint thực tế của bạn
        options: Options(
          headers: headers,
        ),
        data: data,
      );

      if (response.statusCode == 200) {
        // Giả sử response.data là một Map
        var responseData = response.data;

        // Lấy giá trị của predicted_class
        String predictedClass = responseData['predicted_class'];

        return predictedClass;
      } else {
        // Nếu có lỗi từ API, in ra message lỗi
        print('Error: ${response.statusMessage}');
        return "";
      }
    } catch (e) {
      // Xử lý lỗi trong quá trình gọi API
      print('Error occurred: $e');
      return "";
    }
  }
}
