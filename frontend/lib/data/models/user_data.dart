import 'dart:convert';

import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';

/// Khớp API ASP.NET (camelCase): id, email, role, name, phoneNumber, avtImage, status, address.
class UserData {
  final int userId;
  final String email;
  final String phoneNumber;
  final String name;
  final List<String> restaurantId;
  final String role;
  final String? avtImage;
  int state;
  final Address? address;
  final List<String>? reports;
  final List<String>? reviews;
  final List<Restaurant>? restaurants;

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

  static List<String>? _stringListOrNull(dynamic v) {
    if (v == null) return null;
    if (v is List) return v.map((e) => e.toString()).toList();
    return null;
  }

  static int _parseId(dynamic v) {
    if (v == null) return 0;
    if (v is int) return v;
    return int.tryParse(v.toString()) ?? 0;
  }

  factory UserData.fromJson(Map<String, dynamic> data) {
    final rid = data['restaurant_id'] ?? data['restaurantId'];
    List<String> restaurantIds = [];
    if (rid is List) {
      restaurantIds = rid.map((e) => e.toString()).toList();
    }

    Address? addr;
    final rawAddr = data['address'];
    if (rawAddr is Map<String, dynamic>) {
      addr = Address.fromMap(rawAddr);
    }

    final avt = data['avtImage']?.toString();
    return UserData(
      userId: _parseId(data['id']),
      email: data['email']?.toString() ?? '',
      phoneNumber: data['phoneNumber']?.toString() ?? '',
      name: data['name']?.toString() ?? '',
      restaurantId: restaurantIds,
      role: data['role']?.toString() ?? '',
      avtImage: (avt != null && avt.isNotEmpty) ? avt : null,
      state: _parseId(data['status'] ?? data['state']),
      address: addr,
      reports: _stringListOrNull(data['reports']),
      reviews: _stringListOrNull(data['reviews']),
      restaurants: data['restaurants'] != null
          ? (data['restaurants'] as List)
              .map((e) => Restaurant.fromJson(e as Map<String, dynamic>))
              .toList()
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': userId,
      'email': email,
      'phoneNumber': phoneNumber,
      'name': name,
      'role': role,
      'avtImage': avtImage ?? '',
      'status': state,
      'address': address?.toJson(),
    };
  }

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

  factory UserData.fromMap(Map<String, dynamic> map) {
    Address? addr;
    final rawAddr = map['address'];
    if (rawAddr != null) {
      if (rawAddr is String && rawAddr.isNotEmpty) {
        try {
          final decoded = jsonDecode(rawAddr);
          if (decoded is Map<String, dynamic>) {
            addr = Address.fromMap(decoded);
          }
        } catch (_) {}
      } else if (rawAddr is Map<String, dynamic>) {
        addr = Address.fromMap(rawAddr);
      }
    }

    return UserData(
      userId: _parseId(map['userId'] ?? map['id']),
      email: map['email']?.toString() ?? '',
      phoneNumber: map['phoneNumber']?.toString() ?? '',
      name: map['name']?.toString() ?? '',
      role: map['role']?.toString() ?? '',
      avtImage: map['avtImage']?.toString(),
      state: _parseId(map['state'] ?? map['status']),
      address: addr,
      restaurantId: (map['restaurantId']?.toString() ?? '').isEmpty
          ? []
          : map['restaurantId'].toString().split(',').where((s) => s.isNotEmpty).toList(),
      reports: map['reports']?.toString().split(','),
      reviews: map['reviews']?.toString().split(','),
      restaurants: _parseEmbeddedRestaurants(map['restaurants']),
    );
  }

  static List<Restaurant>? _parseEmbeddedRestaurants(dynamic raw) {
    if (raw == null) return null;
    try {
      final s = raw.toString();
      if (s.isEmpty) return null;
      final d = jsonDecode(s);
      if (d is List) {
        return d
            .map((e) => Restaurant.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      }
    } catch (_) {}
    return null;
  }
}
