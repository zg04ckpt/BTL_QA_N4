import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:flutter/material.dart';

import '../common/color_extension.dart';

class NearByListRow extends StatelessWidget {
  final Restaurant fObj;
  final bool isBookmark;
  final VoidCallback? onTap;
  final VoidCallback? onBookmarkToggle;
  const NearByListRow({
    super.key,
    required this.fObj,
    this.isBookmark = false,
    this.onTap,
    this.onBookmarkToggle,
  });

  @override
  Widget build(BuildContext context) {
    // var media = MediaQuery.of(context).size;

    var rateVal = double.tryParse(fObj.averageScore.toString()) ?? 0.0;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(5),
      child: Container(
        // height: 200,
        margin: const EdgeInsets.symmetric(vertical: 8, horizontal: 15),
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(5),
            boxShadow: const [
              BoxShadow(
                  color: Colors.black12, blurRadius: 3, offset: Offset(0, 2))
            ]),
        child: Row(
          children: [
          SizedBox(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(5),
              child: Image.asset(
                "assets/img/res_type/${(fObj.cateId >= 0 && fObj.cateId < restaurantTypes.length) ? restaurantTypes[restaurantTypes.keys.toList()[fObj.cateId]] : "Fast Food"}.png",
                width: 85,
                height: 85,
                fit: BoxFit.cover,
              ),
            ),
          ),
          const SizedBox(
            width: 8,
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        fObj.name.toString(),
                        textAlign: TextAlign.left,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                            color: TColor.text,
                            fontSize: 20,
                            fontWeight: FontWeight.w700),
                      ),
                    ),
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
                            fontWeight: FontWeight.w700),
                      ),
                    )
                  ],
                ),
               
                const SizedBox(
                  height: 4,
                ),
                Text(
                  fObj.address.toString(),
                  textAlign: TextAlign.left,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                      color: TColor.gray,
                      fontSize: 12,
                      fontWeight: FontWeight.w700),
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        (fObj.cateId >= 0 &&
                                fObj.cateId < restaurantTypes.length)
                            ? restaurantTypes.keys.toList()[fObj.cateId]
                            : "Unknown",
                        textAlign: TextAlign.left,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                            color: TColor.gray,
                            fontSize: 12,
                            fontWeight: FontWeight.w700),
                      ),
                    ),
                    if (isBookmark && onBookmarkToggle != null)
                      InkWell(
                        onTap: onBookmarkToggle,
                        child: const Padding(
                          padding: EdgeInsets.only(left: 8),
                          child: Icon(
                            Icons.bookmark_remove,
                            size: 18,
                            color: Colors.red,
                          ),
                        ),
                      )
                    else if (isBookmark)
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
      ),
    );
  }
}
