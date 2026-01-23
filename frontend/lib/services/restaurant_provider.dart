import 'dart:developer';

import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:cp_restaurants/common/app_utils.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/data/repository/restaurant_helper.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/cupertino.dart';
import 'package:shared_preferences/shared_preferences.dart';

class RestaurantProvider with ChangeNotifier {
  final FirebaseFirestore _firestore = FirebaseFirestore.instance;

  List<Restaurant> allAllRestaurant = [];
  List<Restaurant> topReviewRestaurant = [];
  List<Restaurant> searchedRestaurants = [];
  List<Restaurant> nearRestaurants = [];
  List<int> bookmarkRestaurants = [];

  bool isRefreshData = false;

  RestaurantHelper restaurantHelper = RestaurantHelper();
  static const String _bookmarkKey = 'bookmark_restaurants';

  Future<void> getBookmarkRestaurants() async {
    final SharedPreferences sharedPreferences =
        await SharedPreferences.getInstance();

    List<String>? storedList = sharedPreferences.getStringList(_bookmarkKey);
    if (storedList != null) {
      bookmarkRestaurants =
          storedList.map((e) => int.tryParse(e) ?? 0).toList();
    }
  }

  Future<void> setBookmarkRestaurants(Restaurant res,
      {bool? isAdd = true}) async {
    if (isAdd!) {
      restaurantHelper.insertRestaurant(res);
      List<int> newList = [];
      newList = bookmarkRestaurants;
      newList.add(res.id);
      bookmarkRestaurants = [];
      bookmarkRestaurants.addAll(newList);
    } else {
      restaurantHelper.deleteRestaurant(res.id);

      List<int> newList = [];
      newList = bookmarkRestaurants;
      newList.remove(res.id);
      bookmarkRestaurants = [];
      bookmarkRestaurants.addAll(newList);
    }
    notifyListeners();
    final SharedPreferences sharedPreferences =
        await SharedPreferences.getInstance();

    List<String> stringList =
        bookmarkRestaurants.map((e) => e.toString()).toList();
    await sharedPreferences.setStringList(_bookmarkKey, stringList);
  }

  bool isSearching = false;

  void init() {
    if (allAllRestaurant.isNotEmpty && isRefreshData == false) {
      return;
    }
    allAllRestaurant = [];
    getNearRestaurants();
  }

  Future<void> searchRestaurants(String query) async {
    isSearching = true;
    notifyListeners();

    if (query.isEmpty) {
      searchedRestaurants = [];
      isSearching = false;
      notifyListeners();
      return;
    }

    try {
      final response = await APIService.instance.request(
        '/api/Restaurants/GetRestaurants?searchTerm=$query',
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        List<dynamic> datas = response.data as List<dynamic>;
        print(datas);

        searchedRestaurants =
            datas.map((json) => Restaurant.fromJson(json)).toList();
        searchedRestaurants = searchedRestaurants
            .where((restaurant) => restaurant.status == 2)
            .toList();
        for (int i = 0; i < searchedRestaurants.length; i++) {
          double dis = AppUtils.getRestaurantDistance(
            searchedRestaurants[i].address.lat,
            searchedRestaurants[i].address.lat,
          );
          searchedRestaurants[i].distance = dis;
        }
        nearRestaurants.sort((a, b) => a.distance.compareTo(b.distance));
      } else {
        print('Failed to fetch restaurants: ${response.statusCode}');
      }
    } catch (e) {
      print('Error searching restaurants: $e');
    } finally {
      isSearching = false;
      notifyListeners();
    }
  }

