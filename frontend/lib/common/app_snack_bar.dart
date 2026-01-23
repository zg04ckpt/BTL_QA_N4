import 'package:flutter/material.dart';

class AppSnackBar {
  static void loginRequired(BuildContext context,String action) {
    ScaffoldMessenger.of(context).showSnackBar(
       SnackBar(
        dismissDirection: DismissDirection.up,
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 2),

        // margin: EdgeInsets.only(
        //     bottom: media.width * 1.4,
        //     left: 20,
        //     right: 20),
        backgroundColor: Colors.redAccent,
        content: Text(
          "Please login to $action",
          style: const TextStyle(
            fontSize: 15,
            color: Colors.white,
          ),
        ),
      ),
    );
  }
}
