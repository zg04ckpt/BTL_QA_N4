import 'package:cloud_firestore/cloud_firestore.dart';

class Report {
  String reason;
  String resId;
  String reviewId;
  String userId;
  String reportId; 

  Report({
    required this.reason,
    required this.resId,
    required this.reviewId,
    required this.userId,
    required this.reportId,
  });

  // Chuyển đổi từ Report thành JSON (để lưu vào Firestore)
  Map<String, dynamic> toJson() {
    return {
      'reason': reason,
      'resId': resId,
      'reviewId': reviewId,
      'userId': userId,
    };
  }

  // Tạo một Report từ Firestore DocumentSnapshot
  factory Report.fromDocumentSnapshot(DocumentSnapshot snapshot) {
    final data = snapshot.data() as Map<String, dynamic>;
    return Report(
      reason: data['reason'] ?? '',
      resId: data['resId'] ?? '',
      reviewId: data['reviewId'] ?? '',
      userId: data['userId'] ?? '',
      reportId: snapshot.id, // Lấy reportId từ snapshot.id
    );
  }
}
