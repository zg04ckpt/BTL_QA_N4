import 'dart:io';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/data/models/local_address.dart';
import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/material.dart';

import '../../../common/app_picker.dart';
import '../../../common/color_extension.dart';
import '../../../common_widget/address_picker.dart';
import '../../../common_widget/line_textfield.dart';
import '../../../common_widget/round_button.dart';
import '../../../global/global_data.dart';

class EditProfile extends StatefulWidget {
  const EditProfile({super.key, this.userData});

  final UserData? userData;

  @override
  State<EditProfile> createState() => _EditProfileState();
}

class _EditProfileState extends State<EditProfile> {
  Address? address;
  TextEditingController txtName = TextEditingController();
  TextEditingController txtEmail = TextEditingController();
  TextEditingController txtMobile = TextEditingController();
  TextEditingController txtPassword = TextEditingController();
  TextEditingController txtConfirmPassword = TextEditingController();
  final TextEditingController txtDetailAddress = TextEditingController();

  bool isLoading = false;
  bool isPasswordVisible = false;
  bool isConfirmPasswordVisible = false;
  bool isUpdateProfile = false;

  File? avtUrl;

  String? imageUrl = "";

  final GlobalKey<FormState> _formkeysignup = GlobalKey<FormState>();

  void checkInitData() {
    if (widget.userData != null) {
      imageUrl = widget.userData?.avtImage;
      isUpdateProfile = true;
      txtName.text = widget.userData!.name;
      txtEmail.text = widget.userData!.email;
      txtMobile.text = widget.userData!.phoneNumber;
      address = widget.userData!.address;
      txtDetailAddress.text = widget.userData!.address?.detail ?? "";
    }
  }

  @override
  void initState() {
    checkInitData();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    var media = MediaQuery.of(context).size;

    return Scaffold(
      backgroundColor: Colors.white,
      body: SingleChildScrollView(
        child: SafeArea(
          child: SizedBox(
            width: media.width,
            child: Form(
              key: _formkeysignup,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.start,
                children: [
                  AppBar(
                    title: const Text(
                      "Chỉnh sửa thông tin",
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    backgroundColor: Colors.transparent,
                    elevation: 0,
                    leading: IconButton(
                      onPressed: () {
                        Navigator.pop(context);
                      },
                      icon: Icon(
                        Icons.arrow_back_ios,
                        color: TColor.primary,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: media.width * 0.4,
                    height: media.width * 0.4,
                    child: Stack(
                      children: [
                        ClipRRect(
                          borderRadius: BorderRadius.circular(100),
                          child: avtUrl != null
                              ? Image.file(
                                  avtUrl!,
                                  width: media.width * 0.4,
                                  height: media.width * 0.4,
                                  fit: BoxFit.cover,
                                )
                              : GlobalData.instance.userData?.avtImage == null
                                  ? Image.asset(
                                      "assets/img/u1.png",
                                      width: media.width * 0.4,
                                      height: media.width * 0.4,
                                      fit: BoxFit.cover,
                                    )
                                  : CachedNetworkImage(
                                      width: media.width * 0.4,
                                      height: media.width * 0.4,
                                      fit: BoxFit.cover,
                                      imageUrl: GlobalData
                                          .instance.userData!.avtImage!,
                                      placeholder: (context, url) =>
                                          const CircularProgressIndicator(),
                                      errorWidget: (context, url, error) =>
                                          Image.asset(
                                        "assets/img/u1.png",
                                        width: media.width * 0.4,
                                        height: media.width * 0.4,
                                        fit: BoxFit.cover,
                                      ),
                                    ),
                        ),
                        Center(
                          child: IconButton(
                            onPressed: () async {
                              avtUrl = await AppPicker.pickImage();
                              setState(() {});
                            },
                            icon: Container(
                              width: media.width * 0.15,
                              height: media.width * 0.15,
                              decoration: BoxDecoration(
                                  shape: BoxShape.circle,
                                  color: Colors.amber.withOpacity(0.5)),
                              child: const Icon(
                                Icons.edit_outlined,
                                color: Colors.green,
                              ),
                            ),
                          ),
                        )
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),
                  LineTextField(
                    controller: txtName,
                    hitText: "Họ và tên",
                    keyboardType: TextInputType.name,
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return "Vui lòng nhập tên của bạn.";
                      }
                      return null;
                    },
                  ),
                  LineTextField(
                    controller: txtMobile,
                    hitText: "Số điện thoại",
                    keyboardType: TextInputType.phone,
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return "Vui lòng nhập số điện thoại của bạn";
                      }
                      return null;
                    },
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: AddressPicker(
                      initData: LocalAddress(
                        district: address?.district,
                        province: address?.city,
                        ward: address?.ward,
                      ),
                      placeHolderTextStyle:
                          const TextStyle(fontWeight: FontWeight.bold),
                      onAddressChanged: (value) {
                        address!.city = value.province ?? "";
                        address!.district = value.district ?? "";
                        address!.ward = value.ward ?? "";
                      },
                      buildItem: (text) {
                        return Text(text ?? "",
                            style: const TextStyle(color: Colors.blue));
                      },
                    ),
                  ),
                  LineTextField(
                    controller: txtDetailAddress,
                    hitText: "Địa chỉ chi tiết",
                    keyboardType: TextInputType.name,
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return "Vui lòng nhập địa chỉ chi tiết";
                      }
                      return null;
                    },
                  ),
                  SizedBox(
                    height: media.width * 0.1,
                  ),
                  RoundButton(
                    title: "Xác nhận",
                    isLoading: isLoading,
                    onPressed: () async {
                      if (_formkeysignup.currentState!.validate()) {
                        endEditing();
                      }
                      setState(() {
                        isLoading = true;
                      });

                      if (avtUrl != null) {
                        var response =
                            await APIService.instance.uploadImage(avtUrl!);
                        var responseData = response.data;

                        imageUrl = responseData['image'] ?? responseData['Image'] ?? "";
                      }
                      address!.detail = txtDetailAddress.text;
                      UserData user = UserData(
                          userId: widget.userData?.userId ?? 0,
                          email: txtEmail.text,
                          phoneNumber: txtMobile.text,
                          name: txtName.text,
                          avtImage: imageUrl,
                          restaurantId: [],
                          state: widget.userData!.state,
                          role: widget.userData!.role,
                          address: address);
                      // log(user.toJson());

                      var response = await APIService.instance.request(
                          "/api/User/UpdateUser/${widget.userData!.userId}",
                          param: user.toJson(),
                          DioMethod.put);
                      if (response.statusCode == 200 ||
                          response.statusCode == 201) {
                        setState(() {
                          isLoading = false;
                        });
                        showSnackBar(
                            context, "Sửa thông tin thành công", false);
                        Navigator.pop(context);
                        return;
                      }
                      setState(() {
                        isLoading = false;
                      });
                      showSnackBar(context, "Đã có lỗi xảy ra");
                    },
                    type: RoundButtonType.primary,
                  ),
                  SizedBox(
                    height: media.width * 0.1,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

void showSnackBar(BuildContext context, String message,
    [bool? isError = true]) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      dismissDirection: DismissDirection.up,
      behavior: SnackBarBehavior.floating,
      duration: const Duration(seconds: 2),
      backgroundColor: isError! ? Colors.redAccent : TColor.primary,
      content: Text(
        message,
        style: const TextStyle(
          fontSize: 18,
        ),
      ),
    ),
  );
}
