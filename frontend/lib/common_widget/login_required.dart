import 'package:cp_restaurants/common_widget/round_button.dart';
import 'package:cp_restaurants/view/auth/login_view.dart';
import 'package:flutter/material.dart';

class LoginRequired extends StatelessWidget {
  const LoginRequired({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Image.asset("assets/img/login_required.jpg"),
            const Text(
              "Vui lòng đăng nhập để xem chi tiết",
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: Colors.black,
              ),
            ),
            const SizedBox(height: 16),
            RoundButton(
              title: "Đăng nhập",
              isLoading: false,
              onPressed: () async {
                Navigator.of(context)
                    .push(MaterialPageRoute(builder: (context) {
                  return const LoginView();
                }));
              },
              type: RoundButtonType.primary,
            ),
          ],
        ),
      ),
    );
  }
}
