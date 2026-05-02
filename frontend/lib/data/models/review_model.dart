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

  /// POST tạo mới — không gửi `id` âm/lỗi để tránh lỗi bind phía server.
  Map<String, dynamic> toCreateJson() {
    return {
      'photoUrls': imageUrls,
      'score': rate,
      'restaurantId': resId,
      'content': review,
      'createDate': createDate,
      'userId': userId,
    };
  }

  /// PUT cập nhật — giữ id thật.
  Map<String, dynamic> toUpdateJson() {
    final m = toJson();
    if (id != null && id! > 0) {
      m['id'] = id;
    }
    return m;
  }

  factory ReviewModel.fromMap(Map<String, dynamic> data) {
    final photos = <String>[];
    final rawPhotos = data['photoUrls'];
    if (rawPhotos is List) {
      for (final e in rawPhotos) {
        if (e == null) continue;
        final s = e.toString().trim();
        if (s.isNotEmpty) photos.add(s);
      }
    }
    return ReviewModel(
      id: data['id'] is int ? data['id'] as int : int.tryParse('${data['id']}') ?? 0,
      imageUrls: photos,
      rate: (data['score'] as num?)?.toDouble() ?? 0.0,
      resId: data['restaurantId'] ?? 0,
      review: data['content']?.toString() ?? '',
      userName: data['user']?.toString() ?? '',
      userId: data['userId'] ?? 0,
      restaurantName: data['restaurantName']?.toString() ?? '',
      createDate: () {
        final d = data['createDate'];
        if (d == null) return 0;
        if (d is int) return d;
        if (d is num) return d.toInt();
        return int.tryParse(d.toString()) ?? 0;
      }(),
      reportCount: () {
        final r = data['reportsCount'] ?? data['ReportsCount'];
        if (r == null) return 0;
        if (r is int) return r;
        if (r is num) return r.toInt();
        return int.tryParse(r.toString()) ?? 0;
      }(),
    );
  }
}
