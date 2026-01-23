import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/view/admin/restaurant_management/restaurant_management_view.dart';
import 'package:cp_restaurants/view/admin/review_management/review_management_view.dart';
import 'package:cp_restaurants/view/admin/user_management/user_management_view.dart';
import 'package:cp_restaurants/view/on_boarding/on_boarding_view.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';

import '../../../common_widget/dialog/post_noti_dialog.dart';
import '../../../global/global_data.dart';

class HomeAdminView extends StatefulWidget {
  const HomeAdminView({super.key});

  @override
  State<HomeAdminView> createState() => _HomeAdminViewState();
}

class _HomeAdminViewState extends State<HomeAdminView> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(
          "QUẢN TRỊ VIÊN",
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
        ),
        centerTitle: true,
        actions: [
          IconButton(
            onPressed: () {
              confirmLogout();
            },
            icon: const Icon(
              Icons.logout_outlined,
              color: Colors.red,
            ),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.start,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Row(
              children: [
                _buildAdminButton(
                  title: "Quản lý nhà hàng",
                  image: "assets/img/res_manager.png",
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const RestaurantManagementView(),
                      ),
                    );
                  },
                ),
                const SizedBox(width: 12),
                _buildAdminButton(
                  title: "Quản lý người dùng",
                  image: "assets/img/user_manager.png",
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const UserManagementView(),
                      ),
                    );
                  },
                ),
              ],
            ),
            const SizedBox(height: 20),
            Row(
              children: [
                _buildAdminButton(
                  title: "Quản lý đánh giá",
                  image: "assets/img/reviews_manager.png",
                  onPressed: () {
                    Navigator.push(
                        context,
                        MaterialPageRoute(
                            builder: (context) =>
                                const ReviewManagementView()));
                  },
                ),
                const SizedBox(width: 12),
                _buildAdminButton(
                  title: "Thông báo người dùng",
                  image: "assets/img/post_noti.png",
                  onPressed: () {
                    showDialog(
                      context: context,
                      builder: (BuildContext context) {
                        return const PostNotificationDialog();
                      },
                    );
                  },
                ),
              ],
            ),
            
          ],
        ),
      ),
    );
  }

  Widget _buildAdminButton(
      {required String title,
      required String image,
      required Function() onPressed}) {
    return Expanded(
      child: InkWell(
        onTap: onPressed,
        child: Container(
          height: 160,
          padding: const EdgeInsets.symmetric(horizontal: 16,vertical: 20),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(10),
            color: Colors.white,
            boxShadow: const [
              BoxShadow(
                  color: Colors.black12, blurRadius: 2, offset: Offset(0, 1))
            ],
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Text(
                title,
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: Colors.green[300],
                ),
              ),
              // const SizedBox(height: 8),
              const Spacer(),
              Image.asset(
                image,
                height: 60,
                width: 60,
              )
            ],
          ),
        ),
      ),
    );
  }

  Future<void> confirmLogout() async {
    return showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: Center(
            child: RichText(
              text: TextSpan(
                style: const TextStyle(
                  fontSize: 18.0,
                  color: Colors.black, // Default color for text
                ),
                children: <TextSpan>[
                  const TextSpan(
                    text: 'Logout from ',
                    style: TextStyle(
                        color: Colors.black, fontWeight: FontWeight.bold),
                  ),
                  TextSpan(
                    text: 'CP Restaurants',
                    style: TextStyle(
                      color: TColor.primary, // Change color to yellow
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
            ),
          ),
          content: const Text('Are you sure you want to logout?'),
          actions: <Widget>[
            TextButton(
              onPressed: () {
                Navigator.of(context).pop(); // Close the dialog
              },
              child: const Text(
                'Huỷ',
                style: TextStyle(
                  color: Colors.black,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            TextButton(
              onPressed: () async {
                // Perform logout logic here
                await FirebaseAuth.instance.signOut();

                if (mounted) {
                  Navigator.of(context).pop();
                }

                if (mounted) {
                  GlobalData.instance.user = null;
                  GlobalData.instance.userData = null;

                  Navigator.pushReplacement(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const OnBoardingView(),
                    ),
                  );
                }
              },
              child: const Text(
                'Logout',
                style:
                    TextStyle(color: Colors.red, fontWeight: FontWeight.bold),
              ),
            ),
          ],
        );
      },
    );
  }
}
