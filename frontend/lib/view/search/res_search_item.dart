import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:cp_restaurants/view/restaurant_manager/components/edit_res_view.dart';
import 'package:cp_restaurants/view/restaurant_manager/list_order_screens.dart';
import 'package:cp_restaurants/view/restaurant_manager/qr_gen/qr_gen.dart';
import 'package:flutter/material.dart';

class ResSearchItem extends StatelessWidget {
  final Restaurant fObj;
  final bool isMyRes;
  const ResSearchItem({super.key, required this.fObj, this.isMyRes = false});

  @override
  Widget build(BuildContext context) {
    const double thumb = 80;
    final categoryLabel = fObj.category.trim().isNotEmpty
        ? fObj.category
        : (fObj.cateId >= 0 &&
                fObj.cateId < restaurantTypes.keys.length
            ? restaurantTypes.keys.toList()[fObj.cateId]
            : 'Danh mục #${fObj.cateId}');

    return Container(
      margin: const EdgeInsets.symmetric(vertical: 8),
      width: double.infinity,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(5),
        boxShadow: const [
          BoxShadow(color: Colors.black12, blurRadius: 2, offset: Offset(0, 1))
        ],
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SizedBox(width: 8),
          ClipRRect(
            borderRadius: BorderRadius.circular(5),
            child: Container(
              color: TColor.secondary,
              width: thumb,
              height: thumb,
              child: () {
                final avt = resolveMediaUrl(fObj.avtImage);
                if (avt.isEmpty) {
                  return const Center(child: Icon(Icons.restaurant, size: 40));
                }
                return CachedNetworkImage(
                  imageUrl: avt,
                  fit: BoxFit.cover,
                  imageBuilder: (context, imageProvider) => Container(
                    decoration: BoxDecoration(
                      image: DecorationImage(
                        image: imageProvider,
                        fit: BoxFit.cover,
                      ),
                    ),
                  ),
                  placeholder: (context, url) => const Center(
                      child: SizedBox(
                    width: 28,
                    height: 28,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )),
                  errorWidget: (context, url, error) =>
                      const Icon(Icons.error),
                );
              }(),
            ),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        SizedBox(
                          width: double.infinity,
                          child: Text(
                            fObj.name.toString(),
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                            textAlign: TextAlign.left,
                            style: TextStyle(
                                color: TColor.text,
                                fontSize: isMyRes ? 17 : 20,
                                fontWeight: FontWeight.w700),
                          ),
                        ),
                        const SizedBox(
                          height: 4,
                        ),
                        SizedBox(
                          height: 36,
                          child: Text(
                            fObj.address.toString(),
                            overflow: TextOverflow.ellipsis,
                            maxLines: 2,
                            textAlign: TextAlign.left,
                            style: TextStyle(
                                color: TColor.gray,
                                fontSize: 12,
                                fontWeight: FontWeight.w700),
                          ),
                        ),
                        const SizedBox(
                          height: 2,
                        ),
                        Text(
                          categoryLabel,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          textAlign: TextAlign.left,
                          style: TextStyle(
                              color: TColor.gray,
                              fontSize: 12,
                              fontWeight: FontWeight.w700),
                        ),
                        const SizedBox(
                          height: 2,
                        ),
                        //  const Spacer(),
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                "${fObj.averageScore}⭐ (${fObj.totalReviews} vote)",
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: TextStyle(
                                    color: TColor.gray,
                                    fontSize: 12,
                                    fontWeight: FontWeight.w700),
                              ),
                            ),
                            Text(
                              "${fObj.distance}",
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                  color: TColor.gray,
                                  fontSize: 12,
                                  fontWeight: FontWeight.w700),
                            ),
                          ],
                        )
                      ]),
                )
              ],
            ),
          ),
          if (isMyRes)
            Padding(
              padding: const EdgeInsets.only(top: 4, right: 4),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
                    padding: EdgeInsets.zero,
                    visualDensity: VisualDensity.compact,
                    constraints: const BoxConstraints(
                      minWidth: 44,
                      minHeight: 44,
                    ),
                    iconSize: 22,
                    onPressed: () {
                      Navigator.of(context)
                          .push(MaterialPageRoute(builder: (context) {
                        return EditResView(fObj: fObj);
                      }));
                    },
                    icon: const Icon(
                      Icons.edit,
                      color: Colors.green,
                    ),
                  ),
                  IconButton(
                    padding: EdgeInsets.zero,
                    visualDensity: VisualDensity.compact,
                    constraints: const BoxConstraints(
                      minWidth: 44,
                      minHeight: 44,
                    ),
                    iconSize: 22,
                    onPressed: () {
                      Navigator.of(context)
                          .push(MaterialPageRoute(builder: (context) {
                        return OrdersScreen(
                          resId: fObj.id,
                        );
                      }));
                    },
                    icon: const Icon(
                      Icons.table_bar_rounded,
                      color: Colors.green,
                    ),
                  ),
                  IconButton(
                    padding: EdgeInsets.zero,
                    visualDensity: VisualDensity.compact,
                    constraints: const BoxConstraints(
                      minWidth: 44,
                      minHeight: 44,
                    ),
                    iconSize: 22,
                    onPressed: () {
                      Navigator.of(context)
                          .push(MaterialPageRoute(builder: (context) {
                        return PrettyQrHomePage(
                          resId: fObj.id,
                        );
                      }));
                    },
                    icon: const Icon(
                      Icons.qr_code,
                      color: Colors.green,
                    ),
                  ),
                ],
              ),
            )
        ],
      ),
    );
  }
}
