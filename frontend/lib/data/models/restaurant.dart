import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/network/api_mapper.dart';

class Restaurant {
  final int id;
  final String name;
  final int status;
  final String email;
  final String description;
  final String phoneNumber;
  final String avtImage;
  final int cateId;
  final int userId;
  final Address address;
  final List<String> photoUrls;
  final double averageScore;
  final int totalReviews;
  double distance;
  final String category;

  Restaurant({
    this.id = -1,
    required this.name,
    required this.status,
    required this.email,
    required this.description,
    required this.phoneNumber,
    required this.avtImage,
    required this.cateId,
    required this.userId,
    required this.address,
    required this.photoUrls,
    this.averageScore = 0.0,
    this.totalReviews = 0,
    this.distance = 0,
    this.category = "",
  });

  // Convert JSON to Restaurant
  factory Restaurant.fromJson(Map<String, dynamic> json) {
    return Restaurant(
      id: ApiMapper.asInt(json['id']),
      name: ApiMapper.asString(json['name']),
      status: ApiMapper.asInt(json['status']),
      email: ApiMapper.asString(json['email']),
      description: ApiMapper.asString(json['description']),
      phoneNumber: ApiMapper.asString(json['phoneNumber']),
      avtImage: ApiMapper.asMediaUrlOrNull(json['avtImage']) ?? '',
      cateId: ApiMapper.asInt(json['cateId']),
      userId: ApiMapper.asInt(json['userId']),
      address: Address.fromMap(json['address'] ?? {}),
      photoUrls: (json['restaurantPhotos'] as List<dynamic>?)
              ?.map((e) => ApiMapper.asMediaUrlOrNull(e) ?? '')
              .toList() ??
          [],
      averageScore: ApiMapper.asDouble(json['averageScore']),
      totalReviews: ApiMapper.asInt(json['totalReviews']),
      category: ApiMapper.asString(json['category']),
    );
  }

  // Convert Restaurant to JSON
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'status': status,
      'email': email,
      'description': description,
      'phoneNumber': phoneNumber,
      'avtImage': avtImage,
      'cateId': cateId,
      'userId': userId,
      'address': address.toJson(),
      'restaurantPhotos': photoUrls,
      'averageScore': averageScore,
      'totalReviews': totalReviews,
    };
  }

  Map<String, dynamic> toDB() {
    return {
      'id': id,
      'name': name,
      'status': status,
      'email': email,
      'description': description,
      'phoneNumber': phoneNumber,
      'avtImage': avtImage,
      'cateId': cateId,
      'userId': userId,
      'address': address.toJson(), // Convert Address to JSON
      'photoUrls':
          photoUrls.join(','), // Join List<String> to comma-separated String
      'averageScore': averageScore,
      'totalReviews': totalReviews,
      'distance': distance,
      'category': category,
    };
  }

  /// Create a `Restaurant` from a Map (SQLite row)
  factory Restaurant.fromDB(Map<String, dynamic> dbData) {
    return Restaurant(
      id: dbData['id'],
      name: dbData['name'],
      status: dbData['status'],
      email: dbData['email'],
      description: dbData['description'],
      phoneNumber: dbData['phoneNumber'],
      avtImage: dbData['avtImage'],
      cateId: dbData['cateId'],
      userId: dbData['userId'],
      address: Address.fromJson(dbData['address']), // Parse Address from JSON
      photoUrls: dbData['photoUrls']?.split(',') ??
          [], // Split String back to List<String>
      averageScore: dbData['averageScore']?.toDouble() ?? 0.0,
      totalReviews: dbData['totalReviews'] ?? 0,
      distance: dbData['distance']?.toDouble() ?? 0.0,
      category: dbData['category'],
    );
  }

  Restaurant copyWith({
    int? id,
    String? name,
    int? status,
    String? email,
    String? description,
    String? phoneNumber,
    String? avtImage,
    int? cateId,
    int? userId,
    Address? address,
    List<String>? photoUrls,
    double? averageScore,
    int? totalReviews,
    double? distance,
    String? category,
  }) {
    return Restaurant(
      id: id ?? this.id,
      name: name ?? this.name,
      status: status ?? this.status,
      email: email ?? this.email,
      description: description ?? this.description,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      avtImage: avtImage ?? this.avtImage,
      cateId: cateId ?? this.cateId,
      userId: userId ?? this.userId,
      address: address ?? this.address,
      photoUrls: photoUrls ?? this.photoUrls,
      averageScore: averageScore ?? this.averageScore,
      totalReviews: totalReviews ?? this.totalReviews,
      distance: distance ?? this.distance,
      category: category ?? this.category,
    );
  }
}
