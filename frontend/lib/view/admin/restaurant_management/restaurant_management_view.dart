import 'package:cp_restaurants/view/admin/restaurant_management/admin_check_res_view.dart';
import 'package:cp_restaurants/view/restaurant/restaurant_detail_view.dart';
import 'package:flutter/material.dart';
import '../../../data/models/restaurant.dart';
import 'package:cp_restaurants/view/search/res_search_item.dart';

import '../../../global/global_data.dart';
import '../../../network/api_util.dart';

class RestaurantManagementView extends StatefulWidget {
  const RestaurantManagementView({super.key});

  @override
  State<RestaurantManagementView> createState() =>
      _RestaurantManagementViewState();
}

class _RestaurantManagementViewState extends State<RestaurantManagementView>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  bool isLoading = false;
  List<Restaurant> allRestaurants = [];
  List<Restaurant> openRestaurants = [];
  List<Restaurant> nonAuthenRestaurants = [];
  List<Restaurant> rejectRestaurants = [];
  List<Restaurant> closeRestaurants = [];

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 4, vsync: this);
    _fetchRestaurants();
  }

  Future<void> _fetchRestaurants() async {
    setState(() {
      isLoading = true;
    });

    final response = await APIService.instance.request(
        "/api/Restaurants/GetRestaurants",
        DioMethod.get);
    if (response.statusCode == 200) {
      List<dynamic> data = response.data as List<dynamic>;
      allRestaurants = data.map((json) => Restaurant.fromJson(json)).toList();
      nonAuthenRestaurants
        ..clear()
        ..addAll(allRestaurants.where((restaurant) => restaurant.status == 0));
      rejectRestaurants
        ..clear()
        ..addAll(allRestaurants.where((restaurant) => restaurant.status == 1));
      openRestaurants
        ..clear()
        ..addAll(allRestaurants.where((restaurant) => restaurant.status == 2));
      closeRestaurants
        ..clear()
        ..addAll(allRestaurants.where((restaurant) => restaurant.status == 3));

      setState(() {
        isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        centerTitle: true,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text("Quản lý nhà hàng"),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: "Mở cửa"),
            Tab(text: "Chưa xác thực"),
            Tab(text: "Từ chối"),
            Tab(text: "Đóng cửa"),

          ],
        ),
      ),
      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : TabBarView(
              controller: _tabController,
              children: [
                _buildRestaurantList(openRestaurants),
                _buildRestaurantList(nonAuthenRestaurants),
                _buildRestaurantList(rejectRestaurants),
                _buildRestaurantList(closeRestaurants),
              ],
            ),
    );
  }

  Widget _buildRestaurantList(List<Restaurant> restaurants) {
    if (restaurants.isEmpty) {
      return const Center(child: Text("No restaurants found"));
    }

    return ListView.builder(
      itemCount: restaurants.length,
      itemBuilder: (context, index) {
        final res = restaurants[index];
        return InkWell(
          onTap: () async {
            await Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => AdminCheckRes(
                  fObj: res,
                ),
              ),
            );
            _fetchRestaurants();
          },
          child: ResSearchItem(fObj: res),
        );
      },
    );
  }
}
