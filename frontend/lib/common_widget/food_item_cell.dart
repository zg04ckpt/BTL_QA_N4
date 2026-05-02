import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/common_widget/app_network_image.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:flutter/material.dart';

import '../common/color_extension.dart';

/// Ô nhà hàng trên Home (list ngang + grid). Tránh overflow bằng cách co ảnh
/// khi parent có `maxHeight` hữu hạn (GridView / ListView ngang).
class FoodItemCell extends StatelessWidget {
  final Restaurant fObj;

  const FoodItemCell({super.key, required this.fObj});

  /// Khoảng dự trừ cho khối text + padding dưới ảnh (2 dòng tên + danh mục + hàng sao).
  static const double _bottomBlockReserve = 102;

  static const double _outerVMargin = 8;

  @override
  Widget build(BuildContext context) {
    final screenW = MediaQuery.sizeOf(context).width;

    return LayoutBuilder(
      builder: (context, constraints) {
        final maxW = constraints.maxWidth;
        final maxH = constraints.maxHeight;

        final w = maxW.isFinite && maxW > 0 && maxW < double.infinity
            ? maxW
            : screenW * 0.42;

        double imageH = (w * 0.72).clamp(72.0, 136.0);

        if (maxH.isFinite && maxH < double.infinity) {
          final roomForImage =
              maxH - (2 * _outerVMargin) - _bottomBlockReserve;
          imageH = roomForImage.clamp(56.0, 136.0);
        }

        final hasUserLocation = GlobalData.instance.userPosition != null;
        final distanceText =
            hasUserLocation ? fObj.distance.toDistanceText() : '';

        return Container(
          margin: const EdgeInsets.symmetric(
            vertical: _outerVMargin,
            horizontal: 6,
          ),
          width: w,
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(5),
            boxShadow: const [
              BoxShadow(
                color: Colors.black12,
                blurRadius: 2,
                offset: Offset(0, 1),
              )
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              ClipRRect(
                borderRadius: const BorderRadius.only(
                  topLeft: Radius.circular(5),
                  topRight: Radius.circular(5),
                ),
                child: SizedBox(
                  width: w,
                  height: imageH,
                  child: ColoredBox(
                    color: TColor.secondary,
                    child: AppNetworkImage(
                      pathOrUrl: fObj.avtImage,
                      width: w,
                      height: imageH,
                      fit: BoxFit.cover,
                    ),
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(6, 6, 6, 6),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      fObj.name.toString(),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: TColor.text,
                        fontSize: 13,
                        height: 1.2,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      fObj.category.trim().isNotEmpty
                          ? fObj.category
                          : 'Nhà hàng',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: TColor.gray,
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            "${fObj.averageScore.toStringAsFixed(1)}⭐ (${fObj.totalReviews})",
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              color: TColor.gray,
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        if (distanceText.isNotEmpty)
                          Flexible(
                            child: Text(
                              distanceText,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              textAlign: TextAlign.right,
                              style: TextStyle(
                                color: TColor.gray,
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
