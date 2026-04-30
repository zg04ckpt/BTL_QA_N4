import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/report_provider.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ReportHistoryView extends StatefulWidget {
  const ReportHistoryView({super.key});

  @override
  State<ReportHistoryView> createState() => _ReportHistoryViewState();
}

class _ReportHistoryViewState extends State<ReportHistoryView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (GlobalData.instance.userData != null) {
        context
            .read<ReportProvider>()
            .fetchUserReports(GlobalData.instance.userData!.userId);
      }
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
          "Lịch sử báo cáo",
          style: TextStyle(
              color: Colors.black, fontSize: 20, fontWeight: FontWeight.w700),
        ),
      ),
      backgroundColor: TColor.bg,
      body: Consumer<ReportProvider>(
        builder: (context, reportProvider, child) {
          if (reportProvider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (reportProvider.userReports.isEmpty) {
            return const Center(
              child: Text(
                "Bạn chưa có báo cáo nào.",
                style: TextStyle(fontSize: 16, color: Colors.grey),
              ),
            );
          }

          return ListView.builder(
            padding: const EdgeInsets.symmetric(vertical: 15, horizontal: 12),
            itemCount: reportProvider.userReports.length,
            itemBuilder: (context, index) {
              final report = reportProvider.userReports[index];
              return Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.all(15),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.05),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
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
                              fontWeight: FontWeight.w700,
                              fontSize: 16),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: _getStatusColor(report.status).withOpacity(0.1),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: Text(
                            _getStatusText(report.status),
                            style: TextStyle(
                                color: _getStatusColor(report.status),
                                fontSize: 12,
                                fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    const Text(
                      "Lý do:",
                      style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                          color: Colors.grey),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      report.reason,
                      style: const TextStyle(fontSize: 15, color: Colors.black87),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      "Review ID: ${report.reviewId}",
                      style: const TextStyle(color: Colors.grey, fontSize: 12),
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

  Color _getStatusColor(int status) {
    switch (status) {
      case 0:
        return Colors.orange; // Chờ xử lý
      case 1:
        return Colors.green; // Đã xử lý (đã xóa review)
      case 2:
        return Colors.blue; // Đã bác bỏ
      default:
        return Colors.grey;
    }
  }

  String _getStatusText(int status) {
    switch (status) {
      case 0:
        return "Đang chờ";
      case 1:
        return "Đã xử lý";
      case 2:
        return "Đã bác bỏ";
      default:
        return "Không xác định";
    }
  }
}
