import 'dart:io';
import 'package:cp_restaurants/network/api_util.dart';

class ImageService {
  static Future<String> uploadImage(File imageFile) async {
    try {
       var response = await APIService.instance.uploadImage(imageFile);
      var responseData = response.data;

      // Lấy giá trị của predicted_class
      String path = responseData['image'] ?? responseData['Image'] ?? "";
      return path;
    } catch (e) {
      print("Failed to upload image: $e");
      rethrow;
    }
  }
}
