import 'dart:math';

import '../global/global_data.dart';

class AppUtils {
  static double getRestaurantDistance(double lat, double lon) {
    if (GlobalData.instance.userPosition == null) {
      return 0;
    }

    double curlat = GlobalData.instance.userPosition!.latitude;
    double curlon = GlobalData.instance.userPosition!.longitude;

    const double R = 6371.0;

    double dLat = radians(lat - curlat);
    double dLon = radians(lon - curlon);
    double radLat1 = radians(curlat);
    double radLat2 = radians(lat);

    double a = sin(dLat / 2) * sin(dLat / 2) +
        cos(radLat1) * cos(radLat2) * sin(dLon / 2) * sin(dLon / 2);
    double c = 2 * atan2(sqrt(a), sqrt(1 - a));

    double distance = R * c;

    // Chỉ lấy 1 chữ số sau dấu phẩy
    return double.parse(distance.toStringAsFixed(1));
  }

  static double radians(double degree) {
    return degree * pi / 180;
  }
}
