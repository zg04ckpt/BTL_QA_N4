import 'dart:developer';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/common_widget/dialog/app_dialog.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:flutter/material.dart';

import '../../../common/color_extension.dart';
import 'package:custom_rating_bar/custom_rating_bar.dart';

class UserReviewItem extends StatelessWidget {
  const UserReviewItem({
    super.key,
    required this.reviewModel,
    required this.onDeleteSuccess,
    required this.onEdited,
    required this.resName,
    this.onShowHistory,
    this.isHistory = false,
    this.isAdmin = false,
  });

  final ReviewModel reviewModel;
  final VoidCallback onDeleteSuccess;
  final VoidCallback onEdited;
  final String resName;
  final bool isHistory;
  final VoidCallback? onShowHistory;
  final bool isAdmin;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 15),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 12),
      decoration: BoxDecoration(
          color: Colors.green.withOpacity(0.1),
          borderRadius: BorderRadius.circular(8)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(
                (isHistory || isAdmin)
                    ? reviewModel.restaurantName
                    : reviewModel.userName,
                style: TextStyle(
                    color: TColor.text,
                    fontSize: 20,
                    fontWeight: FontWeight.w700),
              ),
              const Text(" - "),
              Text(reviewModel.createDate.toTimeAgo(),
                  style: TextStyle(
                      color: TColor.text,
                      fontSize: 12,
                      fontWeight: FontWeight.w300)),
              const Spacer(),
              if (GlobalData.instance.userData != null)
                PopupMenuButton(
                  style: ButtonStyle(
                      backgroundColor:
                          WidgetStatePropertyAll(Colors.green[200])),
                  onSelected: (value) async {
                    if (value == "/delete") {
                      AppDialog.showDeleteConfirmationDialog(
                        context,
                        deleteContent: "Review",
                        onDelete: () async {
                          await APIService.instance.request(
                              "/api/reviews/${reviewModel.id}",
                              DioMethod.delete);
                          onDeleteSuccess.call();
                        },
                      );
                    } else if (value == "/report") {
                      if (reviewModel.id == null) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text('Error: An error has occurred.'),
                          ),
                        );
                        return;
                      }
                      AppDialog.showReportDialog(
                        context,
                        resId: reviewModel.resId,
                        reviewId: reviewModel.id ?? 0,
                      );
                    } else {
                      await AppDialog.showRatingDialog(
                        context,
                        resName: resName,
                        resId: reviewModel.resId,
                        onSubmitedReview: onEdited,
                        initReview: reviewModel,
                      );
                      onEdited.call();
                    }
                  },
                  itemBuilder: (BuildContext bc) {
                    return [
                      if (reviewModel.userId ==
                              GlobalData.instance.userData?.userId ||
                          !isAdmin)
                        const PopupMenuItem(
                          value: '/edit',
                          child: Row(
                            children: [
                              Icon(Icons.edit),
                              SizedBox(width: 4),
                              Text("Chỉnh sửa")
                            ],
                          ),
                        ),
                      if (reviewModel.userId ==
                              GlobalData.instance.userData?.userId ||
                          isAdmin)
                        const PopupMenuItem(
                          value: '/delete',
                          child: Row(
                            children: [
                              Icon(
                                Icons.delete_forever_outlined,
                                color: Colors.red,
                              ),
                              SizedBox(width: 4),
                              Text(
                                "Xoá",
                                style: TextStyle(color: Colors.red),
                              )
                            ],
                          ),
                        ),
                      if (!isAdmin)
                        const PopupMenuItem(
                          value: '/report',
                          child: Row(
                            children: [
                              Icon(
                                Icons.report,
                                color: Colors.red,
                              ),
                              SizedBox(width: 4),
                              Text(
                                "Báo cáo",
                                style: TextStyle(color: Colors.red),
                              )
                            ],
                          ),
                        ),
                    ];
                  },
                )
            ],
          ),
          const SizedBox(
            height: 8,
          ),
          Row(children: [
            Text(
              "Rated",
              style: TextStyle(
                  color: TColor.gray,
                  fontSize: 12,
                  fontWeight: FontWeight.w700),
            ),
            RatingBar.readOnly(
              size: 16,
              filledIcon: Icons.star,
              alignment: Alignment.center,
              emptyIcon: Icons.star_border,
              initialRating: reviewModel.rate,
              maxRating: 5,
            ),
            Text(
              "${reviewModel.rate}",
              style: TextStyle(
                  color: TColor.primary,
                  fontSize: 14,
                  fontWeight: FontWeight.w700),
            ),
            const Spacer(),
          ]),
          const SizedBox(
            height: 8,
          ),
          Row(
            children: [
              Text(
                reviewModel.review,
                style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: TColor.text),
              ),
              const Spacer(),
              if (onShowHistory != null)
                InkWell(
                  onTap: () {},
                  child: Container(
                    padding: const EdgeInsets.all(4),
                    decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(6),
                        color: Colors.green),
                    child: const Text(
                      "Xem nhà hàng",
                      style: TextStyle(
                          fontSize: 12,
                          color: Colors.white,
                          fontWeight: FontWeight.bold),
                    ),
                  ),
                )
            ],
          ),
          const SizedBox(
            height: 8,
          ),
          if (reviewModel.imageUrls.isNotEmpty)
            SizedBox(
              height: 60,
              width: 360,
              child: ListView.builder(
                  shrinkWrap: true,
                  scrollDirection: Axis.horizontal,
                  itemCount: reviewModel.imageUrls.length,
                  itemBuilder: (context, idx) {
                    return InkWell(
                      onTap: () => AppDialog.showPreviewImage(context,
                          resolveMediaUrl(reviewModel.imageUrls[idx])),
                      child: Container(
                        margin: const EdgeInsets.only(right: 12),
                        child: CachedNetworkImage(
                          imageUrl:
                              resolveMediaUrl(reviewModel.imageUrls[idx]),
                          errorListener: (value) {
                            log(value.toString());
                          },
                          imageBuilder: (context, imageProvider) => Container(
                            height: 60,
                            width: 40,
                            decoration: BoxDecoration(
                                image: DecorationImage(
                              image: imageProvider,
                              fit: BoxFit.cover,
                            )),
                          ),
                          placeholder: (context, url) =>
                              const CircularProgressIndicator(),
                          errorWidget: (context, url, error) =>
                              const Icon(Icons.error),
                        ),
                      ),
                    );
                  }),
            ),
        ],
      ),
    );
  }
}
