import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/view/restaurant_manager/components/add_new_res_view.dart';
import 'package:cp_restaurants/view/search/res_search_item.dart';
import 'package:flutter/material.dart';

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

  @override
  Widget build(BuildContext context) {
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
        centerTitle: true,
        title: const Text(
          "My Restaurant",
          style: TextStyle(fontWeight: FontWeight.bold),
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
              : ListView.builder(
                    padding: EdgeInsets.zero,
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
      ),
    );
  }
}
