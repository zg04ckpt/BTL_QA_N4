import 'package:cp_restaurants/services/review_provider.dart';
import 'package:cp_restaurants/view/restaurant/components/user_review_item.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class UserReviewWidget extends StatefulWidget {
  const UserReviewWidget(
      {super.key,
      required this.resId,
      required this.onDeleteSuccess,
      required this.onEdited,
      required this.resName,
      this.isAdmin = false});

  final int resId;
  final String resName;
  final Function(double) onDeleteSuccess;
  final Function() onEdited;
  final bool isAdmin;

  @override
  State<UserReviewWidget> createState() => _UserReviewWidgetState();
}

class _UserReviewWidgetState extends State<UserReviewWidget> {
  @override
  void initState() {
    context.read<ReviewProvider>().getReviewsByResId(widget.resId);
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<ReviewProvider>(builder: (context, reviewProvider, child) {
      if (reviewProvider.reviewModels == []) {
        return const Center(
          child: CircularProgressIndicator(),
        );
      } else {
        return ListView.builder(
            shrinkWrap: true,
            padding: EdgeInsets.zero,
            physics: const NeverScrollableScrollPhysics(),
            itemCount: reviewProvider.reviewModels.length,
            itemBuilder: (context, idex) {
              return UserReviewItem(
                resName: widget.resName,
                isAdmin: widget.isAdmin,
                reviewModel: reviewProvider.reviewModels[idex],
                onDeleteSuccess: () {
                  widget
                      .onDeleteSuccess(reviewProvider.reviewModels[idex].rate);
                },
                onEdited: widget.onEdited,
              );
            });
      }
    });
  }
}
