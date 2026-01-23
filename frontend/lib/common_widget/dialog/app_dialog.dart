import 'package:cp_restaurants/common_widget/dialog/delete_confirm_dialog.dart';
import 'package:cp_restaurants/common_widget/dialog/image_preview_dialog.dart';
import 'package:cp_restaurants/common_widget/dialog/rating_dialog.dart';
import 'package:cp_restaurants/common_widget/dialog/report_dialog.dart';
import 'package:cp_restaurants/common_widget/dialog/res_type_dialog.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/view/order/order_bottom_sheet.dart';
import 'package:flutter/material.dart';
import 'package:modal_bottom_sheet/modal_bottom_sheet.dart';

class AppDialog {
  static Future<void> showRatingDialog(BuildContext context,
      {required String resName,
      required int resId,
      ReviewModel? initReview,
      required VoidCallback onSubmitedReview}) async {
    await showDialog(
      context: context,
      barrierDismissible: true, // set to false if you want to force a rating
      builder: (context) => ReviewDialog(
        restaurantName: resName,
        resId: resId,
        initReview: initReview,
        onSubmitedReview: onSubmitedReview,
      ),
    );
  }

  static void showDeleteConfirmationDialog(BuildContext context,
      {required String deleteContent, required VoidCallback onDelete}) {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return deleteConfirmDialog(context,
            deleteContent: deleteContent, onDelete: onDelete);
      },
    );
  }

  static void showReportDialog(BuildContext context,
      {required int resId, required int reviewId}) {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return ReportDialog(resId: resId, reviewId: reviewId);
      },
    );
  }

  static void showResTypeDialog(BuildContext context,
      {required Function(String) onConfirm, required String initType}) {
    showDialog(
        context: context,
        builder: (BuildContext context) {
          return ResTypeDialog(
            onConfirm: onConfirm,
            initType: initType,
          );
        });
  }

  static void confirmUpdateState(BuildContext context,
      {required int state, required Function onUpdate}) {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: const Text("Xác nhận hành động"),
          content: Text(
            state == 1
                ? "Bạn có chắc chấp nhận mở cửa nhà hàng này"
                : state == 3
                    ? "Bạn có chắc chẵn đóng cửa nhà hàng này?"
                    : "Bạn có chắc chắn từ chối nhà hàng này?",
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop(); // Đóng dialog nếu chọn 'Hủy'
              },
              child: const Text("Huỷ"),
            ),
            TextButton(
              onPressed: () {
                Navigator.of(context).pop(); // Đóng dialog sau khi xác nhận
                onUpdate();
              },
              child: const Text("Xác nhận"),
            ),
          ],
        );
      },
    );
  }

  static void showOrderModalBottomSheet(BuildContext context,{required int resId}) {
    showMaterialModalBottomSheet(
      context: context,
      expand: false,
      backgroundColor: Colors.transparent,
      builder: (context) =>  OrderBottomSheet(resId: resId,),
    );
  }

  static void showPreviewImage(BuildContext context, String url) {
    showDialog(
        context: context,
        builder: (_) => ImagePreviewDialog(
              imageUrl: url,
            ));
  }
}
