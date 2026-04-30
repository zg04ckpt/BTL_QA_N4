import 'dart:developer';

import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:cp_restaurants/common/app_utils.dart';
import 'package:cp_restaurants/data/models/category_model.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/data/repository/restaurant_helper.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/cupertino.dart';

class RestaurantProvider with ChangeNotifier {
  final FirebaseFirestore _firestore = FirebaseFirestore.instance;

  List<Restaurant> allAllRestaurant = [];
  List<Restaurant> topReviewRestaurant = [];
  List<Restaurant> searchedRestaurants = [];
  List<Restaurant> nearRestaurants = [];
  List<Restaurant> favoriteRestaurants = [];
  List<int> bookmarkRestaurants = [];

  bool isRefreshData = false;
  bool isLoadingHomeData = false;
  bool hasLoadedHomeData = false;
  String? homeLoadError;

  /// Max distance (km) when device location is known; otherwise all open restaurants are listed.
  static const double _nearRadiusKm = 200.0;
  bool isLoadingFavorites = false;
  String? favoriteLoadError;

  RestaurantHelper restaurantHelper = RestaurantHelper();
  static const String _bookmarkKey = 'bookmark_restaurants';

  Future<void> getBookmarkRestaurants() async {
    if (GlobalData.instance.userData == null) return;
    isLoadingFavorites = true;
    favoriteLoadError = null;
    notifyListeners();

    try {
      final response = await APIService.instance.request(
        '/api/Favorite/user/${GlobalData.instance.userData!.userId}',
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        favoriteRestaurants =
            data.map((json) => Restaurant.fromJson(json)).toList();
        bookmarkRestaurants = favoriteRestaurants.map((res) => res.id).toList();
      } else {
        favoriteLoadError =
            "Failed to fetch favorites: ${response.statusCode}";
      }
    } catch (e) {
      favoriteLoadError = e.toString();
      log('Error fetching bookmark restaurants: $e');
    } finally {
      isLoadingFavorites = false;
      notifyListeners();
    }
  }

  Future<void> setBookmarkRestaurants(Restaurant res,
      {bool? isAdd = true}) async {
    if (GlobalData.instance.userData == null) return;
    try {
      final response = await APIService.instance.request(
        '/api/Favorite/toggle?userId=${GlobalData.instance.userData!.userId}&restaurantId=${res.id}',
        DioMethod.post,
      );

      if (response.statusCode == 200) {
        // Toggle local state based on the current list
        if (bookmarkRestaurants.contains(res.id)) {
          bookmarkRestaurants.remove(res.id);
          favoriteRestaurants
              .removeWhere((restaurant) => restaurant.id == res.id);
          restaurantHelper.deleteRestaurant(res.id);
        } else {
          bookmarkRestaurants.add(res.id);
          favoriteRestaurants.add(res);
          restaurantHelper.insertRestaurant(res);
        }
        notifyListeners();
      }
    } catch (e) {
      log('Error toggling favorite: $e');
    }
  }

  bool isSearching = false;

  void init({bool force = false}) {
    if (!force && allAllRestaurant.isNotEmpty && isRefreshData == false) {
      return;
    }
    allAllRestaurant = [];
    nearRestaurants = [];
    topReviewRestaurant = [];
    isLoadingHomeData = true;
    hasLoadedHomeData = false;
    homeLoadError = null;
    notifyListeners();
    getNearRestaurants();
  }

  /// Re-run distance filter/sort after GPS becomes available (without refetching the API).
  void onUserLocationResolved() {
    if (allAllRestaurant.isEmpty) {
      if (!isLoadingHomeData) {
        init(force: true);
      }
      return;
    }
    _applyNearbyFilterAndSort();
    notifyListeners();
  }

  void _applyNearbyFilterAndSort() {
    nearRestaurants = List<Restaurant>.from(allAllRestaurant);
    for (int i = 0; i < nearRestaurants.length; i++) {
      nearRestaurants[i].distance = AppUtils.getRestaurantDistance(
        nearRestaurants[i].address.lat,
        nearRestaurants[i].address.lon,
      );
    }
    final hasPosition = GlobalData.instance.userPosition != null;
    nearRestaurants = nearRestaurants.where((restaurant) {
      if (restaurant.status != 2) return false;
      if (!hasPosition) return true;
      final d = restaurant.distance;
      if (d < 0) return true;
      return d <= _nearRadiusKm;
    }).toList();
    nearRestaurants.sort((a, b) =>
        AppUtils.compareDistanceKm(a.distance, b.distance));
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
        '/api/Restaurants/GetRestaurants?searchTerm=${Uri.encodeQueryComponent(query)}',
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
            searchedRestaurants[i].address.lon,
          );
          searchedRestaurants[i].distance = dis;
        }
        searchedRestaurants.sort((a, b) =>
            AppUtils.compareDistanceKm(a.distance, b.distance));
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
      // Do not filter by profile city here: mismatch with DB breaks "near you" entirely.
      final response = await APIService.instance.request(
          "/api/Restaurants/GetRestaurants",
          DioMethod.get);
      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        allAllRestaurant = data.map((json) => Restaurant.fromJson(json)).toList();
      } else {
        throw Exception(
            "Failed to load restaurants. Status code: ${response.statusCode}");
      }

      _applyNearbyFilterAndSort();
      await getTopReviewRestaurant();
      homeLoadError = null;
    } catch (e) {
      homeLoadError = e.toString();
      print("Error: $e");
    } finally {
      isLoadingHomeData = false;
      hasLoadedHomeData = true;
      notifyListeners();
    }
  }

  Future<void> getTopReviewRestaurant() async {
    try {
      topReviewRestaurant = List<Restaurant>.from(allAllRestaurant)
          .where((restaurant) =>
              restaurant.totalReviews > 0 && restaurant.status == 2)
          .toList()
        ..sort((a, b) => b.averageScore.compareTo(a.averageScore));
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

  Future<List<Restaurant>> getRestaurantByCategory(CategoryModel category) async {
    try {
      final response = await APIService.instance
          .request("/api/Restaurants/category/${category.id}", DioMethod.get);
      List<Restaurant> allRes = [];
      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        allRes = data.map((json) => Restaurant.fromJson(json)).toList();
      } else {
        return [];
      }
      var result = allRes.where((restaurant) {
        return restaurant.status == 2;
      }).toList();
      
      for (int i = 0; i < result.length; i++) {
        double dis = AppUtils.getRestaurantDistance(
          result[i].address.lat,
          result[i].address.lon,
        );
        result[i].distance = dis;
      }
      result.sort((a, b) =>
          AppUtils.compareDistanceKm(a.distance, b.distance));
      notifyListeners();
      return result;
    } catch (e) {
      print("Error: $e");
      return [];
    }
  }
}
