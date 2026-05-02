import 'dart:developer';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common/app_utils.dart';
import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/common_widget/login_required.dart';
import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/common_widget/app_embedded_map.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/view/my_profile/components/edit_profile_page.dart';
import 'package:cp_restaurants/view/on_boarding/on_boarding_view.dart';
import 'package:cp_restaurants/view/restaurant_manager/my_restaurant.dart';
import 'package:cp_restaurants/view/rv_history/rv_history_view.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:local_auth/local_auth.dart';
import 'package:provider/provider.dart';
import '../../common/color_extension.dart';
import '../../common_widget/menu_row.dart';
import '../../data/models/address.dart';
import '../../data/repository/user_repository.dart';

Address sampleAddress = Address(
    street: "123 Main St",
    city: "Hanoi",
    district: "Hà Đông",
    ward: "Mộ Lao",
    detail: "check");

UserData sampleUser = UserData(
  userId: 1,
  email: "example@example.com",
  phoneNumber: "0123456789",
  name: "John Doe",
  restaurantId: ["res_001", "res_002"],
  role: "Admin",
  avtImage: "https://example.com/avatar.png",
  state: 0,
  address: sampleAddress,
  reports: ["rep_001", "rep_002"],
  reviews: ["rev_001"],
  // restaurants: ["res_003", "res_004"],
);

class MyProfileView extends StatefulWidget {
  const MyProfileView({super.key});

  @override
  State<MyProfileView> createState() => _MyProfileViewState();
}

class _MyProfileViewState extends State<MyProfileView> {
  final LocalAuthentication auth = LocalAuthentication();

  @override
  void initState() {
    getUser();
    super.initState();
  }

  Future<void> getUser() async {
    // var user = await UserDataRepository.fetchUserData();
    // log(user?.email??"no user");
    var distance = AppUtils.getRestaurantDistance(20.988311, 105.798657);
    log(distance.toDistanceText());
  }

