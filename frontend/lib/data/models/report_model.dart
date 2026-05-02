/// Báo cáo đánh giá (REST `/api/Report`).
class Report {
  final int id;
  final int userId;
  final int reviewId;
  final String reason;
  final int? status;
  final String? userName;

  Report({
    required this.id,
    required this.userId,
    required this.reviewId,
    required this.reason,
    this.status,
    this.userName,
  });

  factory Report.fromMap(Map<String, dynamic> data) {
    return Report(
      id: _parseInt(data['id']),
      userId: _parseInt(data['userId']),
      reviewId: _parseInt(data['reviewId']),
      reason: data['reason']?.toString() ?? '',
      status: _parseIntOrNull(data['status']),
      userName: data['userName']?.toString(),
    );
  }

  static int _parseInt(dynamic v) {
    if (v == null) return 0;
    if (v is int) return v;
    if (v is num) return v.toInt();
    return int.tryParse(v.toString()) ?? 0;
  }

  static int? _parseIntOrNull(dynamic v) {
    if (v == null) return null;
    if (v is int) return v;
    if (v is num) return v.toInt();
    return int.tryParse(v.toString());
  }
}
