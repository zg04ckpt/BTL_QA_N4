class ReviewModel {
  int? id;
  List<String> imageUrls;
  double rate;
  int resId;
  String review;
  String userName;
  String restaurantName;
  int userId;
  int createDate;
  int reportCount;

  ReviewModel({
    this.id,
    required this.imageUrls,
    required this.rate,
    required this.resId,
    required this.review,
    required this.userName,
    required this.userId,
    required this.createDate,
    this.restaurantName = "",
    this.reportCount = 0,
  });

  Map<String, dynamic> toJson() {
    return {
      'photoUrls': imageUrls,
      'score': rate,
      'restaurantId': resId,
      'content': review,
      'userName': userName,
      'createDate': createDate,
      'userId': userId,
    };
  }

  factory ReviewModel.fromMap(Map<String, dynamic> data) {
    return ReviewModel(
      id: data['id'] ?? 0,
      imageUrls: (data['photoUrls'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          [],
      rate: (data['score'] as num).toDouble(),
      resId: data['restaurantId'] ?? 0,
      review: data['content'] ?? '',
      userName: data['user'] ?? '',
      userId: data['userId'] ?? 0,
      restaurantName: data['restaurantName'] ?? '',
      createDate: data['createDate'] ?? 0,
    );
  }
}
