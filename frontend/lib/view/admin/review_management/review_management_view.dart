import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/view/admin/review_detail/review_detail_view.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';

/// Danh sách đánh giá qua REST `GET /api/reviews`.
class ReviewManagementView extends StatefulWidget {
  const ReviewManagementView({super.key});

  @override
  State<ReviewManagementView> createState() => _ReviewManagementViewState();
}

class _ReviewManagementViewState extends State<ReviewManagementView> {
  List<ReviewModel> reviews = [];
  bool isLoading = false;
  String? errorMessage;
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _fetchReviews();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _fetchReviews() async {
    setState(() {
      isLoading = true;
      errorMessage = null;
    });
    try {
      final response =
          await APIService.instance.request('/api/reviews', DioMethod.get);
      if (response.statusCode == 200 && response.data is List) {
        final raw = response.data as List<dynamic>;
        reviews = raw
            .map((e) =>
                ReviewModel.fromMap(Map<String, dynamic>.from(e as Map)))
            .toList();
        reviews.sort((a, b) => b.createDate.compareTo(a.createDate));
      } else {
        errorMessage = 'HTTP ${response.statusCode}';
        reviews = [];
      }
    } on DioException catch (e) {
      errorMessage = e.response?.data?.toString() ?? e.message;
      reviews = [];
    } catch (e) {
      errorMessage = '$e';
      reviews = [];
    } finally {
      if (mounted) {
        setState(() => isLoading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: TColor.bg,
      appBar: AppBar(
        title: const Text('Quản lý đánh giá'),
        backgroundColor: Colors.white,
        foregroundColor: TColor.text,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: isLoading ? null : _fetchReviews,
          ),
        ],
      ),
      body: RefreshIndicator(
        color: TColor.primary,
        onRefresh: _fetchReviews,
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (isLoading && reviews.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: const [
          SizedBox(height: 120),
          Center(child: CircularProgressIndicator()),
        ],
      );
    }
    if (errorMessage != null && reviews.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(24),
        children: [
          Icon(Icons.error_outline, size: 48, color: TColor.color1),
          const SizedBox(height: 12),
          Text(errorMessage!, style: TextStyle(color: TColor.color1)),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: _fetchReviews,
            child: const Text('Thử lại'),
          ),
        ],
      );
    }
    if (reviews.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          SizedBox(height: MediaQuery.of(context).size.height * 0.2),
          Center(
            child: Column(
              children: [
                Icon(Icons.reviews_outlined, size: 56, color: TColor.gray),
                const SizedBox(height: 12),
                Text('Chưa có đánh giá', style: TextStyle(color: TColor.gray)),
              ],
            ),
          ),
        ],
      );
    }

    return ListView.separated(
      controller: _scrollController,
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      itemCount: reviews.length,
      separatorBuilder: (_, __) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        final review = reviews[index];
        return Material(
          elevation: 1,
          borderRadius: BorderRadius.circular(12),
          color: Colors.white,
          child: ListTile(
            contentPadding: const EdgeInsets.all(12),
            onTap: () async {
              await Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (context) => ReviewDetailView(review: review),
                ),
              );
              _fetchReviews();
            },
            title: Text(
              review.review.isEmpty ? '(Không có nội dung)' : review.review,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 15),
            ),
            subtitle: Padding(
              padding: const EdgeInsets.only(top: 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    review.restaurantName.isNotEmpty
                        ? review.restaurantName
                        : 'Nhà hàng #${review.resId}',
                    style: TextStyle(color: TColor.primary, fontSize: 13),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${review.userName} · ⭐ ${review.rate.toStringAsFixed(1)}',
                    style: TextStyle(color: TColor.gray, fontSize: 12),
                  ),
                  Text(
                    'Báo cáo: ${review.reportCount}',
                    style: TextStyle(color: TColor.gray, fontSize: 12),
                  ),
                ],
              ),
            ),
            isThreeLine: true,
          ),
        );
      },
    );
  }
}
