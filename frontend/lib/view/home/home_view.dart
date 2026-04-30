import 'package:cp_restaurants/common/extension.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/common_widget/no_internet_page.dart';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/view/home/components/legendry_list_view.dart';
import 'package:cp_restaurants/view/home/components/search_bar_view.dart';
import 'package:cp_restaurants/view/restaurant/restaurant_detail_view.dart';
import 'package:cp_restaurants/view/search/search_view.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../common/color_extension.dart';
import '../../common_widget/food_item_cell.dart';
import '../../common_widget/round_button.dart';
import '../../common_widget/selection_text_view.dart';
import '../scan_qr/scan_qr_page.dart';
import 'components/search_location_view.dart';

class HomeView extends StatefulWidget {
  const HomeView({super.key});

  @override
  State<HomeView> createState() => _HomeViewState();
}

class _HomeViewState extends State<HomeView> {
  bool isSelectCity = true;
  TextEditingController txtSearch = TextEditingController();

  late LocationProvider _locationProvider;
  late VoidCallback _onLocationChanged;

  @override
  void initState() {
    super.initState();
    _locationProvider = context.read<LocationProvider>();
    _onLocationChanged = () {
      if (!mounted) return;
      if (GlobalData.instance.userPosition != null) {
        context.read<RestaurantProvider>().onUserLocationResolved();
      }
    };
    _locationProvider.addListener(_onLocationChanged);

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      context.read<RestaurantProvider>().init();
      _onLocationChanged();
    });

    FirebaseMessaging.instance.subscribeToTopic("all_user");
  }

  @override
  void dispose() {
    _locationProvider.removeListener(_onLocationChanged);
    txtSearch.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    var media = MediaQuery.of(context).size;
    return Selector<CommonProvider, bool>(
        selector: (context, commom) => commom.isConnect,
        builder: (context, isConnect, child) {
          if (isConnect) {
            return Scaffold(
              floatingActionButton: InkWell(
                onTap: () {
                  Navigator.of(context)
                      .push(MaterialPageRoute(builder: (context) {
                    return const ScanPage();
                  }));
                },
                child: Container(
                  decoration: BoxDecoration(shape: BoxShape.circle, boxShadow: [
                    BoxShadow(
                      color: const Color.fromARGB(255, 228, 168, 168)
                          .withOpacity(0.5),
                      spreadRadius: 5,
                      blurRadius: 7,
                      offset: const Offset(0, 3), // changes position of shadow
                    ),
                  ]),
                  child: Image.asset(
                    "assets/img/qr-scan.png",
                    height: 80,
                    width: 80,
                  ),
                ),
              ),
              backgroundColor: TColor.bg,
              body: isSelectCity
                  ? NestedScrollView(
                      headerSliverBuilder: (context, innerBoxIsScrolled) {
                      return [
                        SliverAppBar(
                          backgroundColor: Colors.white,
                          elevation: 0,
                          pinned: true,
                          floating: false,
                          automaticallyImplyLeading: false,
                          centerTitle: false,
                          leading: IconButton(
                            icon: Icon(
                              Icons.location_searching,
                              color: TColor.primary,
                            ),
                            onPressed: () {
                              setState(() {
                                isSelectCity = false;
                              });
                            },
                          ),
                          title: Consumer<LocationProvider>(
                              builder: (context, locationProvider, child) {
                            String? locationCity;
                            String? subLocation;

                            if (locationProvider.currentLocationName != null) {
                              locationCity = locationProvider
                                      .currentLocationName
                                      ?.administrativeArea ??
                                  "Unknown Location";
                              subLocation = locationProvider
                                      .currentLocationName?.street ??
                                  "...";
                            } else {
                              locationCity = "Unknown Location";
                              subLocation = "...";
                            }

                            return Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  locationCity,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  textAlign: TextAlign.start,
                                  style: const TextStyle(
                                      color: Colors.black,
                                      fontSize: 20,
                                      fontWeight: FontWeight.w700),
                                ),
                                Text(
                                  subLocation,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  textAlign: TextAlign.start,
                                  style: TextStyle(
                                      color: TColor.gray,
                                      fontSize: 16,
                                      fontWeight: FontWeight.w700),
                                ),
                              ],
                            );
                          }),
                        ),
                        SliverAppBar(
                          backgroundColor: Colors.white,
                          elevation: 1,
                          pinned: false,
                          floating: true,
                          primary: false,
                          automaticallyImplyLeading: false,
                          title: InkWell(
                            onTap: () => Navigator.push(
                              context,
                              MaterialPageRoute(
                                  builder: (context) => const SearchView()),
                            ),
                            child: const SearchBarView(),
                          ),
                        ),
                      ];
                    }, body: SingleChildScrollView(
                      child: Consumer<RestaurantProvider>(
                          builder: (context, resViewModel, child) {
                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            SelectionTextView(
                              title: "Gần bạn",
                              onSeeAllTap: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) =>
                                        const LegendryListView(),
                                  ),
                                );
                              },
                            ),
                            resViewModel.isLoadingHomeData
                                ? const Center(
                                    child: Padding(
                                      padding: EdgeInsets.symmetric(vertical: 16),
                                      child: CircularProgressIndicator(),
                                    ),
                                  )
                                : resViewModel.nearRestaurants.isEmpty
                                    ? const Padding(
                                        padding: EdgeInsets.symmetric(
                                            horizontal: 16, vertical: 8),
                                        child: Text(
                                          "Hiện chưa có nhà hàng phù hợp gần bạn.",
                                        ),
                                      )
                                : SizedBox(
                                        height: foodItemHorizontalStripHeight(
                                            context),
                                        child: ListView.builder(
                                        scrollDirection: Axis.horizontal,
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 8),
                                        itemCount:
                                            resViewModel.nearRestaurants.length,
                                        itemBuilder: (context, index) {
                                          var fObj = resViewModel
                                              .nearRestaurants[index];

                                          return GestureDetector(
                                            onTap: () {
                                              Navigator.push(
                                                  context,
                                                  MaterialPageRoute(
                                                      builder: (context) =>
                                                          RestaurantDetailView(
                                                            fObj: fObj,
                                                          )));
                                            },
                                            child: FoodItemCell(
                                              fObj: fObj,
                                            ),
                                          );
                                        },
                                      ),
                                      ),
                            const SizedBox(
                              height: 15,
                            ),
                            SelectionTextView(
                              title: "Đánh giá cao nhất",
                              onSeeAllTap: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) =>
                                        const LegendryListView(),
                                  ),
                                );
                              },
                            ),
                            resViewModel.isLoadingHomeData
                                ? const Center(
                                    child: Padding(
                                      padding: EdgeInsets.symmetric(vertical: 16),
                                      child: CircularProgressIndicator(),
                                    ),
                                  )
                                : resViewModel.topReviewRestaurant.isEmpty
                                    ? const Padding(
                                        padding: EdgeInsets.symmetric(
                                            horizontal: 16, vertical: 8),
                                        child: Text(
                                          "Chưa có dữ liệu đánh giá để hiển thị.",
                                        ),
                                      )
                                : SizedBox(
                                    // height: media.width,
                                    child: GridView.builder(
                                      shrinkWrap: true,
                                      physics:
                                          const NeverScrollableScrollPhysics(),
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 8),
                                      gridDelegate:
                                          SliverGridDelegateWithFixedCrossAxisCount(
                                        crossAxisCount: 2,
                                        crossAxisSpacing: 8.0,
                                        mainAxisSpacing: 8.0,
                                        childAspectRatio:
                                            foodItemGridChildAspectRatio(
                                                context),
                                      ),
                                      itemCount: resViewModel
                                          .topReviewRestaurant.length,
                                      itemBuilder: (context, index) {
                                        var fObj = resViewModel
                                            .topReviewRestaurant[index];

                                        return GestureDetector(
                                          onTap: () async {
                                            await Navigator.push(
                                              context,
                                              MaterialPageRoute(
                                                builder: (context) =>
                                                    RestaurantDetailView(
                                                  fObj: fObj,
                                                ),
                                              ),
                                            );
                                          },
                                          child: FoodItemCell(
                                            fObj: fObj,
                                          ),
                                        );
                                      },
                                    ),
                                  ),
                            const SizedBox(
                              height: 15,
                            )
                          ],
                        );
                      }),
                    ))
                  : Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        Image.asset("assets/img/home_page_icon.png",
                            width: media.width, height: media.width * .25),
                        SizedBox(
                          height: media.width * 0.04,
                        ),
                        Text(
                          "Xin chào! Rất vui được gặp bạn",
                          textAlign: TextAlign.center,
                          style: TextStyle(
                              color: TColor.text,
                              fontSize: 24,
                              fontWeight: FontWeight.w700),
                        ),
                        SizedBox(
                          height: media.width * 0.04,
                        ),
                        Text(
                          "Set your location to start exploring\nrestaurants around you",
                          textAlign: TextAlign.center,
                          style: TextStyle(
                              color: TColor.gray,
                              fontSize: 16,
                              fontWeight: FontWeight.w700),
                        ),
                        SizedBox(
                          height: media.width * 0.08,
                        ),
                        RoundButton(
                          title: "User current location",
                          type: RoundButtonType.primary,
                          onPressed: () async {
                            await Navigator.push(
                                context,
                                MaterialPageRoute(
                                    builder: (context) =>
                                        const SearchLocationView()));
                            setState(() {
                              isSelectCity = true;
                              endEditing();
                            });
                          },
                        ),
                      ],
                    ),
            );
          } else {
            return const NoInternetPage();
          }
        });
  }
}
