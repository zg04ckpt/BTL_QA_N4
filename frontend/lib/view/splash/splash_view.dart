import 'package:cp_restaurants/data/repository/user_repository.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/view/admin/home_admin/home_admin_view.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:cp_restaurants/view/main_tab/main_tab_view.dart';
import 'package:cp_restaurants/view/on_boarding/on_boarding_view.dart';
import 'package:cp_restaurants/view/restaurant_manager/my_restaurant.dart';
import 'package:cp_restaurants/view/scan_qr/scan_qr_page.dart';
import 'package:flutter/material.dart';
import 'package:local_auth/local_auth.dart';
import 'package:lottie/lottie.dart';
import 'package:provider/provider.dart';

class SplashView extends StatefulWidget {
  const SplashView({super.key});

  @override
  State<SplashView> createState() => _SplashViewState();
}

class _SplashViewState extends State<SplashView> {
  final LocalAuthentication auth = LocalAuthentication();

  @override
  void initState() {
    super.initState();
    Provider.of<LocationProvider>(context, listen: false).determinePosition();
    context.read<CommonProvider>().listenConnectivityChange(context);
    context.read<CommonProvider>().getIsUseManagerOnly();
    context.read<RestaurantProvider>().getBookmarkRestaurants();
    checkUserLogin();
  }

  Future<void> checkUserLogin() async {
    var user = await UserDataRepository.fetchUserData();
    await context.read<CommonProvider>().getIsUseFingerPrint();

    GlobalData.instance.userData = user;
    if (user == null) {
      Navigator.of(context)
          .pushReplacement(MaterialPageRoute(builder: (context) {
        return const OnBoardingView();
      }));
    } else {
      if (user.role == "admin") {
        Navigator.of(context)
            .pushReplacement(MaterialPageRoute(builder: (context) {
          return const HomeAdminView();
        }));
      } else {
        bool didAuthenticate = true;
        if (context.read<CommonProvider>().isUseFingerPrint) {
          didAuthenticate = await auth.authenticate(
              localizedReason: 'Xác nhận người dùng để tiếp tục',
              options: const AuthenticationOptions());
        }
        if (!didAuthenticate) {
          showSnackBar(context, "Không xác minh được người dùng");
          return;
        }

        Navigator.of(context)
            .pushReplacement(MaterialPageRoute(builder: (context) {
          if (context.read<CommonProvider>().isUseManagerOnly) {
            return const ManagerHomeView();
          }else{
            return const MainTabView();
          }
        }));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SizedBox(
        height: double.infinity,
        width: double.infinity,
        child: Center(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.center,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Lottie.asset("assets/animations/loading.json",
                  height: 150, width: 150),
              const Text(
                "Retrieving login information...",
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w400,
                ),
              )
            ],
          ),
        ),
      ),
    );
  }
}
