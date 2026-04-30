import 'package:cp_restaurants/common/jwt_session_helper.dart';
import 'package:cp_restaurants/common/login_session_log.dart';
import 'package:cp_restaurants/data/repository/user_repository.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/common/app_snack_bar.dart';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/view/admin/home_admin/home_admin_view.dart';
import 'package:cp_restaurants/view/main_tab/main_tab_view.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/view/on_boarding/on_boarding_view.dart';
import 'package:cp_restaurants/view/restaurant_manager/my_restaurant.dart';
import 'package:flutter/material.dart';
import 'package:jwt_decoder/jwt_decoder.dart';
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
  bool _hasNavigated = false;

  @override
  void initState() {
    super.initState();
    Provider.of<LocationProvider>(context, listen: false).determinePosition();
    context.read<CommonProvider>().listenConnectivityChange(context);
    context.read<CommonProvider>().getIsUseManagerOnly();
    context.read<CommonProvider>().fetchCategories();
    checkUserLogin();
  }

  Future<void> checkUserLogin() async {
    String? sessionFailureDetail;
    int? traceFromJwt;
    int? traceFromPrefs;
    int? traceResolvedId;
    String? traceTokenPresent;

    try {
      loginSessionLog('Splash: bắt đầu checkUserLogin');
      var user = await UserDataRepository.fetchUserData();
      if (user != null && user.userId <= 0) {
        loginSessionLog(
          'Splash: bỏ user local vì userId<=0 (userId=${user.userId})',
        );
        user = null;
      }
      if (user != null) {
        loginSessionLog(
          'Splash: có user từ SQLite/prefs userId=${user.userId}',
        );
      } else {
        loginSessionLog('Splash: chưa có user local, thử token/JWT');
      }
      await context.read<CommonProvider>().getIsUseFingerPrint();

      // Không phụ thuộc JWT còn hạn: decode payload vẫn lấy được Id; API GetUserById không bắt buộc JWT trên server hiện tại.
      if (user == null) {
        final token = await APIService.instance.getToken();
        traceTokenPresent =
            (token != null && token.isNotEmpty) ? 'có (${token.length} ký tự)' : 'không';
        int? fromJwt;
        if (token != null && token.isNotEmpty) {
          try {
            final decoded = JwtDecoder.decode(token);
            fromJwt = JwtSessionHelper.parseUserId(decoded);
            if (JwtDecoder.isExpired(token)) {
              loginSessionLog(
                'Splash: JWT đã hết hạn — vẫn dùng Id từ payload (nếu có) để gọi GetUserById',
              );
            }
          } catch (e, st) {
            loginSessionLog('Splash: không decode được JWT', e, st);
            sessionFailureDetail ??=
                'Decode JWT lỗi: ${AppSnackBar.describeError(e, st, 600)}';
          }
        } else {
          loginSessionLog('Splash: không có access token trong prefs');
        }

        final fromPrefs = await UserDataRepository.readCachedUserId();
        final resolvedId = fromJwt ?? fromPrefs;
        traceFromJwt = fromJwt;
        traceFromPrefs = fromPrefs;
        traceResolvedId = resolvedId;

        loginSessionLog(
          'Splash: id từ JWT=$fromJwt, id từ prefs=$fromPrefs → resolved=$resolvedId',
        );

        if (resolvedId != null) {
          await GlobalData.instance.fetchUserData(resolvedId.toString());
          user = GlobalData.instance.userData;
          if (user != null) {
            await UserDataRepository.saveUserData(user);
            loginSessionLog('Splash: đã lưu user sau GetUserById');
          } else {
            loginSessionLog(
              'Splash: GetUserById/API parse trả null cho id=$resolvedId — xem log getUserById',
            );
            sessionFailureDetail =
                'GetUserById hoặc parse JSON user trả về rỗng.\n'
                '• id đã gọi: $resolvedId\n'
                '• Token prefs: $traceTokenPresent\n'
                'Xem console log [LoginSession] getUserById / fetchUserData.';
          }
        } else {
          loginSessionLog(
            'Splash: không có user id (JWT không đọc được và prefs không có cp_cached_user_id)',
          );
          sessionFailureDetail =
              'Không có id người dùng để gọi GetUserById.\n'
              '• id từ JWT (claim Id): $fromJwt\n'
              '• id từ prefs (cp_cached_user_id): $fromPrefs\n'
              '• Token trong prefs: $traceTokenPresent';
        }
      }

      GlobalData.instance.userData = user;

      if (user != null && mounted) {
        try {
          await context.read<RestaurantProvider>().getBookmarkRestaurants();
        } catch (e, st) {
          loginSessionLog(
            'Splash: lỗi getBookmarkRestaurants (không chặn đăng nhập)',
            e,
            st,
          );
        }
      }

      if (user == null) {
        if (mounted) {
          AppSnackBar.showDetailed(
            context,
            'Không lấy được thông tin đăng nhập',
            sessionFailureDetail ??
                'Không có user trong SQLite/prefs và không khôi phục được từ API.\n'
                    '• JWT id: $traceFromJwt\n'
                    '• Prefs id: $traceFromPrefs\n'
                    '• Resolved id: $traceResolvedId\n'
                    '• Token: $traceTokenPresent',
          );
        }
        _navigateTo(const OnBoardingView());
        return;
      }

      if (user.role == "admin") {
        _navigateTo(const HomeAdminView());
        return;
      }

      bool didAuthenticate = true;
      if (context.read<CommonProvider>().isUseFingerPrint) {
        try {
          didAuthenticate = await auth.authenticate(
            localizedReason: 'Xác nhận người dùng để tiếp tục',
            persistAcrossBackgrounding: true,
          );
        } on LocalAuthException catch (e, st) {
          // local_auth 3.x throws instead of returning false for many cases.
          switch (e.code) {
            case LocalAuthExceptionCode.noCredentialsSet:
            case LocalAuthExceptionCode.noBiometricsEnrolled:
            case LocalAuthExceptionCode.noBiometricHardware:
            case LocalAuthExceptionCode.biometricHardwareTemporarilyUnavailable:
              loginSessionLog(
                'Splash: sinh trắc học không dùng được (${e.code.name}) — bỏ qua, vào app',
                e,
                st,
              );
              didAuthenticate = true;
            case LocalAuthExceptionCode.userCanceled:
            case LocalAuthExceptionCode.systemCanceled:
            case LocalAuthExceptionCode.timeout:
              didAuthenticate = false;
            case LocalAuthExceptionCode.authInProgress:
            case LocalAuthExceptionCode.uiUnavailable:
            case LocalAuthExceptionCode.temporaryLockout:
            case LocalAuthExceptionCode.biometricLockout:
            case LocalAuthExceptionCode.userRequestedFallback:
            case LocalAuthExceptionCode.deviceError:
            case LocalAuthExceptionCode.unknownError:
              loginSessionLog(
                'Splash: lỗi sinh trắc học (${e.code.name})',
                e,
                st,
              );
              rethrow;
          }
        }
      }

      if (!didAuthenticate) {
        if (mounted) {
          AppSnackBar.showDetailed(
            context,
            'Xác thực sinh trắc học thất bại',
            'Đăng nhập vân tay / Face ID bị hủy hoặc không thành công.\n'
            'Tắt tùy chọn vân tay trong app (nếu có) hoặc thử lại.',
          );
        }
        _navigateTo(const OnBoardingView());
        return;
      }

      if (context.read<CommonProvider>().isUseManagerOnly) {
        _navigateTo(const ManagerHomeView());
      } else {
        _navigateTo(const MainTabView());
      }
    } catch (e, st) {
      loginSessionLog('Splash: checkUserLogin exception — toàn bộ luồng dừng', e, st);
      if (mounted) {
        AppSnackBar.showDetailed(
          context,
          'Lỗi khi khởi tạo phiên đăng nhập',
          AppSnackBar.describeError(e, st),
        );
      }
      _navigateTo(const OnBoardingView());
    }
  }

  void _navigateTo(Widget page) {
    if (!mounted || _hasNavigated) {
      return;
    }
    _hasNavigated = true;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (context) => page),
    );
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
