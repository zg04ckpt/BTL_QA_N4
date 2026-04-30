import 'package:flutter/material.dart';
import 'package:cp_restaurants/data/models/restaurant.dart';

import '../../common_widget/food_item_cell.dart';
import '../restaurant/restaurant_detail_view.dart';

class ListRestaurantView extends StatefulWidget {
  const ListRestaurantView({super.key, required this.resDatas});

  final List<Restaurant> resDatas;

  @override
  State<ListRestaurantView> createState() => _ListRestaurantViewState();
}

class _ListRestaurantViewState extends State<ListRestaurantView> {
  List<Restaurant> filteredRestaurants = [];
  double selectedScore = 1; // Điểm đánh giá được chọn
  bool isNearest = false; // Lọc gần nhất hay không

  @override
  void initState() {
    super.initState();
    filteredRestaurants = widget.resDatas; // Khởi tạo danh sách ban đầu
  }

  // Hàm áp dụng filter
  void applyFilter() {
    setState(() {
      filteredRestaurants = widget.resDatas.where((restaurant) {
        // Lọc theo điểm đánh giá
        bool matchesScore = restaurant.averageScore >= selectedScore;

        // Lọc theo khoảng cách
        if (isNearest) {
          filteredRestaurants.sort((a, b) => a.distance.compareTo(b.distance));
        }

        return matchesScore;
      }).toList();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: Text(widget.resDatas[0].category),
        centerTitle: true,
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: Row(
              children: [
                // Dropdown chọn điểm đánh giá
                Expanded(
                  child: DropdownButton<double>(
                    isExpanded: true,
                    value: selectedScore,
                    items: List.generate(
                      5,
                      (index) => DropdownMenuItem(
                        value: (index + 1).toDouble(),
                        child: Text('${index + 1} sao'),
                      ),
                    ),
                    onChanged: (value) {
                      if (value != null) {
                        selectedScore = value;
                        applyFilter();
                      }
                    },
                  ),
                ),
                const SizedBox(width: 8),
                // Toggle lọc gần nhất
                Expanded(
                  child: ElevatedButton.icon(
                    icon: Icon(
                      isNearest ? Icons.check_circle : Icons.radio_button_unchecked,
                    ),
                    label: Text("Gần nhất"),
                    onPressed: () {
                      setState(() {
                        isNearest = !isNearest;
                        applyFilter();
                      });
                    },
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: GridView.builder(
              shrinkWrap: true,
              padding: const EdgeInsets.symmetric(horizontal: 8),
              gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                crossAxisSpacing: 8.0,
                mainAxisSpacing: 8.0,
                childAspectRatio: foodItemGridChildAspectRatio(context),
              ),
              itemCount: filteredRestaurants.length,
              itemBuilder: (context, index) {
                var fObj = filteredRestaurants[index];
                return GestureDetector(
                  onTap: () async {
                    await Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => RestaurantDetailView(
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
        ],
      ),
    );
  }
}
