import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:dio/dio.dart';
import 'package:flutter/cupertino.dart';

class ReviewProvider with ChangeNotifier {
  List<ReviewModel> reviewModels = [];

  bool isLoadingReview = false;

  Future getReviewsByResId(int resId,
      {bool isNearest = true, int? star}) async {
    reviewModels = [];

    var response = await APIService.instance.request(
      '/api/reviews/by-restaurant/$resId',
      DioMethod.get,
    );

    if (response.data != null && response.data is List) {
      reviewModels = (response.data as List)
          .map((json) => ReviewModel.fromMap(json as Map<String, dynamic>))
          .toList();
      if (star != null) {
        reviewModels = reviewModels.where((review) {
          return review.rate == star;
        }).toList();
      }
      if (isNearest) {
        reviewModels.sort((a, b) => a.createDate.compareTo(b.createDate));
      } else {
        reviewModels.sort((a, b) => b.createDate.compareTo(a.createDate));
      }
    } else {
      reviewModels = [];
    }

    isLoadingReview = false;
    notifyListeners();
  }

  /// Xóa đánh giá trên server (`DELETE /api/reviews/{id}`). Cascade báo cáo & ảnh do backend xử lý.
  Future<bool> deleteReview(int reviewId) async {
    try {
      final response = await APIService.instance.request(
        '/api/reviews/$reviewId',
        DioMethod.delete,
      );
      return response.statusCode == 200;
    } on DioException catch (_) {
      return false;
    } catch (_) {
      return false;
    }
  }

  List<ReviewModel> reviewHistoryModels = [];

  Future<List<ReviewModel>> getUserReviews() async {
    List<ReviewModel> result = [];

    try {
      final response = await APIService.instance.request(
          "/api/reviews/by-user/${GlobalData.instance.userData!.userId}",
          DioMethod.get);
      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        result = data.map((json) => ReviewModel.fromMap(json)).toList();
      } else {
        throw Exception(
            "Failed to load restaurants. Status code: ${response.statusCode}");
      }
      notifyListeners();
    } catch (e) {
      print("Failed to fetch user reviews: $e");
    }
    return result;
  }
}
