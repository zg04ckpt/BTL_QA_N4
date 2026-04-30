import 'package:flutter/material.dart';

extension CommonExtension on State {
  void endEditing() {
    FocusScope.of(context).requestFocus(FocusNode());
  }
}

extension DistanceFormatter on double {
  /// [AppUtils] uses **-1** when GPS/địa chỉ nhà hàng không hợp lệ.
  String toDistanceText() {
    if (this < 0) {
      return '—';
    }
    if (this < 1) {
      final meters = (this * 1000).round();
      return '${meters.toString()} m';
    }
    return '${toStringAsFixed(1)} km';
  }
}

extension TimeAgo on int {
  String toTimeAgo() {
    final now = DateTime.now();
    final dateTime = DateTime.fromMillisecondsSinceEpoch(this);
    final difference = now.difference(dateTime);

    if (difference.inMinutes < 1) {
      return "Vừa xong";
    } else if (difference.inMinutes < 60) {
      return "${difference.inMinutes} phút trước";
    } else if (difference.inHours < 24) {
      return "${difference.inHours} giờ trước";
    } else if (difference.inDays < 30) {
      return "${difference.inDays} ngày trước";
    } else if (difference.inDays < 365) {
      final months = (difference.inDays / 30).floor();
      return "$months tháng trước";
    } else {
      final years = (difference.inDays / 365).floor();
      return "$years năm trước";
    }
  }
}

