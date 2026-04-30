import 'package:cp_restaurants/network/api_util.dart';

class ApiMapper {
  static int asInt(dynamic value, {int fallback = 0}) {
    if (value is int) return value;
    if (value is num) return value.toInt();
    if (value is String) return int.tryParse(value) ?? fallback;
    return fallback;
  }

  static double asDouble(dynamic value, {double fallback = 0.0}) {
    if (value is double) return value;
    if (value is num) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? fallback;
    return fallback;
  }

  static String asString(dynamic value, {String fallback = ''}) {
    if (value == null) return fallback;
    final text = value.toString();
    return text.isEmpty ? fallback : text;
  }

  static List<T> asList<T>(dynamic value, T Function(dynamic item) mapItem) {
    if (value is! List) return <T>[];
    return value.map(mapItem).toList();
  }

  static String? asMediaUrlOrNull(dynamic rawPath) {
    final path = asString(rawPath);
    if (path.isEmpty) return null;
    return APIService.instance.resolveMediaUrl(path);
  }
}
