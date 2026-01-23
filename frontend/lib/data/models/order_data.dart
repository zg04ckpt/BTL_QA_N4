class OrderData {
  int id;
  String name;
  String phoneNumber;
  String email;
  int userId;
  int restaurantId;
  int numOfMembers;
  String reservationTime;
  String specialRequest;
  int createdAt;
  int status;

  OrderData({
    this.id = 0,
    required this.name,
    required this.phoneNumber,
    required this.email,
    required this.userId,
    required this.restaurantId,
    required this.numOfMembers,
    required this.reservationTime,
    required this.specialRequest,
    required this.createdAt,
    this.status = 0,
  });

  // Phương thức chuyển từ JSON sang OrderData
  factory OrderData.fromJson(Map<String, dynamic> json) {
    return OrderData(
      id: json['id'] as int,
      name: json['name'] as String,
      phoneNumber: json['phoneNumber'] as String,
      email: json['email'] as String,
      userId: json['userId'] as int,
      restaurantId: json['restaurantId'] as int,
      numOfMembers: json['numOfMembers'] as int,
      reservationTime: json['reservationTime'] as String,
      specialRequest: json['specialRequest'] as String,
      createdAt: json['createdAt'] as int,
      status: json['status'] as int
    );
  }

  // Phương thức chuyển từ OrderData sang JSON
  Map<String, dynamic> toJson() {
    return {
      'name': name,
      'phoneNumber': phoneNumber,
      'email': email,
      'userId': userId,
      'restaurantId': restaurantId,
      'numOfMembers': numOfMembers,
      'reservationTime': reservationTime,
      'specialRequest': specialRequest,
      'createdAt': createdAt,
    };
  }
}
