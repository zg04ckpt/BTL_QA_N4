class ReportModel {
  final int id;
  final int userId;
  final int reviewId;
  final String reason;
  final int status;

  ReportModel({
    required this.id,
    required this.userId,
    required this.reviewId,
    required this.reason,
    required this.status,
  });

  factory ReportModel.fromJson(Map<String, dynamic> json) {
    return ReportModel(
      id: json['id'] ?? 0,
      userId: json['userId'] ?? 0,
      reviewId: json['reviewId'] ?? 0,
      reason: json['reason'] ?? '',
      status: json['status'] ?? 0,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'reviewId': reviewId,
      'reason': reason,
      'status': status,
    };
  }
}
