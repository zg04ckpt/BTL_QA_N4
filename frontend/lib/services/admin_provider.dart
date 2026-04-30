import 'dart:developer';
import 'package:cp_restaurants/data/models/report_model.dart';
import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/material.dart';

class AdminProvider with ChangeNotifier {
  List<UserData> allUsers = [];
  List<ReportModel> allReports = [];
  bool isLoading = false;

  Future<void> fetchAllUsers() async {
    isLoading = true;
    notifyListeners();

    try {
      final response = await APIService.instance.request(
        '/api/User/GetAllUsers',
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        allUsers = data.map((json) => UserData.fromJson(json)).toList();
      }
    } catch (e) {
      log('Error fetching all users: $e');
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> updateUserStatus(int userId, int status) async {
    try {
      // We need the full user object to update because the API uses UserUpdateDTO
      final user = allUsers.firstWhere((u) => u.userId == userId);
      
      final updateData = {
        'name': user.name,
        'phoneNumber': user.phoneNumber,
        'avtImage': user.avtImage,
        'status': status,
        'address': user.address?.toJson(),
      };

      final response = await APIService.instance.request(
        '/api/User/UpdateUser/$userId',
        DioMethod.put,
        param: updateData,
      );

      if (response.statusCode == 200) {
        // Update local state
        final index = allUsers.indexWhere((u) => u.userId == userId);
        if (index != -1) {
          allUsers[index].state = status;
          notifyListeners();
        }
        return true;
      }
      return false;
    } catch (e) {
      log('Error updating user status: $e');
      return false;
    }
  }

  Future<void> fetchAllReports() async {
    isLoading = true;
    notifyListeners();

    try {
      final response = await APIService.instance.request(
        '/api/Report/all',
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        allReports = data.map((json) => ReportModel.fromJson(json)).toList();
      }
    } catch (e) {
      log('Error fetching all reports: $e');
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> resolveReport(int reportId, int status, {int? reviewId}) async {
    try {
      final response = await APIService.instance.request(
        '/api/Report/$reportId/status',
        DioMethod.put,
        param: status,
      );

      if (response.statusCode == 200) {
        // If status is 1 (Deleted), we might want to delete the review too if reviewId is provided
        if (status == 1 && reviewId != null) {
          await APIService.instance.request(
            '/api/Review/$reviewId',
            DioMethod.delete,
          );
        }

        // Update local state
        final index = allReports.indexWhere((r) => r.id == reportId);
        if (index != -1) {
          allReports[index] = ReportModel(
            id: allReports[index].id,
            userId: allReports[index].userId,
            reviewId: allReports[index].reviewId,
            reason: allReports[index].reason,
            status: status,
          );
          notifyListeners();
        }
        return true;
      }
      return false;
    } catch (e) {
      log('Error resolving report: $e');
      return false;
    }
  }
}
