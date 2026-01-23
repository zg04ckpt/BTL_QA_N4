import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/data/models/restaurant.dart'; // Import model Restaurant
import 'package:cp_restaurants/network/api_util.dart';

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
    return UserData(
      userId: data['id']?? '', // Assign snapshot.id as userId
      email: data['email'] ?? '',
      phoneNumber: data['phoneNumber'] ?? '',
      name: data['name'] ?? '',
      restaurantId: List<String>.from(data['restaurant_id'] ?? []),
      role: data['role'] ?? '',
      avtImage: '${APIService.instance.baseUrl}/${data['avtImage'] ?? ''}',
      state: data['status'] ?? 0,
      address: data['address'] == null
          ? null
          : Address.fromMap(data['address'] ?? {}),
      reports:
          data['reports'] != null ? List<String>.from(data['reports']) : null,
      reviews:
          data['reviews'] != null ? List<String>.from(data['reviews']) : null,
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
      'id':userId,
      'email': email,
      'phoneNumber': phoneNumber,
      'name': name,
      // 'restaurant_id': restaurantId,
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
      'id': userId,
      'email': email,
      'phoneNumber': phoneNumber,
      'name': name,
      'role': role,
      'avtImage': avtImage,
      'status': state,
      'address': address?.toJson(),
      'restaurantId': restaurantId.join(','),
      'reports': reports?.join(','),
      'reviews': reviews?.join(','),
      'restaurants': restaurants?.map((e) => e.toJson()).join(','), // Serialize
    };
  }

  // Convert Map to UserData
  factory UserData.fromMap(Map<String, dynamic> map) {
    return UserData(
      userId: map['id'],
      email: map['email'],
      phoneNumber: map['phoneNumber'],
      name: map['name'],
      role: map['role'],
      avtImage: map['avtImage'],
      state: map['state'] ?? 0,
      address: map['address'] != null
          ? Address.fromJson(map['address'])
          : null,
      restaurantId: map['restaurantId']?.split(',') ?? [],
      reports: map['reports']?.split(','),
      reviews: map['reviews']?.split(','),
      restaurants: map['restaurants'] != null
          ? (map['restaurants'] as List)
              .map((e) => Restaurant.fromJson(e))
              .toList()
          : null,
    );
  }
}
