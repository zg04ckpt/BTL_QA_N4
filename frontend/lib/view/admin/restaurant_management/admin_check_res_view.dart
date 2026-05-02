import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/common_widget/app_network_image.dart';
import 'package:cp_restaurants/common/map_coordinates.dart';
import 'package:cp_restaurants/common_widget/app_embedded_map.dart';
import 'package:cp_restaurants/common_widget/dialog/app_dialog.dart';
import 'package:cp_restaurants/common_widget/icon_text_button.dart';
import 'package:cp_restaurants/common_widget/img_text_button.dart';
import 'package:cp_restaurants/common_widget/selection_text_view.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:cp_restaurants/services/location_service.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/services/review_provider.dart';
import 'package:cp_restaurants/view/home/components/photo_list_view.dart';
import 'package:cp_restaurants/view/restaurant/components/user_review_widget.dart';
import 'package:custom_rating_bar/custom_rating_bar.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class AdminCheckRes extends StatefulWidget {
  final Restaurant fObj;
  const AdminCheckRes({
    super.key,
    required this.fObj,
  });

  @override
  State<AdminCheckRes> createState() => _AdminCheckResState();
}

class _AdminCheckResState extends State<AdminCheckRes> {
  Restaurant? resData;
  bool isLoading = false;
  UniqueKey reviewKey = UniqueKey();
  UniqueKey starKey = UniqueKey();

  @override
  void initState() {
    resData = widget.fObj;
    isLoading = false;

    super.initState();
  }

  Future<void> getResData() async {
    final response = await APIService.instance.request(
      '/api/Restaurants/${resData!.id}',
      DioMethod.get,
    );
    resData = Restaurant.fromJson(response.data as Map<String, dynamic>);
    setState(() {
      starKey = UniqueKey();
    });
  }

