import 'package:cp_restaurants/common/app_snack_bar.dart';
import 'package:cp_restaurants/common_widget/dialog/app_dialog.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:custom_rating_bar/custom_rating_bar.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../common/color_extension.dart';
import '../../common/map_coordinates.dart';
import '../../common_widget/app_embedded_map.dart';
import '../../common_widget/app_network_image.dart';
import '../../common_widget/icon_text_button.dart';
import '../../common_widget/img_text_button.dart';
import '../../common_widget/selection_text_view.dart';
import '../../services/location_service.dart';
import '../../services/review_provider.dart';
import 'components/user_review_widget.dart';
import 'package:share_plus/share_plus.dart';

class RestaurantDetailView extends StatefulWidget {
  final Restaurant fObj;
  final bool? isAdminCheck;
  const RestaurantDetailView({
    super.key,
    required this.fObj,
    this.isAdminCheck = false,
  });

  @override
  State<RestaurantDetailView> createState() => _RestaurantDetailViewState();
}

class _RestaurantDetailViewState extends State<RestaurantDetailView> {
  Restaurant? resData;
  bool isLoading = false;
  UniqueKey reviewKey = UniqueKey();
  UniqueKey starKey = UniqueKey();

  int? selectedStar;
  bool isNearest = true;

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
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: Row(
            children: [
              Expanded(
                child: InkWell(
                  borderRadius: BorderRadius.circular(8),
                  onTap: () {
                    AppDialog.showOrderModalBottomSheet(context,
                        resId: widget.fObj.id);
                  },
                  child: Container(
                    height: 50,
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(8),
                      color: const Color.fromARGB(255, 121, 233, 123),
                      boxShadow: const [
                        BoxShadow(
                            color: Color.fromARGB(206, 0, 0, 0),
                            blurRadius: 3,
                            offset: Offset(0, 2))
                      ],
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Image.asset(
                          "assets/img/order.png",
                          height: 24,
                          width: 24,
                          fit: BoxFit.contain,
                        ),
                        const SizedBox(width: 8),
                        const Flexible(
                          child: Text(
                            "Đặt bàn",
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: Colors.black,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: InkWell(
                  borderRadius: BorderRadius.circular(8),
                  onTap: () async {
                    if (GlobalData.instance.userData == null) {
                      AppSnackBar.loginRequired(
                          context, "review this restaurant");
                      return;
                    }
                    await AppDialog.showRatingDialog(context,
                        resName: resData!.name,
                        resId: resData!.id, onSubmitedReview: () {
                      getResData();
                    });
                  },
                  child: Container(
                    height: 50,
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(8),
                      color: const Color.fromARGB(255, 121, 233, 123),
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
                        const Icon(
                            Icons.rate_review_outlined, color: Colors.black),
                        const SizedBox(width: 8),
                        Flexible(
                          child: Text(
                            "Đánh giá",
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: Colors.black,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
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
                              fit: StackFit.expand,
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
                                initialRating: resData!.averageScore,
                                maxRating: 5,
                              ),
                              const SizedBox(width: 6),
                              Flexible(
                                child: Text(
                                  "${resData!.averageScore.toStringAsFixed(1)} (${resData!.totalReviews})",
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  textAlign: TextAlign.right,
                                  style: const TextStyle(
                                      color: Colors.green,
                                      fontSize: 18,
                                      fontWeight: FontWeight.w700),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(width: 12),
                        Row(
                          children: [
                            const SizedBox(width: 20),
                            const Icon(Icons.phone,
                                color: Colors.green, size: 24),
                            InkWell(
                              onTap: () async {
                                var url = "tel:${widget.fObj.phoneNumber}";
                                launchUrl(Uri.parse(url));
                              },
                              child: Text(
                                " : ${widget.fObj.phoneNumber}",
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            const SizedBox(width: 20),
                            const Icon(Icons.email,
                                color: Colors.green, size: 24),
                            InkWell(
                              onTap: () {},
                              child: Text(
                                " : ${widget.fObj.email}",
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
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
                                  IconTextButton(
                                    title: viewModel.bookmarkRestaurants
                                            .contains(resData!.id)
                                        ? "Đã Lưu"
                                        : "Lưu",
                                    subTitle: "",
                                    icon: viewModel.bookmarkRestaurants
                                            .contains(resData!.id)
                                        ? "assets/img/bookmark_fill.png"
                                        : "assets/img/bookmark_detail.png",
                                    onPressed: () {
                                      // if (checkUserLogin(context)) {
                                      viewModel.setBookmarkRestaurants(resData!,
                                          isAdd: !viewModel.bookmarkRestaurants
                                              .contains(resData!.id));
                                      // }
                                    },
                                  ),
                                  IconTextButton(
                                    title: "Share",
                                    subTitle: "",
                                    icon: "assets/img/share.png",
                                    onPressed: () async {
                                      await Share.share(
                                          'Nhà hàng ${resData!.name} được đánh giá ${resData!.averageScore} sao trên CP-Foods. Địa chỉ ${resData!.address.toString()}, Nhấn vào đây để tìm đường https://www.google.com/maps/dir/?api=1&destination=${resData!.address.lat},${resData!.address.lon}');
                                    },
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
                        if (resData!.photoUrls.isEmpty)
                          Padding(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 16, vertical: 8),
                            child: Text(
                              'Chưa có ảnh mô tả.',
                              style: TextStyle(
                                color: TColor.gray,
                                fontSize: 14,
                              ),
                            ),
                          )
                        else
                          SizedBox(
                            height: (media.width * 0.3).clamp(100.0, 180.0),
                            width: media.width,
                            child: ListView.separated(
                              scrollDirection: Axis.horizontal,
                              padding:
                                  const EdgeInsets.symmetric(horizontal: 8),
                              itemCount: resData!.photoUrls.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(width: 10),
                              itemBuilder: (context, index) {
                                final photoUrl = resolveMediaUrl(
                                    resData!.photoUrls[index]);
                                return ImgTextButton(
                                  image: photoUrl,
                                  onPressed: () {
                                    AppDialog.showPreviewImage(
                                        context, photoUrl);
                                  },
                                );
                              },
                            ),
                          ),
                        Divider(
                          height: 4,
                          color: TColor.gray,
                        ),
                        if (!widget.isAdminCheck!)
                          SelectionTextView(
                            title: "Đánh giá ",
                            actionTitle:
                                "${resData!.averageScore.toStringAsFixed(1)}⭐ (${resData!.totalReviews} )",
                            onSeeAllTap: () {},
                          ),
                        SizedBox(
                          height: 60,
                          // width: double.infinity,
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              // const SizedBox(width: 40,),
                              DropdownButton<int>(
                                hint: const Text("Chọn số sao"),
                                value: selectedStar,
                                items: List.generate(5, (index) => index + 1)
                                    .map((star) => DropdownMenuItem(
                                          value: star,
                                          child: Text("$star sao"),
                                        ))
                                    .toList(),
                                onChanged: (value) {
                                  setState(() {
                                    selectedStar = value!;
                                  });
                                  // _filterReviews();
                                  context
                                      .read<ReviewProvider>()
                                      .getReviewsByResId(widget.fObj.id,
                                          star: selectedStar,
                                          isNearest: isNearest);
                                },
                              ),
                              DropdownButton<String>(
                                hint: const Text("Chọn thời gian"),
                                value: isNearest ? "Gần nhất" : "Xa nhất",
                                items: ["Gần nhất", "Xa nhất"]
                                    .map((time) => DropdownMenuItem(
                                          value: time,
                                          child: Text(time),
                                        ))
                                    .toList(),
                                onChanged: (value) {
                                  setState(() {
                                    isNearest = value == "Gần nhất";
                                  });
                                  context
                                      .read<ReviewProvider>()
                                      .getReviewsByResId(widget.fObj.id,
                                          star: selectedStar,
                                          isNearest: isNearest);

                                  // _filterReviews();
                                },
                              ),
                            ],
                          ),
                        ),
                        SizedBox(
                          key: reviewKey,
                          child: UserReviewWidget(
                            key: starKey,
                            resId: resData!.id,
                            resName: resData!.name,
                            onEdited: () async {
                              // await AppDialog.showRatingDialog(context,
                              //     resName: resData!.name,
                              //     resId: resData!.id,
                              //     initReview: ,
                              //     onSubmitedReview: () {});
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
                        const SizedBox(
                          height: 100,
                        )
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
