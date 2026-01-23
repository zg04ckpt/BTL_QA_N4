import 'dart:io';

import 'package:image_picker/image_picker.dart';

class AppPicker {
  static Future<File?> pickImage() async {
    final pickedFile =
        await ImagePicker().pickImage(source: ImageSource.gallery);

    if (pickedFile != null) {
      var image = File(pickedFile.path);
      return image;
    }
    return null;
  }

  static Future<List<File>?> pickImages() async {
    final pickedFiles = await ImagePicker().pickMultiImage();
    List<File> imageFiles = [];
      imageFiles.addAll(pickedFiles.map((pickedFile) => File(pickedFile.path)));
   return imageFiles;
  }
}
