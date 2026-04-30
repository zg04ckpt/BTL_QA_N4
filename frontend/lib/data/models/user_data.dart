import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/data/models/restaurant.dart'; // Import model Restaurant
import 'package:cp_restaurants/network/api_mapper.dart';
import 'dart:convert';

class UserData {
  final int userId;
  final String email;
  final String phoneNumber;
  final String name;
  final List<String> restaurantId; // Giữ nguyên để ánh xạ ID các nhà hàng
  final String role;
  final String? avtImage;
  int state;
  final Address? address; // Địa chỉ người dùng
  final List<String>? reports; // Nullable
  final List<String>? reviews; // Nullable
  final List<Restaurant>? restaurants; // Thay đổi sang List<Restaurant>

  UserData({
    required this.userId,
    required this.email,
    required this.phoneNumber,
    required this.name,
    required this.restaurantId,
    required this.state,
    required this.role,
    this.avtImage,
    required this.address,
    this.reports,
    this.reviews,
    this.restaurants,
  });

  // Convert JSON to UserData
  factory UserData.fromJson(Map<String, dynamic> data) {
    List<String>? parseRefList(dynamic value) {
      if (value == null || value is! List) {
        return null;
      }
      return value
          .map((item) {
            if (item is Map<String, dynamic>) {
              final id = item['id'];
              return id?.toString() ?? '';
            }
            return item?.toString() ?? '';
          })
          .where((item) => item.isNotEmpty)
          .toList();
    }

    List<String> restaurantIdsFrom(dynamic v) {
      if (v == null) return [];
      if (v is List) {
        return v.map((e) => e.toString()).where((s) => s.isNotEmpty).toList();
      }
      final s = v.toString().trim();
      if (s.isEmpty) return [];
      return s.split(',').map((e) => e.trim()).where((e) => e.isNotEmpty).toList();
    }

    return UserData(
      userId: ApiMapper.asInt(
          data['id'] ?? data['Id'] ?? data['userId'] ?? data['UserId']),
      email: ApiMapper.asString(data['email']),
      phoneNumber: ApiMapper.asString(data['phoneNumber']),
      name: ApiMapper.asString(data['name']),
      restaurantId: restaurantIdsFrom(
          data['restaurant_id'] ?? data['restaurantId'] ?? data['restaurantIds']),
      role: ApiMapper.asString(data['role']),
      avtImage: ApiMapper.asMediaUrlOrNull(data['avtImage']),
      state: ApiMapper.asInt(data['status']),
      address: data['address'] == null
          ? null
          : Address.fromMap(data['address'] ?? {}),
      reports: parseRefList(data['reports']),
      reviews: parseRefList(data['reviews']),
      restaurants: data['restaurants'] != null
          ? (data['restaurants'] as List)
              .map((e) => Restaurant.fromJson(e))
              .toList()
          : null,
    );
  }

  // Convert UserData to JSON
  Map<String, dynamic> toJson() {
    return {
      'id': userId,
      'email': email,
      'phoneNumber': phoneNumber,
      'name': name,
      'restaurant_id': restaurantId,
      'role': role,
      'avtImage': avtImage ?? "",
      'status': state,
      'address': address?.toJson(),
      // 'reports': reports ?? [],
      // 'reviews': reviews ?? [],
      // 'restaurants': restaurants?.map((e) => e.toJson()).toList() ?? [],
    };
  }

  // Convert UserData to Map
  Map<String, dynamic> toMap() {
    return {
      'userId': userId,
      'email': email,
      'phoneNumber': phoneNumber,
      'name': name,
      'role': role,
      'avtImage': avtImage,
      'status': state,
      'address': address != null ? jsonEncode(address!.toJson()) : null,
      'restaurantId': restaurantId.join(','),
      'reports': reports?.join(','),
      'reviews': reviews?.join(','),
      'restaurants': restaurants != null
          ? jsonEncode(restaurants!.map((e) => e.toJson()).toList())
          : null,
    };
  }

  // Convert Map to UserData
  factory UserData.fromMap(Map<String, dynamic> map) {
    Address? parsedAddress;
    final rawAddress = map['address'];
    if (rawAddress is String && rawAddress.isNotEmpty) {
      try {
        parsedAddress = Address.fromMap(jsonDecode(rawAddress));
      } catch (_) {
        parsedAddress = Address.fromJson(rawAddress);
      }
    } else if (rawAddress is Map<String, dynamic>) {
      parsedAddress = Address.fromMap(rawAddress);
    }

    List<Restaurant>? parsedRestaurants;
    final rawRestaurants = map['restaurants'];
    if (rawRestaurants is String && rawRestaurants.isNotEmpty) {
      try {
        final decoded = jsonDecode(rawRestaurants);
        if (decoded is List) {
          parsedRestaurants =
              decoded.map((e) => Restaurant.fromJson(e)).toList();
        }
      } catch (_) {
        parsedRestaurants = null;
      }
    } else if (rawRestaurants is List) {
      parsedRestaurants = rawRestaurants.map((e) => Restaurant.fromJson(e)).toList();
    }

    List<String> parseCsv(dynamic value) {
      if (value == null) return [];
      if (value is List) {
        return value.map((e) => e.toString()).where((e) => e.isNotEmpty).toList();
      }
      final source = value.toString().trim();
      if (source.isEmpty) return [];
      return source.split(',').map((e) => e.trim()).where((e) => e.isNotEmpty).toList();
    }

    return UserData(
      userId: ApiMapper.asInt(map['userId'] ?? map['id']),
      email: map['email'] ?? '',
      phoneNumber: map['phoneNumber'] ?? '',
      name: map['name'] ?? '',
      role: map['role'] ?? '',
      avtImage: map['avtImage'],
      state: map['status'] ?? map['state'] ?? 0,
      address: parsedAddress,
      restaurantId: parseCsv(map['restaurantId']),
      reports: parseCsv(map['reports']),
      reviews: parseCsv(map['reviews']),
      restaurants: parsedRestaurants,
    );
  }
}
