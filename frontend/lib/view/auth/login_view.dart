import 'package:cp_restaurants/common/app_snack_bar.dart';
import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/common/jwt_session_helper.dart';
import 'package:cp_restaurants/common/login_session_log.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/view/admin/home_admin/home_admin_view.dart';
import 'package:cp_restaurants/view/auth/auth_view_model.dart';
import 'package:cp_restaurants/view/lock_account_view/lock_account_view.dart';
import 'package:cp_restaurants/view/main_tab/main_tab_view.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:flutter/material.dart';
import 'package:jwt_decoder/jwt_decoder.dart';
import 'package:provider/provider.dart';
import '../../common/color_extension.dart';
import '../../common_widget/line_textfield.dart';
import '../../common_widget/round_button.dart';
import '../../data/repository/user_repository.dart';
import 'components/forgot_password_view.dart';
import 'signup_view.dart';

class LoginView extends StatefulWidget {
  const LoginView({super.key});

  @override
  State<LoginView> createState() => _LoginViewState();
}

class _LoginViewState extends State<LoginView> {
  String password = "";
  String email = "";
  bool isLoading = false;
  bool isPasswordVisible = false;
  TextEditingController txtEmail = TextEditingController();
  TextEditingController txtPassword = TextEditingController();
  final _formkey = GlobalKey<FormState>();

