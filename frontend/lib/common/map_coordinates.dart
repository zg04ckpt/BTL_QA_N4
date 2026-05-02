import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';

/// Tọa độ mặc định trong [Address] khi chưa set (gần 1,1 — không dùng làm map).
bool isValidMapCoordinate(double lat, double lon) {
  if (lat.isNaN || lon.isNaN) return false;
  if (lat.abs() > 90 || lon.abs() > 180) return false;
  if (lat == 0 && lon == 0) return false;
  if ((lat - 1.0).abs() < 1e-9 && (lon - 1.0).abs() < 1e-9) return false;
  return true;
}

/// Trung tâm bản đồ: ưu tiên tọa độ nhà hàng, sau đó GPS người dùng, cuối cùng Hà Nội.
LatLng resolveMapCenter({
  required double restaurantLat,
  required double restaurantLon,
  Position? userPosition,
}) {
  if (isValidMapCoordinate(restaurantLat, restaurantLon)) {
    return LatLng(restaurantLat, restaurantLon);
  }
  if (userPosition != null &&
      isValidMapCoordinate(userPosition.latitude, userPosition.longitude)) {
    return LatLng(userPosition.latitude, userPosition.longitude);
  }
  return const LatLng(21.028511, 105.804817);
}

bool usedRestaurantCoordsForMap(double restaurantLat, double restaurantLon) {
  return isValidMapCoordinate(restaurantLat, restaurantLon);
}
