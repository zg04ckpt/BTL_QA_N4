import 'dart:io';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/image_service.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

class EditProfileDialog extends StatefulWidget {
  const EditProfileDialog({super.key});

  @override
  State<EditProfileDialog> createState() => _EditProfileDialogState();
}

class _EditProfileDialogState extends State<EditProfileDialog> {
  final TextEditingController nameController =
      TextEditingController(text: GlobalData.instance.userData?.name);
  final TextEditingController emailController =
      TextEditingController(text: GlobalData.instance.userData?.email);
  final TextEditingController mobileController =
      TextEditingController(text: GlobalData.instance.userData?.phoneNumber);

  File? selectedAvatar; // Biến để lưu ảnh đã chọn

  bool isLoading = false;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(15),
          ),
          title: const Center(
            child: Text(
              'Edit Profile',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                InkWell(
                  onTap: () async {
                    // Chức năng thay đổi ảnh - chọn ảnh từ thư viện
                    final pickedFile = await ImagePicker()
                        .pickImage(source: ImageSource.gallery);
                    if (pickedFile != null) {
                      selectedAvatar = File(pickedFile.path);
                    }
                    setState(() {});
                  },
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(100),
                    child: selectedAvatar == null &&
                            GlobalData.instance.userData?.avtImage == null
                        ? Image.asset(
                            "assets/img/u1.png",
                            width: 100,
                            height: 100,
                            fit: BoxFit.cover,
                          )
                        : selectedAvatar != null
                            ? Image.file(
                                selectedAvatar!,
                                width: 100,
                                height: 100,
                                fit: BoxFit.cover,
                              )
                            : CachedNetworkImage(
                                width: 100,
                                height: 100,
                                fit: BoxFit.cover,
                                imageUrl: GlobalData.instance.userData!.avtImage!,
                                placeholder: (context, url) =>
                                    const CircularProgressIndicator(),
                              ),
                  ),
                ),
                const SizedBox(height: 20),
                TextField(
                  controller: nameController,
                  decoration: const InputDecoration(labelText: 'Name'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: emailController,
                  decoration: const InputDecoration(labelText: 'Email'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: mobileController,
                  decoration: const InputDecoration(labelText: 'Mobile Number'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(context);
              },
              child: const Text(
                "Huỷ",
                style: TextStyle(color: Colors.red),
              ),
            ),
            ElevatedButton(
              onPressed: () async {
                // Xử lý xác nhận và đẩy ảnh lên Firestore
                setState(() {
                  isLoading = true;
                });
                String? avatarUrl;
                if (selectedAvatar != null) {
                  // Upload ảnh lên Firebase Storage
                  avatarUrl = await ImageService.uploadImage(selectedAvatar!);
                }

                // Cập nhật dữ liệu người dùng
                // await FirebaseFirestore.instance
                //     .collection('users')
                //     .doc(GlobalData.instance.userData?.userId)
                //     .update({
                //   'name': nameController.text,
                //   'email': emailController.text,
                //   'mobile_number': mobileController.text,
                //   if (avatarUrl != null)
                //     'avtImage': avatarUrl, // Cập nhật URL ảnh nếu có
                // });

                // Cập nhật lại dữ liệu trong GlobalData (nếu cần)
                // GlobalData.instance.userData = UserData(
                //   state: GlobalData.instance.userData!.state,
                //   userId: GlobalData.instance.userData!.userId,
                //   email: emailController.text,
                //   phoneNumber: mobileController.text,
                //   name: nameController.text,
                //   restaurantId: GlobalData.instance.userData!.restaurantId,
                //   role: GlobalData.instance.userData?.role ?? "user",
                //   avtImage: avatarUrl ??
                //       GlobalData.instance.userData!
                //           .avtImage, // Giữ URL ảnh cũ nếu không thay đổi
                // );

                // Hiển thị thông báo thành công
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                      content: Text('Profile updated successfully!')),
                );
                // await GlobalData.instance.fetchUserData();

                Navigator.pop(context);
              },
              child: const Text(
                "Confirm",
                style: TextStyle(color: Colors.green),
              ),
            ),
          ],
        ),
        if (isLoading)
        Align(
          alignment: Alignment.center,
          child: Container(
            height: 500,
            width: 250,
            color: Colors.transparent,
            child: Center(
              child: Container(
                // height: double.infinity,
                // width: double.infinity,
                height: 180,
                width: 180,
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12),
                  boxShadow: const [
                    BoxShadow(
                      color: Colors.black12,
                      blurRadius: 2,
                      offset: Offset(0, 2),
                    )
                  ],
                ),
                child: const Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      CircularProgressIndicator(),
                      SizedBox(height: 20),
                      Text(
                        "Loading...",
                        style: TextStyle(
                          fontSize: 18,
                        ),
                      )
                    ],
                  ),
                ),
              ),
            ),
          ),
        )
      ],
    );
  }
}

Future showEditProfileDialog(BuildContext context) async {
  await showDialog(
    context: context,
    barrierDismissible: false,
    builder: (BuildContext context) {
      return const EditProfileDialog();
    },
  );
}
