import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/view/restaurant/restaurant_detail_view.dart';
import 'package:cp_restaurants/view/search/res_search_item.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';
import 'package:cp_restaurants/common_widget/line_textfield.dart';

class SearchView extends StatefulWidget {
  const SearchView({super.key});

  @override
  State<SearchView> createState() => _SearchViewState();
}

class _SearchViewState extends State<SearchView> {
  TextEditingController textController = TextEditingController();
  bool _hasSubmittedSearch = false;

  @override
  void initState() {
    super.initState();

    // Initialize the search results to be empty
    // textController.addListener(() {
    //   // Huỷ the previous timer if it exists
    //   if (_debounce?.isActive ?? false) {
    //     _debounce!.cancel();
    //   }

    //   // Start a new timer with a 0.5s delay
    //   _debounce = Timer(const Duration(milliseconds: 500), () {
    //     String query = textController.text.trim();
    //     // if (query.isNotEmpty) {

    // }
    // });
    // });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        centerTitle: true,
        title: const Text(
          "Searching...",
          style: TextStyle(
            color: Colors.black,
            fontSize: 20,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20),
        child: Column(
          children: [
            RoundTextField(
              onSubmitted: (p0) {
                setState(() => _hasSubmittedSearch = true);
                Provider.of<RestaurantProvider>(context, listen: false)
                    .searchRestaurants(p0);
              },
              controller: textController,
              hitText: "Tìm kiếm nhà hàng…",
              leftIcon: InkWell(
                  onTap: () {
                    setState(() => _hasSubmittedSearch = true);
                    Provider.of<RestaurantProvider>(context, listen: false)
                        .searchRestaurants(textController.text);
                  },
                  child: const Icon(Icons.search, color: Colors.grey)),
            ),
            const SizedBox(height: 20),
            Expanded(
              child: Consumer<RestaurantProvider>(
                builder: (context, provider, child) {
                  if (!_hasSubmittedSearch && !provider.isSearching) {
                    return Center(
                      child: Text(
                        'Nhập tên nhà hàng hoặc thành phố, nhấn tìm hoặc Enter.',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: Colors.grey.shade700,
                          fontSize: 15,
                        ),
                      ),
                    );
                  }
                  if (provider.searchedRestaurants.isEmpty &&
                      !provider.isSearching) {
                    return const Center(
                      child: Text('Không có nhà hàng phù hợp.'),
                    );
                  }
                  if (provider.isSearching) {
                    return const Center(
                      child: CircularProgressIndicator(),
                    );
                  }

                  return ListView.builder(
                    itemCount: provider.searchedRestaurants.length,
                    itemBuilder: (context, index) {
                      Restaurant restaurant =
                          provider.searchedRestaurants[index];
                      return InkWell(
                        onTap: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => RestaurantDetailView(
                              fObj: restaurant,
                            ),
                          ),
                        ),
                        child: SizedBox(
                          // height: 1÷,
                          width: double.infinity,
                          child: ResSearchItem(
                            fObj: restaurant,
                          ),
                        ),
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
