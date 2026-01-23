
import 'package:cp_restaurants/global/global_data.dart';
import 'package:flutter/material.dart';
import '../../common/color_extension.dart';
import '../../common_widget/line_textfield.dart';
import '../../common_widget/near_by_list_row.dart';
import '../../common_widget/popup_layout.dart';
import '../../data/models/restaurant.dart';
import '../../data/repository/restaurant_helper.dart';
import '../discovery/filter_view.dart';
import '../../common_widget/login_required.dart';

class BookmarkView extends StatefulWidget {
  const BookmarkView({super.key});

  @override
  State<BookmarkView> createState() => _BookmarkViewState();
}

class _BookmarkViewState extends State<BookmarkView> {
  TextEditingController txtSearch = TextEditingController();
  List<Restaurant> bookmarkedRestaurants = [];

  List<Restaurant> filteredRestaurants = [];
  String searchQuery = "";

  @override
  void initState() {
    super.initState();
    _fetchBookmarkedRestaurants();
  }

  Future<void> _fetchBookmarkedRestaurants() async {
    final helper = RestaurantHelper();
    final restaurants = await helper.fetchAllRestaurants();

    setState(() {
      bookmarkedRestaurants = restaurants;
      filteredRestaurants = restaurants;
    });
  }

  void updateSearchQuery(String query) {
    if (query == "") {
      setState(() {
        filteredRestaurants = bookmarkedRestaurants;
      });
      return;
    }
    setState(() {
      searchQuery = query;
      filteredRestaurants = bookmarkedRestaurants
          .where((restaurant) =>
              restaurant.name.toLowerCase().contains(searchQuery.toLowerCase()))
          .toList();
    });
  }

  @override
  Widget build(BuildContext context) {
    if (GlobalData.instance.userData!=null) {
      return Scaffold(
        backgroundColor: TColor.bg,
        body: NestedScrollView(
          headerSliverBuilder: (context, innerBoxIsScrolled) {
            return [
              SliverAppBar(
                backgroundColor: Colors.white,
                elevation: 0,
                pinned: true,
                floating: false,
                centerTitle: false,
                leadingWidth: 0,
                automaticallyImplyLeading: false,
                title: Row(
                  children: [
                    Image.asset(
                      "assets/img/bookmark_icon.png",
                      width: 30,
                      height: 30,
                      fit: BoxFit.contain,
                    ),
                    const SizedBox(width: 8),
                    Text(
                      "Đã lưu",
                      textAlign: TextAlign.left,
                      style: TextStyle(
                          color: TColor.text,
                          fontSize: 32,
                          fontWeight: FontWeight.w700),
                    ),
                  ],
                ),
              ),
              SliverAppBar(
                backgroundColor: Colors.white,
                elevation: 1,
                pinned: false,
                automaticallyImplyLeading: false,
                floating: true,
                primary: false,
                expandedHeight: 50,
                flexibleSpace: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 15),
                  child: RoundTextField(
                    onChanged: updateSearchQuery,
                    onSubmitted: updateSearchQuery,
                    controller: txtSearch,
                    hitText: "Tìm kiếm nhà hàng…",
                    leftIcon: Icon(Icons.search, color: TColor.gray),
                  ),
                ),
              ),
            ];
          },
          body: filteredRestaurants.isEmpty
              ? const Center(child: Text("Không tìm thấy nhà hàng"))
              : Stack(
                  alignment: Alignment.topCenter,
                  children: [
                    ListView.builder(
                      itemCount: filteredRestaurants.length,
                      itemBuilder: (context, index) {
                        final restaurant = filteredRestaurants[index];
                        return NearByListRow(
                          fObj: restaurant,
                          isBookmark: true,
                        );
                      },
                    ),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 8),
                          child: TextButton(
                            onPressed: () {
                              Navigator.push(
                                context,
                                PopupLayout(child: const FilterView()),
                              );
                            },
                            child: Text(
                              "Filter",
                              style: TextStyle(
                                  color: TColor.primary,
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700),
                            ),
                          ),
                        )
                      ],
                    ),
                  ],
                ),
        ),
      );
    } else {
      return const LoginRequired();
    }
  }
}
