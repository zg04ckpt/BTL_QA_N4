
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../common/color_extension.dart';
import '../../common_widget/line_textfield.dart';
import '../../common_widget/near_by_list_row.dart';
import '../../common_widget/popup_layout.dart';
import '../restaurant/restaurant_detail_view.dart';
import '../discovery/filter_view.dart';
import '../../common_widget/login_required.dart';

class BookmarkView extends StatefulWidget {
  const BookmarkView({super.key});

  @override
  State<BookmarkView> createState() => _BookmarkViewState();
}

class _BookmarkViewState extends State<BookmarkView> {
  TextEditingController txtSearch = TextEditingController();
  String searchQuery = "";

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<RestaurantProvider>().getBookmarkRestaurants();
    });
  }

  void updateSearchQuery(String query) {
    setState(() {
      searchQuery = query;
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
          body: Consumer<RestaurantProvider>(
            builder: (context, provider, child) {
              final bookmarkedRestaurants = provider.favoriteRestaurants;
              final filteredRestaurants = searchQuery.isEmpty
                  ? bookmarkedRestaurants
                  : bookmarkedRestaurants
                      .where((restaurant) => restaurant.name
                          .toLowerCase()
                          .contains(searchQuery.toLowerCase()))
                      .toList();

              if (provider.isLoadingFavorites) {
                return const Center(child: CircularProgressIndicator());
              }
              if (provider.favoriteLoadError != null) {
                return Center(
                    child: Text("Lỗi tải danh sách đã lưu: ${provider.favoriteLoadError}"));
              }
              if (filteredRestaurants.isEmpty) {
                return const Center(child: Text("Không tìm thấy nhà hàng"));
              }
              return Stack(
                  alignment: Alignment.topCenter,
                  children: [
                    ListView.builder(
                      itemCount: filteredRestaurants.length,
                      itemBuilder: (context, index) {
                        final restaurant = filteredRestaurants[index];
                        return NearByListRow(
                          fObj: restaurant,
                          isBookmark: true,
                          onTap: () {
                            Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (_) => RestaurantDetailView(fObj: restaurant),
                              ),
                            );
                          },
                          onBookmarkToggle: () async {
                            await provider.setBookmarkRestaurants(restaurant);
                          },
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
                );
            },
          ),
        ),
      );
    } else {
      return const LoginRequired();
    }
  }
}
