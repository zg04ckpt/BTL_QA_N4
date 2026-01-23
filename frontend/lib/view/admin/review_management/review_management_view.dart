import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/view/admin/review_detail/review_detail_view.dart';
import 'package:flutter/material.dart';

class ReviewManagementView extends StatefulWidget {
  const ReviewManagementView({super.key});

  @override
  State<ReviewManagementView> createState() => _ReviewManagementViewState();
}

class _ReviewManagementViewState extends State<ReviewManagementView> {
  List<ReviewModel> reviews = [];
  bool isLoading = false;
  bool hasMore = true;
  DocumentSnapshot? lastDocument;
  final int pageSize = 10;
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _fetchReviews();
  }

  Future<void> _fetchReviews() async {}

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Review Management")),
      body: reviews.isEmpty
          ? Center(
              child: isLoading
                  ? const CircularProgressIndicator()
                  : const Text("No reviews found"))
          : ListView.builder(
              itemCount: reviews.length + 1,
              itemBuilder: (context, index) {
                if (index == reviews.length) {
                  return isLoading
                      ? const Center(child: CircularProgressIndicator())
                      : const SizedBox.shrink();
                }

                final review = reviews[index];
                return InkWell(
                  onTap: () async {
                    await Navigator.push(
                      context,
                      MaterialPageRoute(
                          builder: (context) => ReviewDetailView(
                                review: review,
                              )),
                    );
                    _fetchReviews();
                  },
                  child: Container(
                    margin:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 16),
                    padding: const EdgeInsets.all(4),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(8),
                      color: Colors.white,
                      boxShadow: const [
                        BoxShadow(
                            color: Colors.black12,
                            blurRadius: 2,
                            offset: Offset(0, 1))
                      ],
                    ),
                    child: ListTile(
                      title: Text(
                        review.review,
                        style: const TextStyle(
                            fontSize: 16, fontWeight: FontWeight.bold),
                      ),
                      subtitle: Text(
                          "Reports: ${review.reportCount}, Rating: ${review.rate}"),
                    ),
                  ),
                );
              },
              controller: ScrollController()
                ..addListener(() {
                  if (_scrollController.position.pixels ==
                      _scrollController.position.maxScrollExtent) {
                    _fetchReviews();
                  }
                }),
            ),
    );
  }
}
