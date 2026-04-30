import 'dart:math';

import '../global/global_data.dart';

class AppUtils {
  /// Km, 1 chữ số thập phân. Trả về **-1** nếu chưa có GPS hoặc tọa độ nhà hàng không dùng được
  /// (tránh mặc định 1,1° hoặc 0,0 gây km sai / "0 m" cho mọi nhà hàng).
  static double getRestaurantDistance(double lat, double lon) {
    if (GlobalData.instance.userPosition == null) {
      return -1;
    }
    if (!_isPlausibleWgs84Point(lat, lon)) {
      return -1;
    }

    final curlat = GlobalData.instance.userPosition!.latitude;
    final curlon = GlobalData.instance.userPosition!.longitude;
    if (!_isPlausibleWgs84Point(curlat, curlon)) {
      return -1;
    }

    const double R = 6371.0;

    final dLat = radians(lat - curlat);
    final dLon = radians(lon - curlon);
    final radLat1 = radians(curlat);
    final radLat2 = radians(lat);

    final a = sin(dLat / 2) * sin(dLat / 2) +
        cos(radLat1) * cos(radLat2) * sin(dLon / 2) * sin(dLon / 2);
    final c = 2 * atan2(sqrt(a), sqrt(1 - a));

    final distance = R * c;
    return double.parse(distance.toStringAsFixed(1));
  }

  /// Bỏ placeholder (1,1), gần (0,0), và giá trị nằm ngoài WGS84.
  static bool _isPlausibleWgs84Point(double lat, double lon) {
    if (lat == 1 && lon == 1) return false;
    if (lat.abs() < 1e-7 && lon.abs() < 1e-7) return false;
    if (lat.abs() > 90 || lon.abs() > 180) return false;
    return true;
  }

  static double radians(double degree) {
    return degree * pi / 180;
  }

  /// Sắp xếp theo km; khoảng cách không xác định (-1) đưa xuống cuối.
  static int compareDistanceKm(double a, double b) {
    if (a < 0 && b < 0) return 0;
    if (a < 0) return 1;
    if (b < 0) return -1;
    return a.compareTo(b);
  }
}
