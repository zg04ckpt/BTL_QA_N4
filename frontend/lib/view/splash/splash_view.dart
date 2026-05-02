import 'package:cp_restaurants/data/repository/user_repository.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/view/admin/home_admin/home_admin_view.dart';
import 'package:cp_restaurants/view/auth/login_view.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:cp_restaurants/view/main_tab/main_tab_view.dart';
import 'package:cp_restaurants/view/on_boarding/on_boarding_view.dart';
import 'package:cp_restaurants/common/color_extension.dart';
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
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      context.read<CommonProvider>().listenConnectivityChange(context);
      context.read<CommonProvider>().getIsUseManagerOnly();
      context.read<RestaurantProvider>().getBookmarkRestaurants();
      checkUserLogin();
    });
  }

  Future<void> checkUserLogin() async {
    void goOnboarding() {
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const OnBoardingView()),
      );
    }

    void goLogin() {
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const LoginView()),
      );
    }

    try {
      final user = await UserDataRepository.fetchUserData().timeout(
        const Duration(seconds: 15),
        onTimeout: () {
          debugPrint('Splash: fetchUserData timeout');
          return null;
        },
      );
      if (!mounted) return;

      await context.read<CommonProvider>().getIsUseFingerPrint().timeout(
            const Duration(seconds: 8),
          );

      GlobalData.instance.userData = user;
      if (user == null) {
        goOnboarding();
        return;
      }

      if (user.role == "admin") {
        if (!mounted) return;
        Navigator.of(context).pushReplacement(
          MaterialPageRoute(builder: (_) => const HomeAdminView()),
        );
        return;
      }

      bool didAuthenticate = true;
      final wantBio = context.read<CommonProvider>().isUseFingerPrint;
      if (wantBio) {
        final canUseBio =
            await auth.isDeviceSupported() &&
                (await auth.getAvailableBiometrics()).isNotEmpty;
        if (!canUseBio) {
          didAuthenticate = false;
        } else {
          try {
            didAuthenticate = await auth
                .authenticate(
                  localizedReason: 'Xác nhận người dùng để tiếp tục',
                  options: const AuthenticationOptions(),
                )
                .timeout(
                  const Duration(seconds: 45),
                  onTimeout: () => false,
                );
          } catch (_) {
            didAuthenticate = false;
          }
        }
      }

      if (!mounted) return;
      if (!didAuthenticate) {
        showSnackBar(context, "Không xác minh được người dùng");
        goLogin();
        return;
      }

      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const MainTabView()),
      );
    } catch (e, st) {
      debugPrint('Splash checkUserLogin error: $e\n$st');
      goOnboarding();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: TColor.bg,
      body: SizedBox(
        height: double.infinity,
        width: double.infinity,
        child: Center(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.center,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Lottie.asset("assets/animations/loading.json",
                  height: 150,
                  width: 150,
                  errorBuilder: (_, __, ___) => const SizedBox(
                    height: 150,
                    width: 150,
                    child: Center(
                      child: CircularProgressIndicator(),
                    ),
                  )),
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
