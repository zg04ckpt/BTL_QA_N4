import 'dart:developer';

import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:cp_restaurants/common/app_utils.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/data/repository/restaurant_helper.dart';
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

  /// Đang tải dữ liệu trang Home (gần bạn + top đánh giá).
  bool isLoadingHomeData = false;

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
        '/api/Restaurants/GetRestaurants',
        DioMethod.get,
        param: {'searchTerm': query},
      );

      if (response.statusCode == 200) {
        List<dynamic> datas = response.data as List<dynamic>;
        searchedRestaurants =
            datas.map((json) => Restaurant.fromJson(json)).toList();
        searchedRestaurants = searchedRestaurants
            .where((restaurant) => restaurant.status == 2)
            .toList();
        for (int i = 0; i < searchedRestaurants.length; i++) {
          double dis = AppUtils.getRestaurantDistance(
            searchedRestaurants[i].address.lat,
            searchedRestaurants[i].address.lon,
          );
          searchedRestaurants[i].distance = dis;
        }
        searchedRestaurants.sort((a, b) => a.distance.compareTo(b.distance));
      } else {
        print('Failed to fetch restaurants: ${response.statusCode}');
        searchedRestaurants = [];
      }
    } catch (e) {
      print('Error searching restaurants: $e');
      searchedRestaurants = [];
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

  /// Bán kính "gần bạn" (km). [AppUtils.getRestaurantDistance] trả về km.
  static const double maxNearbyDistanceKm = 100;

  Future<void> getNearRestaurants() async {
    isLoadingHomeData = true;
    notifyListeners();
    try {
      // Không gửi `city`: backend so khớp chính xác (`Address.City == city`) —
      // chuỗi từ profile (vd. "TP.HCM") thường không khớp DB → API rỗng.
      // Lọc khoảng cách phía client sau khi có GPS.
      final response = await APIService.instance.request(
        '/api/Restaurants/GetRestaurants',
        DioMethod.get,
        param: null,
      );
      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        final fromApi =
            data.map((json) => Restaurant.fromJson(json)).toList();

        _syncTopReviewRestaurant(fromApi);

        nearRestaurants = List<Restaurant>.from(fromApi);
      } else {
        throw Exception(
            "Failed to load restaurants. Status code: ${response.statusCode}");
      }

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
              restaurant.distance <= maxNearbyDistanceKm &&
              restaurant.status == 2)
          .toList();
      nearRestaurants.sort((a, b) => a.distance.compareTo(b.distance));
    } catch (e) {
      print("Error: $e");
      nearRestaurants = [];
      topReviewRestaurant = [];
    } finally {
      isLoadingHomeData = false;
      notifyListeners();
    }
  }

  /// Top đánh giá: lấy từ cùng nguồn API với “gần bạn” (trước lọc khoảng cách).
  /// Trước đây lọc từ [allAllRestaurant] luôn rỗng → grid “Đánh giá cao nhất” không có dữ liệu.
  void _syncTopReviewRestaurant(List<Restaurant> source) {
    topReviewRestaurant = source.where((r) => r.status == 2).toList()
      ..sort((a, b) {
        final byScore = b.averageScore.compareTo(a.averageScore);
        if (byScore != 0) return byScore;
        return b.totalReviews.compareTo(a.totalReviews);
      });
    if (topReviewRestaurant.length > 24) {
      topReviewRestaurant = topReviewRestaurant.sublist(0, 24);
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
