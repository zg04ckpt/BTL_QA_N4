import 'package:cp_restaurants/view/on_boarding/on_boarding_view.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../global/global_data.dart';

class LockAccountView extends StatelessWidget {
  const LockAccountView({super.key});

  Future<void> sendMailToAdmin() async {
    final Uri emailLaunchUri = Uri(
      scheme: 'mailto',
      path: 'huykullkaq@gmail.com',
      queryParameters: {
        'subject': 'CP_FOOD UNLOCK ACCOUNT',
      },
    );

    if (await canLaunchUrl(emailLaunchUri)) {
      await launchUrl(emailLaunchUri);
    } else {
      print('Could not launch email client');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SizedBox(
        height: double.infinity,
        width: double.infinity,
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Image.asset(
                  "assets/img/lock_account.png",
                  height: 80,
                ),
                const SizedBox(height: 24),
                const Text(
                  "Your account is locked, please contact admin to unlock your account",
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 30),
                ElevatedButton(
                  onPressed: sendMailToAdmin,
                  child: const Text("Email to Admin"),
                ),
                TextButton(
                  onPressed: () async {
                    // Perform logout logic here
                    await FirebaseAuth.instance.signOut();

                    GlobalData.instance.user = null;
                    GlobalData.instance.userData = null;

                    Navigator.pushReplacement(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const OnBoardingView(),
                      ),
                    );
                  },
                  child: const Text(
                    'Logout',
                    style: TextStyle(
                        color: Colors.red, fontWeight: FontWeight.bold),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
