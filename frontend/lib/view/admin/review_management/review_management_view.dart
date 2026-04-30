import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/services/admin_provider.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ReviewManagementView extends StatefulWidget {
  const ReviewManagementView({super.key});

  @override
  State<ReviewManagementView> createState() => _ReviewManagementViewState();
}

class _ReviewManagementViewState extends State<ReviewManagementView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AdminProvider>().fetchAllReports();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          "Quản lý báo cáo review",
          style: TextStyle(
              color: Colors.black, fontSize: 18, fontWeight: FontWeight.bold),
        ),
      ),
      backgroundColor: TColor.bg,
      body: Consumer<AdminProvider>(
        builder: (context, adminProvider, child) {
          if (adminProvider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (adminProvider.allReports.isEmpty) {
            return const Center(child: Text("Không có báo cáo nào."));
          }

          return ListView.builder(
            padding: const EdgeInsets.all(15),
            itemCount: adminProvider.allReports.length,
            itemBuilder: (context, index) {
              final report = adminProvider.allReports[index];
              return Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.all(15),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.05),
                      blurRadius: 5,
                      offset: const Offset(0, 2),
                    ),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "Báo cáo #${report.id}",
                          style: TextStyle(
                              color: TColor.primary,
                              fontWeight: FontWeight.bold,
                              fontSize: 15),
                        ),
                        _buildStatusBadge(report.status),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Text(
                      "Review ID: ${report.reviewId}",
                      style: const TextStyle(
                          fontWeight: FontWeight.w600, fontSize: 14),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      "Lý do: ${report.reason}",
                      style: const TextStyle(color: Colors.black87),
                    ),
                    const Divider(height: 25),
                    if (report.status == 0)
                      Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          TextButton(
                            onPressed: () => _handleResolve(report, 2), // Dismiss
                            child: const Text(
                              "Bác bỏ",
                              style: TextStyle(color: Colors.grey),
                            ),
                          ),
                          const SizedBox(width: 10),
                          ElevatedButton(
                            style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.red),
                            onPressed: () => _handleResolve(report, 1), // Resolve & Delete
                            child: const Text(
                              "Xóa Review",
                              style: TextStyle(color: Colors.white),
                            ),
                          ),
                        ],
                      )
                    else
                      const Center(
                        child: Text(
                          "Đã xử lý",
                          style: TextStyle(
                              color: Colors.grey,
                              fontStyle: FontStyle.italic,
                              fontSize: 12),
                        ),
                      ),
                  ],
                ),
              );
            },
          );
        },
      ),
    );
  }

  Widget _buildStatusBadge(int status) {
    String text;
    Color color;
    switch (status) {
      case 0:
        text = "Chờ xử lý";
        color = Colors.orange;
        break;
      case 1:
        text = "Đã xóa";
        color = Colors.red;
        break;
      case 2:
        text = "Đã bác bỏ";
        color = Colors.blue;
        break;
      default:
        text = "N/A";
        color = Colors.grey;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        text,
        style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold),
      ),
    );
  }

  void _handleResolve(report, int status) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(status == 1 ? "Xác nhận xóa review" : "Xác nhận bác bỏ báo cáo"),
        content: Text(status == 1
            ? "Hành động này sẽ xóa vĩnh viễn review bị báo cáo. Bạn chắc chắn chứ?"
            : "Bạn chắc chắn muốn bác bỏ báo cáo này?"),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text("Huỷ"),
          ),
          ElevatedButton(
            onPressed: () async {
              Navigator.pop(context);
              final success = await context.read<AdminProvider>().resolveReport(
                  report.id, status,
                  reviewId: status == 1 ? report.reviewId : null);
              if (mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(success ? "Thành công" : "Thất bại"),
                    backgroundColor: success ? Colors.green : Colors.red,
                  ),
                );
              }
            },
            child: const Text("Xác nhận"),
          ),
        ],
      ),
    );
  }
}
