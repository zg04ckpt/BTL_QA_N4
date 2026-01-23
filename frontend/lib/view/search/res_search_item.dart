import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/network/api_util.dart';
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
    var media = MediaQuery.of(context).size;
    return Container(
      margin: const EdgeInsets.symmetric(vertical: 8),
      width: media.width * 0.4,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(5),
        boxShadow: const [
          BoxShadow(color: Colors.black12, blurRadius: 2, offset: Offset(0, 1))
        ],
      ),
      child: Row(
        children: [
          const SizedBox(width: 5),
          ClipRRect(
            borderRadius: BorderRadius.circular(5),
            child: Container(
              color: TColor.secondary,
              width: media.width * 0.25,
              height: media.width * 0.25,
              child: CachedNetworkImage(
                imageUrl:
                    '${APIService.instance.baseUrl}/${fObj.avtImage.toString()}',
                fit: BoxFit.cover,
                imageBuilder: (context, imageProvider) => Container(
                  decoration: BoxDecoration(
                    image: DecorationImage(
                      image: imageProvider,
                      fit: BoxFit.cover,
                    ),
                  ),
                ),
                placeholder: (context, url) =>
                    const CircularProgressIndicator(),
                errorWidget: (context, url, error) => const Icon(Icons.error),
              ),
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
                        Row(
                          children: [
                            Text(
                              fObj.name.toString(),
                              maxLines: 1,
                              textAlign: TextAlign.left,
                              style: TextStyle(
                                  color: TColor.text,
                                  fontSize: 20,
                                  fontWeight: FontWeight.w700),
                            ),
                          ],
                        ),
                        const SizedBox(
                          height: 4,
                        ),
                        SizedBox(
                          height: 20,
                          child: Text(
                            fObj.address.toString(),
                            overflow: TextOverflow.clip,
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
                          restaurantTypes.keys.toList()[fObj.cateId],
                          maxLines: 1,
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
                            Text(
                              "${fObj.averageScore}⭐ (${fObj.totalReviews} vote)",
                              style: TextStyle(
                                  color: TColor.gray,
                                  fontSize: 12,
                                  fontWeight: FontWeight.w700),
                            ),
                            const Spacer(),
                            Text(
                              "${fObj.distance}",
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
            SizedBox(
              width: 50,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
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
                  )
                ],
              ),
            )
        ],
      ),
    );
  }
}
