import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/location_service.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../common/color_extension.dart';
import '../bookmark/bookmark_view.dart';
import '../discovery/discovery_view.dart';
import '../home/home_view.dart';
import '../my_profile/my_profile_view.dart';

class MainTabView extends StatefulWidget {
  const MainTabView({super.key});

  @override
  State<MainTabView> createState() => _MainTabViewState();
}

class _MainTabViewState extends State<MainTabView>
    with TickerProviderStateMixin {
  TabController? controller;

  @override
  void initState() {
    controller = TabController(length: 4, vsync: this);
    LocationService.checkAndRequestLocationPermission();

    controller?.addListener(() {
      if (mounted) {
        setState(() {});
      }
    });
    super.initState();
    context.read<CommonProvider>().getTopics();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      
      body: TabBarView(controller: controller, children: const [
        HomeView(),
        DiscoveryView(),
        BookmarkView(),
        MyProfileView()
      ]),
      bottomNavigationBar: BottomAppBar(
        color: Colors.white,
        child: TabBar(
            controller: controller,
            overlayColor: WidgetStateColor.resolveWith(
              (Set<WidgetState> states) {
                if (states.contains(WidgetState.pressed)) {
                  return Colors.transparent;
                }

                return Colors.transparent;
              },
            ),
            labelColor: TColor.primary,
            labelPadding: EdgeInsets.zero,
            unselectedLabelColor: TColor.gray,
            labelStyle:
                const TextStyle(fontSize: 10, fontWeight: FontWeight.w700),
            unselectedLabelStyle:
                const TextStyle(fontSize: 10, fontWeight: FontWeight.w700),
            indicatorColor: Colors.transparent,
            padding: EdgeInsets.zero,
            tabs: [
              Tab(
                icon: Image.asset(
                  "assets/img/home_tab.png",
                  width: 25,
                  height: 25,
                  fit: BoxFit.contain,
                  color: controller?.index == 0 ? TColor.primary : TColor.gray,
                ),
                text: "Trang chủ",
              ),
              Tab(
                icon: Image.asset(
                  "assets/img/discovery_tab.png",
                  width: 25,
                  height: 25,
                  fit: BoxFit.contain,
                  color: controller?.index == 1 ? TColor.primary : TColor.gray,
                ),
                text: "Khám phá",
              ),
              Tab(
                icon: Image.asset(
                  "assets/img/bookmark_tab.png",
                  width: 25,
                  height: 25,
                  fit: BoxFit.contain,
                  color: controller?.index == 2 ? TColor.primary : TColor.gray,
                ),
                text: "Đã lưu",
              ),
              Tab(
                icon: Image.asset(
                  "assets/img/my_profile_tab.png",
                  width: 25,
                  height: 25,
                  fit: BoxFit.contain,
                  color: controller?.index == 3 ? TColor.primary : TColor.gray,
                ),
                text: "Tôi",
              ),
            ]),
      ),
    );
  }
}
