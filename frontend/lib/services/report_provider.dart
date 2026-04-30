import 'dart:developer';
import 'package:cp_restaurants/data/models/report_model.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/material.dart';

class ReportProvider with ChangeNotifier {
  List<ReportModel> userReports = [];
  bool isLoading = false;

  Future<void> fetchUserReports(int userId) async {
    isLoading = true;
    notifyListeners();

    try {
      final response = await APIService.instance.request(
        '/api/Report/user/$userId',
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        userReports = data.map((json) => ReportModel.fromJson(json)).toList();
      }
    } catch (e) {
      log('Error fetching user reports: $e');
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }
}
