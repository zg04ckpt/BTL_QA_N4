import 'package:cp_restaurants/data/models/report_model.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:flutter/material.dart';
import 'package:cloud_firestore/cloud_firestore.dart';

class ReviewDetailView extends StatefulWidget {
  final ReviewModel review;  // Accept review object

  const ReviewDetailView({Key? key, required this.review}) : super(key: key);

  @override
  State<ReviewDetailView> createState() => _ReviewDetailViewState();
}

class _ReviewDetailViewState extends State<ReviewDetailView> {
  List<ReportModel> reports = [];
  bool isLoading = true;

  @override
  void initState() {
    super.initState();
    fetchReports();  // Fetch reports related to this review
  }

  Future<void> fetchReports() async {
    // Fetch reports from Firestore
    try {
      QuerySnapshot reportSnapshot = await FirebaseFirestore.instance
          .collection('reports')
          .where('reviewId', isEqualTo: widget.review.id)
          .get();

      reports = reportSnapshot.docs
          .map((doc) => ReportModel.fromJson(doc.data() as Map<String, dynamic>))
          .toList();

      setState(() {
        isLoading = false;
      });
    } catch (e) {
      print("Error fetching reports: $e");
    }
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
          'Review Detail',
          style: TextStyle(
            color: Colors.black,
            fontSize: 18,
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          // AppDialog.showDeleteConfirmationDialog(
          //             context,
          //             deleteContent: "Review",
          //             onDelete: () {
          //               context
          //                   .read<ReviewProvider>()
          //                   .deleteReview(widget.review.id ?? "");
          //               Navigator.of(context).pop();
          //             },
          //           );
        },
        child: const SizedBox(
          height: 70,
          width: 70,
          child: Icon(Icons.delete_forever_outlined,color: Colors.red,),
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
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
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
                  reports.isEmpty
                      ? const Text('No reports found for this review.')
                      : Expanded(
                          child: ListView.builder(
                            itemCount: reports.length,
                            itemBuilder: (context, index) {
                              final report = reports[index];
                              return ListTile(
                                title: Text('Reason: ${report.reason}'),
                                subtitle: Text('Reported by: ${report.userId}'),
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
