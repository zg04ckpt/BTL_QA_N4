import 'package:cp_restaurants/common_widget/app_network_image.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:flutter/material.dart';

import '../common/color_extension.dart';

class NearByListRow extends StatelessWidget {
  final Restaurant fObj;
  final bool isBookmark;
  const NearByListRow({super.key, required this.fObj, this.isBookmark = false});

  @override
  Widget build(BuildContext context) {
    var rateVal = double.tryParse(fObj.averageScore.toString()) ?? 0.0;

    final keys = restaurantTypes.keys.toList();
    final safeIndex =
        fObj.cateId >= 0 && fObj.cateId < keys.length ? fObj.cateId : 0;
    final fallbackAsset =
        "assets/img/res_type/${restaurantTypes[keys[safeIndex]]}.png";

    const thumb = 88.0;

    return Container(
      margin: const EdgeInsets.symmetric(vertical: 8, horizontal: 15),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(5),
        boxShadow: const [
          BoxShadow(
              color: Colors.black12, blurRadius: 3, offset: Offset(0, 2))
        ],
      ),
      child: Row(
        children: [
          SizedBox(
            width: thumb,
            height: thumb,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(5),
              child: fObj.avtImage.trim().isNotEmpty
                  ? AppNetworkImage(
                      pathOrUrl: fObj.avtImage,
                      width: thumb,
                      height: thumb,
                      fit: BoxFit.cover,
                    )
                  : Image.asset(
                      fallbackAsset,
                      width: thumb,
                      height: thumb,
                      fit: BoxFit.cover,
                    ),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        fObj.name.toString(),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        textAlign: TextAlign.left,
                        style: TextStyle(
                          color: TColor.text,
                          fontSize: 20,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          vertical: 2, horizontal: 8),
                      decoration: BoxDecoration(
                        color: (rateVal < 2.0)
                            ? Colors.red
                            : (rateVal < 3.0)
                                ? Colors.orange
                                : (rateVal < 4.0)
                                    ? Colors.yellow
                                    : TColor.primary,
                        borderRadius: BorderRadius.circular(5),
                      ),
                      child: Text(
                        fObj.averageScore.toString(),
                        textAlign: TextAlign.left,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    )
                  ],
                ),
                const SizedBox(height: 6),
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        fObj.category.trim().isNotEmpty
                            ? fObj.category
                            : (fObj.cateId >= 0 && fObj.cateId < keys.length
                                ? keys[fObj.cateId]
                                : keys[safeIndex]),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        textAlign: TextAlign.left,
                        style: TextStyle(
                          color: TColor.gray,
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    if (isBookmark)
                      Image.asset(
                        "assets/img/bookmark_fill.png",
                        width: 15,
                      ),
                  ],
                ),
              ],
            ),
          )
        ],
      ),
    );
  }
}