  Future<void> updateRestaurantData(String resId, double reviewRate,
      {bool isDeleteReview = false}) async {
    final DocumentReference restaurantRef =
        _firestore.collection('restaurants').doc(resId);

    _firestore.runTransaction((transaction) async {
      DocumentSnapshot snapshot = await transaction.get(restaurantRef);

      if (!snapshot.exists) {
        throw Exception("Restaurant does not exist!");
      }

      int currentQuantity = snapshot.get('quantity');
      double currentAverage = snapshot.get('average').toDouble();

      if (isDeleteReview) {
        if (currentQuantity > 1) {
          int newQuantity = currentQuantity - 1;
          double newAverage =
              ((currentAverage * currentQuantity) - reviewRate) / newQuantity;
          transaction.update(restaurantRef, {
            'quantity': newQuantity,
            'average': newAverage,
          });
        } else {
          transaction.update(restaurantRef, {
            'quantity': 0,
            'average': 0.0,
          });
        }
      } else {
        int newQuantity = currentQuantity + 1;
        double newAverage =
            ((currentAverage * currentQuantity) + reviewRate) / newQuantity;
        transaction.update(restaurantRef, {
          'quantity': newQuantity,
          'average': newAverage,
        });
      }
    }).then((_) {
      print("Restaurant data updated successfully");
    }).catchError((error) {
      print("Failed to update restaurant data: $error");
    });
  }

  Future<Restaurant?> getRestaurantById(int resId) async {
    final response = await APIService.instance
        .request("/api/Restaurants/$resId", DioMethod.get);
    Restaurant? result;
    if (response.statusCode == 200) {
      result = Restaurant.fromJson(response.data);
    } else {
      throw Exception(
          "Failed to load restaurants. Status code: ${response.statusCode}");
    }
    return result;
  }

  Future<void> getNearRestaurants() async {
    try {
      final response = await APIService.instance.request(
          "/api/Restaurants/GetRestaurants?city=${GlobalData.instance.userData!.address?.city}",
          DioMethod.get);
      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        nearRestaurants =
            data.map((json) => Restaurant.fromJson(json)).toList();
      } else {
        throw Exception(
            "Failed to load restaurants. Status code: ${response.statusCode}");
      }

      getTopReviewRestaurant();

      nearRestaurants.addAll(allAllRestaurant);

      for (int i = 0; i < nearRestaurants.length; i++) {
        double dis = AppUtils.getRestaurantDistance(
          nearRestaurants[i].address.lat,
          nearRestaurants[i].address.lon,
        );
        nearRestaurants[i].distance = dis;
      }
      nearRestaurants = nearRestaurants
          .where((restaurant) =>
              restaurant.distance <= 100000 && restaurant.status == 2)
          .toList();
      nearRestaurants.sort((a, b) => a.distance.compareTo(b.distance));
      notifyListeners();
    } catch (e) {
      print("Error: $e");
    }
  }

  Future<void> getTopReviewRestaurant() async {
    try {
      topReviewRestaurant = List<Restaurant>.from(allAllRestaurant)
          .where((restaurant) =>
              restaurant.totalReviews > 100 && restaurant.status == 2)
          .toList()
        ..sort((a, b) => b.averageScore.compareTo(a.averageScore));

      notifyListeners();

      notifyListeners();
    } catch (e) {
      print("Error: $e");
    }
  }

  Future<bool> createRestaurant(Map<String, dynamic> restaurantData) async {
    try {
      final response = await APIService.instance.request(
        '/api/Restaurants',
        DioMethod.post,
        param: restaurantData,
      );
      if (response.statusCode == 201) {
        log('Tạo nhà hàng thành công: ${response.data}');
        return true;
      } else {
        return false;
      }
    } catch (e) {
      log('Error creating restaurant: $e');
      return false;
    }
  }

  Future<bool> editRestaurant(Restaurant res) async {
    log(res.toJson().toString());
    final response = await APIService.instance.request(
      '/api/Restaurants/update/${res.id}',
      DioMethod.put,
      param: res.toJson(),
    );
    if (response.statusCode == 200) {
      log('Tạo nhà hàng thành công: ${response.data}');
      return true;
    } else {
      return false;
    }
  }

  Future<List<Restaurant>> getRestaurantByCategory(String category) async {
    try {
      final response = await APIService.instance
          .request("/api/Restaurants/GetRestaurants", DioMethod.get);
      List<Restaurant> allRes = [];
      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        allRes = data.map((json) => Restaurant.fromJson(json)).toList();
      } else {
        return [];
      }
      var result = allRes.where((restaurant) {
        return restaurant.category == category && restaurant.status == 2;
      }).toList();
      nearRestaurants.sort((a, b) => a.distance.compareTo(b.distance));
      notifyListeners();
      return result;
    } catch (e) {
      print("Error: $e");
      return [];
    }
  }
}