  @override
  Widget build(BuildContext context) {
    var media = MediaQuery.of(context).size;
    if (true) {
      return Scaffold(
        backgroundColor: TColor.bg,
        appBar: AppBar(
          automaticallyImplyLeading: false,
          backgroundColor: Colors.white,
          elevation: 0,
          centerTitle: true,
          title: const Text(
            "Thông tin cá nhân",
            style: TextStyle(
              fontWeight: FontWeight.bold,
            ),
          ),
          actions: [
            TextButton(
              onPressed: () async {
                await Navigator.of(context)
                    .push(MaterialPageRoute(builder: (context) {
                  return EditProfile(
                    userData: GlobalData.instance.userData,
                  );
                }));
                setState(() {});
                await GlobalData.instance.fetchUserData(
                    GlobalData.instance.userData?.userId.toString() ?? '');
                if (GlobalData.instance.userData != null) {
                  await UserDataRepository.saveUserData(
                      GlobalData.instance.userData!);
                }
                setState(() {});
              },
              child: Text(
                "Sửa",
                style: TextStyle(
                    color: TColor.primary,
                    fontSize: 16,
                    fontWeight: FontWeight.w700),
              ),
            )
          ],
        ),
        body: SingleChildScrollView(
          child: Consumer<CommonProvider>(builder: (context, common, child) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Container(
                  width: media.width,
                  decoration: const BoxDecoration(
                      color: Colors.white,
                      boxShadow: [
                        BoxShadow(color: Colors.black12, blurRadius: 1)
                      ]),
                  child: Consumer<LocationProvider>(
                      builder: (context, locationProvider, child) {
                    String? locationCity;
                    final place = locationProvider.currentLocationName;
                    if (place != null) {
                      final sub =
                          place.subAdministrativeArea?.trim() ?? '';
                      final adm = place.administrativeArea?.trim() ?? '';
                      if (sub.isEmpty && adm.isEmpty) {
                        locationCity = 'Vị trí không xác định';
                      } else if (sub.isEmpty) {
                        locationCity = adm;
                      } else if (adm.isEmpty || sub == adm) {
                        locationCity = sub;
                      } else {
                        locationCity = '$sub - $adm';
                      }
                    } else {
                      locationCity = 'Vị trí không xác định';
                    }
                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        ClipRRect(
                          borderRadius:
                              BorderRadius.circular(media.width * 0.25),
                          child: Container(
                            color: TColor.secondary,
                            child: () {
                              final url =
                                  GlobalData.instance.userData?.avtImage;
                              final hasUrl =
                                  url != null && url.trim().isNotEmpty;
                              if (!hasUrl) {
                                return Image.asset(
                                  "assets/img/u1.png",
                                  width: media.width * 0.4,
                                  height: media.width * 0.4,
                                  fit: BoxFit.cover,
                                );
                              }
                              return CachedNetworkImage(
                                width: media.width * 0.4,
                                height: media.width * 0.4,
                                fit: BoxFit.cover,
                                errorWidget: (context, url, error) {
                                  return Image.asset(
                                    "assets/img/u1.png",
                                    width: media.width * 0.4,
                                    height: media.width * 0.4,
                                    fit: BoxFit.cover,
                                  );
                                },
                                imageUrl: resolveMediaUrl(url),
                                imageBuilder: (context, imageProvider) =>
                                    Container(
                                  decoration: BoxDecoration(
                                    image: DecorationImage(
                                      image: imageProvider,
                                      fit: BoxFit.cover,
                                    ),
                                  ),
                                ),
                                placeholder: (context, url) =>
                                    const CircularProgressIndicator(),
                              );
                            }(),
                          ),
                        ),
                        SizedBox(
                          height: media.width * 0.04,
                        ),
                        Text(
                          GlobalData.instance.userData?.name ?? "...",
                          textAlign: TextAlign.center,
                          style: TextStyle(
                              color: TColor.text,
                              fontSize: 24,
                              fontWeight: FontWeight.w700),
                        ),
                        SizedBox(height: media.width * 0.02),
                        Divider(
                          color: TColor.gray,
                          height: 1,
                        ),
                        SizedBox(height: media.width * 0.04),
                        Text(
                          GlobalData.instance.userData?.email ?? "...",
                          textAlign: TextAlign.center,
                          style: TextStyle(
                              color: TColor.primary,
                              fontSize: 18,
                              fontWeight: FontWeight.w700),
                        ),
                        SizedBox(height: media.width * 0.02),
                        Text(
                          GlobalData.instance.userData?.phoneNumber ?? "...",
                          textAlign: TextAlign.center,
                          style: TextStyle(
                              color: TColor.gray,
                              fontSize: 18,
                              fontWeight: FontWeight.w700),
                        ),
                        SizedBox(
                          height: media.width * 0.025,
                        ),
                        Text(
                          locationCity,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                              color: TColor.gray,
                              fontSize: 16,
                              fontWeight: FontWeight.w700),
                        ),
                        SizedBox(
                          height: media.width * 0.1,
                        ),
                      ],
                    );
                  }),
                ),
                Container(
                  margin: const EdgeInsets.symmetric(vertical: 4),
                  padding: const EdgeInsets.symmetric(horizontal: 15),
                  decoration: const BoxDecoration(
                      color: Colors.white,
                      boxShadow: [
                        BoxShadow(color: Colors.black12, blurRadius: 1)
                      ]),
                  child: Column(
                    children: [
                      MenuRow(
                        icon: "assets/img/rv_history.png",
                        title: "Lịch sử đánh giá",
                        onPressed: () {
                          Navigator.push(
                              context,
                              MaterialPageRoute(
                                  builder: (context) => const RvHistoryView()));
                        },
                      ),
                      MenuRow(
                        icon: "assets/img/res_manager.png",
                        title: "Quản lý nhà hàng",
                        onPressed: () {
                          Navigator.push(
                              context,
                              MaterialPageRoute(
                                  builder: (context) =>
                                      const ManagerHomeView()));
                        },
                      ),
                      MenuRow(
                        icon: "assets/img/mapView.png",
                        title: "Nền bản đồ (OSM / vệ tinh)",
                        onPressed: () {
                          showMapTileStyleSheet(context);
                        },
                      ),
                      const Divider(
                        color: Colors.black26,
                        height: 1,
                      ),
                      MenuRow(
                        icon: "assets/img/fingerprint.png",
                        title: "Xác thực sinh trác học",
                        showleftIcon: false,
                        value: common.isUseFingerPrint,
                        showSwicth: true,
                        onChanged: (p0) async {
                          bool didAuthenticate = true;
                          if (p0) {
                            didAuthenticate = await auth.authenticate(
                                localizedReason:
                                    'Xác nhận người dùng để tiếp tục',
                                options: const AuthenticationOptions());
                          }
                          if (!didAuthenticate) {
                            return;
                          }
                          await common.setIsUseFingerPrint(p0);
                          setState(() {});
                        },
                        onPressed: () {},
                      ),
                      const Divider(
                        color: Colors.black26,
                        height: 1,
                      ),
                      MenuRow(
                        icon: "assets/img/manager.png",
                        title: "Chỉ dùng chức năng quản lý",
                        showleftIcon: false,
                        value: common.isUseManagerOnly,
                        showSwicth: true,
                        onChanged: (p0) async {
                          await common.setIsUseManagerOnly(p0);
                          setState(() {});
                        },
                        onPressed: () {},
                      ),
                      const Divider(
                        color: Colors.black26,
                        height: 1,
                      ),
                      MenuRow(
                        icon: "assets/img/sign_out.png",
                        title: "Đăng xuất",
                        showleftIcon: false,
                        txtcolor: Colors.red,
                        color: Colors.red,
                        onPressed: () {
                          confirmLogout();
                        },
                      ),
                      const Divider(
                        color: Colors.black26,
                        height: 1,
                      )
                    ],
                  ),
                )
              ],
            );
          }),
        ),
      );
    } else {
      return const LoginRequired();
    }
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
                'Đăng xuất',
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