  @override
  Widget build(BuildContext context) {
    var media = MediaQuery.of(context).size;

    return Scaffold(
      floatingActionButton: SizedBox(
        height: 50,
        width: media.width,
        child: Row(
          children: [
            const SizedBox(width: 40),
            Expanded(
              flex: 3,
              child: InkWell(
                onTap: () async {
                  AppDialog.confirmUpdateState(
                    context,
                    state: resData!.status == 0
                        ? 2
                        : resData!.status == 2
                            ? 3
                            : 2,
                    onUpdate: () async {
                      setState(() {
                        isLoading = true;
                      });
                      resData = resData!.copyWith(
                          status: resData!.status ==0
                              ? 2
                              : resData!.status == 2
                                  ? 3
                                  : 2,
                          cateId: restaurantTypes.keys.toList().indexWhere(
                                  (key) => key == resData!.category) +
                              1,
                          averageScore: 0,
                          totalReviews: 0);
                      await context
                          .read<RestaurantProvider>()
                          .editRestaurant(resData!);
                      Navigator.of(context).pop();
                    },
                  );
                },
                child: Container(
                    height: 70,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(8),
                      color: const Color.fromARGB(255, 38, 130, 39),
                      boxShadow: const [
                        BoxShadow(
                            color: Color.fromARGB(206, 0, 0, 0),
                            blurRadius: 3,
                            offset: Offset(0, 1))
                      ],
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          resData!.status == 0
                              ? "Chấp nhận"
                              : resData!.status == 2
                                  ? "Đóng cửa"
                                  : "Mở cửa",
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Icon(
                          resData!.status == 0
                              ? Icons.done
                              : resData!.status == 2
                                  ? Icons.lock
                                  : Icons.done_all_outlined,
                          size: 24,
                          color: Colors.white,
                        ),
                      ],
                    )),
              ),
            ),
            const SizedBox(width: 20),
            if (resData!.status <= 1)
              Expanded(
                flex: 3,
                child: InkWell(
                  onTap: () async {
                    AppDialog.confirmUpdateState(
                      context,
                      state: 2,
                      onUpdate: () async {
                        setState(() {
                          isLoading = true;
                        });
                        AppDialog.confirmUpdateState(
                          context,
                          state: 1,
                          onUpdate: () async {
                            resData = resData!.copyWith(
                              status: 1,
                              averageScore: 0,
                              totalReviews: 0,
                              cateId: restaurantTypes.keys.toList().indexWhere(
                                      (key) => key == resData!.category) +
                                  1,
                            );
                            await context
                                .read<RestaurantProvider>()
                                .editRestaurant(resData!);
                            Navigator.of(context).pop();
                          },
                        );
                      },
                    );
                  },
                  child: Container(
                      height: 70,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(8),
                        color: const Color.fromARGB(255, 252, 50, 0),
                        boxShadow: const [
                          BoxShadow(
                              color: Color.fromARGB(206, 0, 0, 0),
                              blurRadius: 3,
                              offset: Offset(0, 1))
                        ],
                      ),
                      child: const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text(
                            "Từ chối",
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          SizedBox(width: 12),
                          Icon(
                            Icons.cancel_outlined,
                            size: 24,
                            color: Colors.white,
                          ),
                        ],
                      )),
                ),
              ),
          ],
        ),
      ),
      body: Stack(
        children: [
          Container(
            color: Colors.white,
            child: Scaffold(
              backgroundColor: Colors.white,
              body: NestedScrollView(
                  headerSliverBuilder: (context, innerBoxIsScrolled) {
                    return [
                      SliverAppBar(
                        backgroundColor: Colors.transparent,
                        elevation: 0,
                        expandedHeight: media.width * 0.67,
                        floating: false,
                        centerTitle: false,
                        flexibleSpace: FlexibleSpaceBar(
                          titlePadding: EdgeInsets.zero,
                          title: Container(
                            color: TColor.secondary,
                            width: media.width,
                            height: media.width * 0.67,
                            child: Stack(
                              children: [
                                AppNetworkImage(
                                  pathOrUrl: resData!.avtImage,
                                  fit: BoxFit.cover,
                                  width: media.width,
                                  height: media.width * 0.67,
                                ),
                                Container(
                                  width: media.width,
                                  height: media.width * 0.75,
                                  decoration: const BoxDecoration(
                                      gradient: LinearGradient(
                                          colors: [
                                        Colors.black87,
                                        Colors.black54,
                                        Colors.transparent,
                                      ],
                                          begin: Alignment.topLeft,
                                          end: Alignment.bottomCenter)),
                                ),
                              ],
                            ),
                          ),
                        ),
                        leading: IconButton(
                          icon: Image.asset(
                            "assets/img/back.png",
                            width: 24,
                            color: Colors.white,
                            height: 30,
                          ),
                          onPressed: () {
                            Navigator.pop(context);
                          },
                        ),
                      ),
                    ];
                  },
                  body: SingleChildScrollView(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          color: Colors.white,
                          padding: const EdgeInsets.symmetric(
                              vertical: 8, horizontal: 15),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Text(
                                  resData!.name.toString(),
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                  textAlign: TextAlign.left,
                                  style: TextStyle(
                                      color: TColor.text,
                                      fontSize: 24,
                                      fontWeight: FontWeight.w700),
                                ),
                              ),
                              const SizedBox(width: 8),
                              RatingBar(
                                key: starKey,
                                size: 20,
                                filledIcon: Icons.star,
                                alignment: Alignment.center,
                                emptyIcon: Icons.star_border,
                                onRatingChanged: (value) {},
                                initialRating: resData!
                                    .averageScore, // Use the initial rating value
                                maxRating: 5,
                              ),
                              Text(
                                // "${resData!.averageScore}",
                                "${resData!.averageScore.toStringAsFixed(1)} (${resData!.totalReviews})",
                                textAlign: TextAlign.left,
                                style: const TextStyle(
                                    color: Colors.green,
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700),
                              )
                            ],
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 20),
                          child: Text(
                            resData!.description,
                            textAlign: TextAlign.left,
                            style: const TextStyle(
                                fontSize: 16, fontWeight: FontWeight.w400),
                          ),
                        ),
                        Container(
                          color: Colors.white,
                          padding: const EdgeInsets.symmetric(
                              vertical: 8, horizontal: 15),
                          child: Consumer<RestaurantProvider>(
                            builder: (context, viewModel, child) {
                              return Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                mainAxisAlignment:
                                    MainAxisAlignment.spaceEvenly,
                                children: [
                                  IconTextButton(
                                    title: resData!.category,
                                    subTitle: '',
                                    icon:
                                        "assets/img/res_type/${restaurantTypes[resData!.category]}.png",
                                    onPressed: () {},
                                  ),
                                ],
                              );
                            },
                          ),
                        ),

                        Container(
                          margin: const EdgeInsets.symmetric(horizontal: 12),
                          color: Colors.white,
                          height: 300,
                          child: Stack(
                            clipBehavior: Clip.hardEdge,
                            children: [
                              AppEmbeddedMap(
                                latitude: resData!.address.lat,
                                longitude: resData!.address.lon,
                                height: 300,
                                initialZoom: 16,
                              ),
                              if (!usedRestaurantCoordsForMap(
                                  resData!.address.lat,
                                  resData!.address.lon))
                                Positioned(
                                  top: 8,
                                  left: 8,
                                  right: 52,
                                  child: DecoratedBox(
                                    decoration: BoxDecoration(
                                      color: Colors.orange.shade100,
                                      borderRadius: BorderRadius.circular(6),
                                    ),
                                    child: const Padding(
                                      padding: EdgeInsets.all(6),
                                      child: Text(
                                        'Chưa có tọa độ nhà hàng — hiển thị khu vực gần bạn hoặc mặc định.',
                                        style: TextStyle(fontSize: 11),
                                      ),
                                    ),
                                  ),
                                ),
                              Positioned(
                                left: 16,
                                top: 44,
                                right: 56,
                                child: Container(
                                  padding: const EdgeInsets.all(8),
                                  decoration: BoxDecoration(
                                    color: Colors.green.withOpacity(0.4),
                                    borderRadius: BorderRadius.circular(4),
                                  ),
                                  child: Text(
                                    resData!.address.toString(),
                                    textAlign: TextAlign.left,
                                    maxLines: 3,
                                    style: const TextStyle(
                                        color: Colors.black,
                                        fontSize: 16,
                                        fontWeight: FontWeight.w700),
                                  ),
                                ),
                              ),
                              Positioned(
                                right: 8,
                                bottom: 8,
                                child: InkWell(
                                  onTap: () {
                                    final c = resolveMapCenter(
                                      restaurantLat: resData!.address.lat,
                                      restaurantLon: resData!.address.lon,
                                      userPosition:
                                          GlobalData.instance.userPosition,
                                    );
                                    LocationService.openMap(
                                        c.latitude, c.longitude);
                                  },
                                  child: Container(
                                    padding: const EdgeInsets.all(6),
                                    decoration: BoxDecoration(
                                      borderRadius: BorderRadius.circular(4),
                                      color: Colors.green,
                                    ),
                                    child: const Text(
                                      "Chỉ đường",
                                      style: TextStyle(
                                          color: Colors.white, fontSize: 14),
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),

                        const SizedBox(
                          height: 9,
                        ),

                        SelectionTextView(
                          title: "Ảnh",
                          actionTitle: "Xem Tất cả",
                          onSeeAllTap: () {},
                        ),

                        GestureDetector(
                          onTap: () {
                            Navigator.push(
                                context,
                                MaterialPageRoute(
                                    builder: (context) =>
                                        const PhotoListView()));
                          },
                          child: Container(
                            height: media.width * 0.35,
                            width: media.width,
                            padding: const EdgeInsets.symmetric(horizontal: 8),
                            child: SingleChildScrollView(
                              scrollDirection: Axis.horizontal,
                              child: ListView.builder(
                                  scrollDirection: Axis.horizontal,
                                  shrinkWrap: true,
                                  itemCount: resData!.photoUrls.length >= 3
                                      ? 3
                                      : resData!.photoUrls.length,
                                  itemBuilder: (context, index) {
                                    return ImgTextButton(
                                      image: resolveMediaUrl(
                                          resData!.photoUrls[index]),
                                      onPressed: () {
                                        AppDialog.showPreviewImage(
                                            context,
                                            resolveMediaUrl(
                                                resData!.photoUrls[index]));
                                      },
                                    );
                                  }),
                            ),
                          ),
                        ),

                        Divider(
                          height: 4,
                          color: TColor.gray,
                        ),

                        SelectionTextView(
                          title: "Đánh giá ",
                          actionTitle:
                              "${resData!.averageScore.toStringAsFixed(1)}⭐ (${resData!.totalReviews} )",
                          onSeeAllTap: () {},
                        ),
                        SizedBox(
                          key: reviewKey,
                          child: UserReviewWidget(
                            isAdmin: true,
                            resId: resData!.id,
                            resName: resData!.name,
                            onEdited: () async {
                              getResData();
                              context
                                  .read<ReviewProvider>()
                                  .getReviewsByResId(widget.fObj.id);
                            },
                            onDeleteSuccess: (rate) async {
                              getResData();
                              context
                                  .read<ReviewProvider>()
                                  .getReviewsByResId(widget.fObj.id);
                            },
                          ),
                        ),

                        Divider(
                          height: 4,
                          color: TColor.gray,
                        ),

                        // Trending this week
                      ],
                    ),
                  )),
            ),
          ),
          if (isLoading)
            Center(
              child: Container(
                height: 100,
                width: 200,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(12),
                  color: Colors.white,
                ),
                child: const Center(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [CircularProgressIndicator(), Text("Loading...")],
                  ),
                ),
              ),
            )
        ],
      ),
    );
  }
}

bool checkUserLogin(BuildContext context) {
  if (GlobalData.instance.user != null) {
    return true;
  } else {
    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
      dismissDirection: DismissDirection.up,
      behavior: SnackBarBehavior.floating,
      duration: Duration(seconds: 2),

      // margin: EdgeInsets.only(
      //     bottom: media.width * 1.4,
      //     left: 20,
      //     right: 20),
      backgroundColor: Colors.redAccent,
      content: Text(
        "Vui lòng đăng nhập để sử dụng tính năng này",
        style: TextStyle(
          fontSize: 15,
          color: Colors.white,
        ),
      ),
    ));
    return false;
  }
}
