import 'package:cp_restaurants/common_widget/dialog/app_dialog.dart';
import 'package:cp_restaurants/data/models/report_model.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/services/review_provider.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ReviewDetailView extends StatefulWidget {
  final ReviewModel review;

  const ReviewDetailView({Key? key, required this.review}) : super(key: key);

  @override
  State<ReviewDetailView> createState() => _ReviewDetailViewState();
}

class _ReviewDetailViewState extends State<ReviewDetailView> {
  List<Report> reports = [];
  bool isLoading = true;
  String? errorMessage;

  @override
  void initState() {
    super.initState();
    fetchReports();
  }

  Future<void> fetchReports() async {
    final id = widget.review.id;
    if (id == null || id <= 0) {
      setState(() {
        isLoading = false;
        errorMessage = 'Thiếu id đánh giá.';
      });
      return;
    }

    setState(() {
      isLoading = true;
      errorMessage = null;
    });

    try {
      final response = await APIService.instance.request(
        '/api/Report/by-review/$id',
        DioMethod.get,
      );
      if (response.statusCode == 200 && response.data is List) {
        final raw = response.data as List<dynamic>;
        reports = raw
            .map((e) => Report.fromMap(Map<String, dynamic>.from(e as Map)))
            .toList();
      } else {
        reports = [];
        errorMessage = 'HTTP ${response.statusCode}';
      }
    } on DioException catch (e) {
      reports = [];
      errorMessage = e.response?.data?.toString() ?? e.message;
    } catch (e) {
      reports = [];
      errorMessage = '$e';
    } finally {
      if (mounted) {
        setState(() => isLoading = false);
      }
    }
  }

  Future<void> _onDeletePressed() async {
    final id = widget.review.id;
    if (id == null || id <= 0) return;

    AppDialog.showDeleteConfirmationDialog(
      context,
      deleteContent: 'Review',
      onDelete: () async {
        final ok =
            await context.read<ReviewProvider>().deleteReview(id);
        if (!context.mounted) return;
        if (ok) {
          Navigator.of(context).pop(true);
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Không xóa được đánh giá.')),
          );
        }
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Review Detail'),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _onDeletePressed,
        child: const SizedBox(
          height: 70,
          width: 70,
          child: Icon(Icons.delete_forever_outlined, color: Colors.red),
        ),
      ),
      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'User: ${widget.review.userName}',
                    style: const TextStyle(
                        fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 10),
                  Text('Rating: ${widget.review.rate}'),
                  const SizedBox(height: 10),
                  Text('Review: ${widget.review.review}'),
                  const SizedBox(height: 20),
                  const Text(
                    'Reports:',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 10),
                  if (errorMessage != null)
                    Text(errorMessage!,
                        style: const TextStyle(color: Colors.red)),
                  if (errorMessage == null && reports.isEmpty)
                    const Text('No reports found for this review.'),
                  if (reports.isNotEmpty)
                    Expanded(
                      child: ListView.builder(
                        itemCount: reports.length,
                        itemBuilder: (context, index) {
                          final report = reports[index];
                          return ListTile(
                            title: Text('Reason: ${report.reason}'),
                            subtitle: Text(
                              'Bởi: ${report.userName ?? report.userId} · Trạng thái: ${report.status ?? "-"}',
                            ),
                          );
                        },
                      ),
                    ),
                ],
              ),
            ),
    );
  }
}
