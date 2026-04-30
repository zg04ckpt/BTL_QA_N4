import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/view/main_tab/main_tab_view.dart';
import 'package:cp_restaurants/view/restaurant_manager/components/add_new_res_view.dart';
import 'package:cp_restaurants/view/search/res_search_item.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../data/models/restaurant.dart';
import '../restaurant/restaurant_detail_view.dart';

class ManagerHomeView extends StatefulWidget {
  const ManagerHomeView({super.key});

  @override
  State<ManagerHomeView> createState() => _ManagerHomeViewState();
}

class _ManagerHomeViewState extends State<ManagerHomeView> {
  bool isLoading = false;
  List<String> restaurantId = [];
  List<Restaurant> myRes = [];

  @override
  void initState() {
    super.initState();
    getMyRestaurantsData();
  }

  Future<void> getMyRestaurantsData() async {
    setState(() {
      isLoading = true;
    });

    try {
      List<Restaurant> myRestaurants = [];
      final response = await APIService.instance.request(
          "/api/Restaurants/GetRestaurants?userId=${GlobalData.instance.userData?.userId}",
          DioMethod.get);
      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        myRestaurants =
            data.map((json) => Restaurant.fromJson(json)).toList();
      } else {
        throw Exception(
            "Failed to load restaurants. Status code: ${response.statusCode}");
      }
      setState(() {
        isLoading = false;
        myRes = myRestaurants;
      });
    } catch (e) {
      setState(() {
        isLoading = false;
      });
    }
  }

  Future<void> _openAddRestaurantPage() async {
    final result = await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => const AddRestaurantPage(),
      ),
    );
    if (result == true) {
      getMyRestaurantsData();
    }
  }

  Future<void> _goToMainTabHome() async {
    await context.read<CommonProvider>().setIsUseManagerOnly(false);
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute<void>(builder: (context) => const MainTabView()),
    );
  }

  @override
  Widget build(BuildContext context) {
    final canPop = Navigator.of(context).canPop();

    return Scaffold(
      floatingActionButton: FloatingActionButton(
        onPressed: _openAddRestaurantPage,
        child: const Icon(
          Icons.add_circle_outline_rounded,
          size: 30,
          color: Colors.red,
        ),
      ),
      appBar: AppBar(
        backgroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        elevation: 0.5,
        shadowColor: Colors.black26,
        foregroundColor: Colors.black,
        iconTheme: const IconThemeData(color: Colors.black),
        centerTitle: true,
        leadingWidth: 56,
        leading: IconButton(
          tooltip: canPop ? 'Quay lại' : 'Về trang chủ',
          padding: const EdgeInsetsDirectional.only(start: 8),
          icon: canPop
              ? Image.asset(
                  'assets/img/back.png',
                  width: 24,
                  height: 24,
                  fit: BoxFit.contain,
                )
              : Icon(Icons.home_rounded, color: TColor.primary, size: 28),
          onPressed: () {
            if (canPop) {
              Navigator.of(context).pop();
            } else {
              _goToMainTabHome();
            }
          },
        ),
        actions: [
          if (!canPop)
            TextButton(
              onPressed: _goToMainTabHome,
              style: TextButton.styleFrom(foregroundColor: TColor.primary),
              child: const Text(
                'Trang chủ',
                style: TextStyle(fontWeight: FontWeight.w700),
              ),
            ),
        ],
        title: const Text(
          'My Restaurant',
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: Colors.black,
            fontSize: 18,
          ),
        ),
      ),
      body: Container(
          padding: const EdgeInsets.all(16),
          height: double.infinity,
          width: double.infinity,
          child: isLoading
              ? const Center(
                  child: CircularProgressIndicator(),
                )
              : SizedBox(
                  child: ListView.builder(
                    shrinkWrap: true,
                    itemCount: myRes.length,
                    itemBuilder: (BuildContext context, int index) {
                      return InkWell(
                        onTap: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => RestaurantDetailView(
                              fObj: myRes[index],
                            ),
                          ),
                        ),
                        child: ResSearchItem(
                          isMyRes: true,
                          fObj: myRes[index],
                        ),
                      );
                    },
                  ),
                )),
    );
  }
}
