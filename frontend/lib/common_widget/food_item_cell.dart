import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/material.dart';

import '../common/color_extension.dart';

/// Chiều cao list ngang "Gần bạn" — khớp cấu trúc [FoodItemCell] (ảnh + padding + khối chữ + margin),
/// tránh `shrinkWrap` ListView trong `SingleChildScrollView` gây `RenderBox was not laid out`.
double foodItemHorizontalStripHeight(BuildContext context) {
  final w = MediaQuery.sizeOf(context).width;
  final cardW = w * 0.4;
  const imageAspect = 1.12;
  final scale = MediaQuery.textScalerOf(context)
          .clamp(minScaleFactor: 0.85, maxScaleFactor: 1.35)
          .scale(14) /
      14;
  final imageH = cardW / imageAspect;
  const textPadding = 14.0;
  final textBlock = 86.0 * scale;
  const outerMargin = 12.0;
  const safety = 8.0;
  return imageH + textPadding + textBlock + outerMargin + safety;
}

/// Tỉ lệ ô Grid (width/height) cho 2 cột + padding 8 mỗi bên + [crossAxisSpacing],
/// ước lượng phần chữ theo [imageAspect] — không gán chiều cao ô bằng pixel cố định.
double foodItemGridChildAspectRatio(
  BuildContext context, {
  double horizontalInset = 24,
  double crossAxisSpacing = 8,
  double imageAspect = 1.12,
}) {
  final w = MediaQuery.sizeOf(context).width;
  final scale = MediaQuery.textScalerOf(context)
          .clamp(minScaleFactor: 0.85, maxScaleFactor: 1.35)
          .scale(14) /
      14;
  final textPadEstimate = 92.0 * scale;
  final tileW = (w - horizontalInset - crossAxisSpacing) / 2;
  final tileH = tileW / imageAspect + textPadEstimate;
  return (tileW / tileH).clamp(0.42, 0.92);
}

class FoodItemCell extends StatelessWidget {
  final Restaurant fObj;
  const FoodItemCell({super.key, required this.fObj});

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.sizeOf(context);
    final cardWidth = media.width * 0.4;

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      width: cardWidth,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(5),
        boxShadow: const [
          BoxShadow(
              color: Colors.black12, blurRadius: 2, offset: Offset(0, 1))
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(5),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AspectRatio(
              aspectRatio: 1.12,
              child: ColoredBox(
                color: TColor.secondary,
                child: CachedNetworkImage(
                  imageUrl: APIService.instance.resolveMediaUrl(fObj.avtImage),
                  fit: BoxFit.cover,
                  width: double.infinity,
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(8, 6, 8, 8),
              child: FittedBox(
                fit: BoxFit.scaleDown,
                alignment: Alignment.topLeft,
                child: SizedBox(
                  width: cardWidth - 16,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        fObj.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: TColor.text,
                          fontSize: 14,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '${fObj.address.city.isNotEmpty ? fObj.address.city : fObj.address.district}'
                        '${fObj.address.detail.isNotEmpty ? ' · ${fObj.address.detail}' : ''}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: TColor.gray,
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        fObj.category,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: TColor.gray,
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          Expanded(
                            child: Text(
                              '${fObj.averageScore.toStringAsFixed(1)}⭐ (${fObj.totalReviews})',
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                color: TColor.gray,
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                          const SizedBox(width: 6),
                          Text(
                            fObj.distance.toDistanceText(),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              color: TColor.primary,
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
