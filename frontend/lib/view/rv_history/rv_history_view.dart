import 'package:cp_restaurants/common_widget/loading_widget.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/services/review_provider.dart';
import 'package:cp_restaurants/view/restaurant/components/user_review_item.dart';
import 'package:cp_restaurants/view/restaurant/restaurant_detail_view.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class RvHistoryView extends StatefulWidget {
  const RvHistoryView({super.key});

  @override
  State<RvHistoryView> createState() => _RvHistoryViewState();
}

class _RvHistoryViewState extends State<RvHistoryView> {
  @override
  void initState() {
    super.initState();
    // Gọi hàm để lấy danh sách các review của user
    fetchRVHistory();
  }

  Future<void> fetchRVHistory() async {
    List<ReviewModel> result =
        await context.read<ReviewProvider>().getUserReviews();
    setState(() {
      rVHistorys = result;
    });
  }

  bool isLoading = false;

  List<ReviewModel>? rVHistorys;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        // automaticallyImplyLeading: false,
        centerTitle: true,
        title: const Text(
          "Lịch sử Review",
          style: TextStyle(
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
      body: rVHistorys == null
          ? const Center(child: CircularProgressIndicator())
          : Stack(
              children: [
                ListView.builder(
                  itemCount: rVHistorys!.length,
                  itemBuilder: (context, index) {
                    final review = rVHistorys![index];
                    return UserReviewItem(
                      onShowHistory: () async {
                        setState(() {
                          isLoading = true;
                        });
                        final restaurant = await context
                            .read<RestaurantProvider>()
                            .getRestaurantById(review.resId);
                        setState(() {
                          isLoading = false;
                        });
                        if (restaurant != null) {
                          // Điều hướng sang trang chi tiết nhà hàng
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => RestaurantDetailView(
                                fObj: restaurant,
                              ),
                            ),
                          );
                        }
                      },
                      reviewModel: review,
                      onDeleteSuccess: () {
                        fetchRVHistory();
                      },
                      onEdited: () {
                        fetchRVHistory();
                      },
                      resName: "",
                    );
                  },
                ),
                if (isLoading) const LoadingWidget()
              ],
            ),
    );
  }
}
