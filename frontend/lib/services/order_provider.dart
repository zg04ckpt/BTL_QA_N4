import 'dart:developer';
import 'package:cp_restaurants/data/models/order_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/material.dart';

class OrderProvider with ChangeNotifier {
  List<OrderData> userOrders = [];
  bool isLoading = false;

  Future<void> fetchUserOrders(int userId) async {
    isLoading = true;
    notifyListeners();

    try {
      final response = await APIService.instance.request(
        '/api/Orders/user/$userId',
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        userOrders = data.map((json) => OrderData.fromJson(json)).toList();
        // Sort by created time descending if possible
        userOrders.sort((a, b) => b.createdAt.compareTo(a.createdAt));
      }
    } catch (e) {
      log('Error fetching user orders: $e');
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }
}
