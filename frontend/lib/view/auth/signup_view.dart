import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/view/auth/auth_view_model.dart';
import 'package:cp_restaurants/view/auth/login_view.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../common/color_extension.dart';
import '../../common_widget/address_picker.dart';
import '../../common_widget/line_textfield.dart';
import '../../common_widget/round_button.dart';

class SignUpView extends StatefulWidget {
  const SignUpView({super.key, this.userData});

  final UserData? userData;

  @override
  State<SignUpView> createState() => _SignUpViewState();
}

class _SignUpViewState extends State<SignUpView> {
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

  final GlobalKey<FormState> _formkeysignup = GlobalKey<FormState>();

  Future<void> register() async {
    final fields = [
      txtName.text,
      txtEmail.text,
      txtMobile.text,
      if (!isUpdateProfile) txtPassword.text,
      txtDetailAddress.text,
      if (!isUpdateProfile) txtConfirmPassword.text,
      address?.district,
      address?.city,
      address?.ward,
    ];

    if (fields.any((field) => field == null || field.isEmpty)) {
      showSnackBar(
        context,
        "Vui lòng điền đủ các trường thông tin...!",
      );
      return;
    }
    if (txtPassword.text != txtConfirmPassword.text && !isUpdateProfile) {
      showSnackBar(context, "Mật khẩu xác nhận không trùng khớp");
      return;
    }

    try {
      setState(() {
        isLoading = true;
      });
      address!.lat = 0.0;
      address!.lon = 0.0;

      address!.detail = txtDetailAddress.text;

      context.read<AuthViewModel>().register(
          email: txtEmail.text,
          password: txtPassword.text,
          name: txtName.text,
          phoneNumber: txtMobile.text,
          address: address!,
          onSuccess: (result) {
            showSnackBar(context, "Đăng ký thành công, đăng nhập để tiếp tục", false);

            if (mounted) {
              Navigator.pushReplacement(context,
                  MaterialPageRoute(builder: (context) => const LoginView()));
              GlobalData.instance.user = FirebaseAuth.instance.currentUser;
            }
          },
          onError: (onError) {
            showSnackBar(context, onError);
          });
    } catch (e) {
      showSnackBar(context, e.toString());
    } finally {
      setState(() {
        isLoading = false;
      });
    }
  }

  void checkInitData() {
    if (widget.userData != null) {
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
                  Text(
                    "Chào mừng tới với\nCP Restaurant",
                    textAlign: TextAlign.center,
                    style: TextStyle(
                        color: TColor.text,
                        fontSize: 24,
                        fontWeight: FontWeight.w700),
                  ),
                  Text(
                    "Đăng ký để tiếp tục",
                    textAlign: TextAlign.center,
                    style: TextStyle(
                        color: TColor.gray,
                        fontSize: 16,
                        fontWeight: FontWeight.w700),
                  ),
                  LineTextField(
                    controller: txtName,
                    hitText: "Họ và tên",
                    keyboardType: TextInputType.name,
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return "Vui lònh nhập tên của bạn";
                      }
                      return null;
                    },
                  ),
                  LineTextField(
                    controller: txtEmail,
                    hitText: "Email",
                    keyboardType: TextInputType.emailAddress,
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return "Vui lòng nhập email của bạn.";
                      }
                      final emailRegex = RegExp(
                          r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
                      if (!emailRegex.hasMatch(value)) {
                        return 'Email không hợp lệ';
                      }
                      return null;
                    },
                  ),
                  LineTextField(
                    maxLength: 11,
                    controller: txtMobile,
                    hitText: "Số điện thoại",
                    keyboardType: TextInputType.phone,
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return "Vui lòng nhập số điện thoại của bạn.";
                      }
                      if (value.length > 11 || value.length < 9) {
                        return "Số điện thoại trong khoảng 9-11 số";
                      }

                      return null;
                    },
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: AddressPicker(
                      placeHolderTextStyle:
                          const TextStyle(fontWeight: FontWeight.bold),
                      onAddressChanged: (vlue) {
                        var addr = Address(
                          id: 0,
                          street: "",
                          city: vlue.province ?? "",
                          district: vlue.district ?? "",
                          ward: vlue.ward ?? "",
                          detail: "",
                        );
                        address = addr;
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
                  if (!isUpdateProfile)
                    LineTextField(
                      controller: txtPassword,
                      obscureText: !isPasswordVisible,
                      hitText: "Mật khẩu",
                      suffixIcon: GestureDetector(
                        onTap: () {
                          setState(() {
                            isPasswordVisible = !isPasswordVisible;
                          });
                        },
                        child: Padding(
                          padding: const EdgeInsets.only(right: 17),
                          child: Icon(
                            isPasswordVisible
                                ? Icons.visibility
                                : Icons.visibility_off,
                            color: isPasswordVisible
                                ? TColor.primary
                                : Colors.grey,
                          ),
                        ),
                      ),
                      validator: (value) {
                        if (value == null || value.isEmpty) {
                          return "Tạo mật khẩu để tiếp tục";
                        }
                        return null;
                      },
                    ),
                  if (!isUpdateProfile)
                    LineTextField(
                      controller: txtConfirmPassword,
                      obscureText: !isConfirmPasswordVisible,
                      suffixIcon: GestureDetector(
                        onTap: () {
                          setState(() {
                            isConfirmPasswordVisible =
                                !isConfirmPasswordVisible;
                          });
                        },
                        child: Padding(
                          padding: const EdgeInsets.only(right: 17),
                          child: Icon(
                            isConfirmPasswordVisible
                                ? Icons.visibility
                                : Icons.visibility_off,
                            color: isConfirmPasswordVisible
                                ? TColor.primary
                                : Colors.grey,
                          ),
                        ),
                      ),
                      hitText: "Xác nhận mật khẩu",
                      validator: (value) {
                        if (value == null || value.isEmpty) {
                          return "Vui lòng xác nhận lại mật khẩu";
                        }
                        if (value != txtPassword.text) {
                          return "Mật khẩu không trùng khớp";
                        }
                        return null;
                      },
                    ),
                  SizedBox(
                    height: media.width * 0.1,
                  ),
                  RoundButton(
                    title: "Đăng ký",
                    isLoading: isLoading,
                    onPressed: () async {
                      if (_formkeysignup.currentState!.validate()) {
                        register();
                        endEditing();
                      }
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
      duration: const Duration(seconds: 3),
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