  Future<void> login() async {
    if (txtEmail.text.isEmpty || txtPassword.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
        dismissDirection: DismissDirection.up,
        behavior: SnackBarBehavior.floating,
        duration: Duration(seconds: 1),
        // margin: EdgeInsets.only(
        //     bottom: media.width * 1.4,
        //     left: 20,
        //     right: 20),
        backgroundColor: Colors.redAccent,
        content: Text(
          "Please enter both email and password",
          style: TextStyle(
            fontSize: 15,
            color: Colors.white,
          ),
        ),
      ));
      return; // Stop execution if fields are empty
    }

    setState(() {
      isLoading = true;
    });

    await context.read<AuthViewModel>().login(
        email: txtEmail.text.trim(),
        password: txtPassword.text.trim(),
        onError: (p0) {
          setState(() {
            isLoading = false;
          });
          if (context.mounted) {
            AppSnackBar.showDetailed(
              context,
              'Đăng nhập thất bại',
              p0.toString(),
            );
          }
        },
        onSuccess: (token) async {
          Map<String, dynamic> decodedToken = JwtDecoder.decode(token);
          final resolvedId = JwtSessionHelper.parseUserId(decodedToken);
          if (resolvedId == null) {
            loginSessionLog(
              'LoginView: parseUserId null — kiểm tra claim Id trong JWT. decoded keys=${decodedToken.keys.toList()}',
            );
            if (context.mounted) {
              AppSnackBar.showDetailed(
                context,
                'Không đọc được mã người dùng (JWT)',
                'Các key trong payload: ${decodedToken.keys.join(", ")}\n'
                'Cần claim Id (user id) như backend phát hành.',
              );
            }
            setState(() {
              isLoading = false;
            });
            return;
          }
          await GlobalData.instance.fetchUserData(resolvedId.toString());
          if (GlobalData.instance.userData == null) {
            loginSessionLog(
              'LoginView: sau fetchUserData userData vẫn null (resolvedId=$resolvedId). Xem log [LoginSession] getUserById phía trên.',
            );
            if (context.mounted) {
              AppSnackBar.showDetailed(
                context,
                'Không tải được hồ sơ sau đăng nhập',
                'Đã gọi GetUserById với id=$resolvedId nhưng dữ liệu rỗng hoặc parse lỗi.\n'
                'Xem log [LoginSession] getUserById / fetchUserData trong console.',
              );
              setState(() {
                isLoading = false;
              });
            }
            return;
          }
          await UserDataRepository.saveUserData(GlobalData.instance.userData!);
          if (context.mounted) {
            context.read<RestaurantProvider>().getBookmarkRestaurants();
          }

          if (context.mounted) {
            ScaffoldMessenger.of(context).showSnackBar((SnackBar(
              dismissDirection: DismissDirection.up,
              behavior: SnackBarBehavior.floating,
              duration: const Duration(seconds: 2),
              backgroundColor: TColor.primary,
              content: const Text(
                "Đăng nhập thành công",
                style: TextStyle(
                  fontSize: 20,
                ),
              ),
            )));
          }
          // GlobalData.instance.fetchUserData().then((_) {
          setState(() {
            if (GlobalData.instance.userData?.role == null ||
                GlobalData.instance.userData?.role == "customer") {
              if (GlobalData.instance.userData?.state == 1) {
                Navigator.pushReplacement(
                  context,
                  MaterialPageRoute(
                    builder: (context) => const MainTabView(),
                  ),
                );
              } else {
                Navigator.pushReplacement(
                  context,
                  MaterialPageRoute(
                    builder: (context) => const LockAccountView(),
                  ),
                );
              }
            } else {
              Navigator.pushReplacement(
                context,
                MaterialPageRoute(
                  builder: (context) => const HomeAdminView(),
                ),
              );
            }
          });
          // });
        });
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
            key: _formkey,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.start,
              children: [
                if (Navigator.of(context).canPop())
                  Align(
                    alignment: Alignment.centerLeft,
                    child: IconButton(
                      onPressed: () => Navigator.pop(context),
                      icon: Icon(
                        Icons.arrow_back_ios,
                        color: TColor.primary,
                      ),
                    ),
                  )
                else
                  SizedBox(
                    height: media.width * 0.07,
                  ),
                Text(
                  "Welcome to\nCP Restaurant",
                  textAlign: TextAlign.center,
                  style: TextStyle(
                      color: TColor.text,
                      fontSize: 24,
                      fontWeight: FontWeight.w700),
                ),
                SizedBox(
                  height: media.width * 0.02,
                ),
                Text(
                  "Sign in to continue",
                  textAlign: TextAlign.center,
                  style: TextStyle(
                      color: TColor.gray,
                      fontSize: 16,
                      fontWeight: FontWeight.w700),
                ),
                SizedBox(
                  height: media.width * 0.07,
                ),
                LineTextField(
                  controller: txtEmail,
                  hitText: "Email",
                  keyboardType: TextInputType.emailAddress,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return "Vui lòng nhập email của bạn.";
                    }
                    return null;
                  },
                ),
                SizedBox(
                  height: media.width * 0.07,
                ),
                LineTextField(
                  controller: txtPassword,
                  obscureText: !isPasswordVisible,
                  hitText: "Password",
                  suffixIcon: IconButton(
                    onPressed: () {
                      setState(() {
                        isPasswordVisible = !isPasswordVisible;
                      });
                    },
                    icon: Icon(
                      isPasswordVisible
                          ? Icons.visibility
                          : Icons.visibility_off,
                      color: isPasswordVisible ? TColor.primary : Colors.grey,
                    ),
                  ),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return "Please enter your password.";
                    }
                    return null;
                  },
                ),
                SizedBox(
                  height: media.width * 0.02,
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(
                      onPressed: () async {
                        await Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => const ForgotPasswordView(),
                          ),
                        );

                        endEditing();
                      },
                      child: Text(
                        "Forgot Password?",
                        textAlign: TextAlign.center,
                        style: TextStyle(
                            color: TColor.primary,
                            fontSize: 16,
                            fontWeight: FontWeight.w700),
                      ),
                    ),
                  ],
                ),
                SizedBox(
                  height: media.width * 0.04,
                ),
                RoundButton(
                  title: "Login",
                  isLoading: isLoading,
                  onPressed: () async {
                    if (_formkey.currentState!.validate()) {
                      setState(() {
                        email = txtEmail.text.trim();
                        password = txtPassword.text.trim();
                      });
                    }
                    login();
                    endEditing();
                  },
                  type: RoundButtonType.primary,
                ),
                SizedBox(
                  height: media.width * 0.04,
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      "New to CP Restaurant?",
                      textAlign: TextAlign.center,
                      style: TextStyle(
                          color: TColor.gray,
                          fontSize: 16,
                          fontWeight: FontWeight.w700),
                    ),
                    TextButton(
                      onPressed: () async {
                        await Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => const SignUpView(),
                          ),
                        );
                        endEditing();
                      },
                      child: Text(
                        "Signup",
                        textAlign: TextAlign.center,
                        style: TextStyle(
                            color: TColor.primary,
                            fontSize: 16,
                            fontWeight: FontWeight.w700),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        )),
      ),
    );
  }
}
