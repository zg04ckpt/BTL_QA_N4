import 'package:cp_restaurants/common_widget/no_internet_page.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../common/color_extension.dart';
import '../../common_widget/discovery_cell.dart';
import 'list_restaurant_view.dart';

class DiscoveryView extends StatefulWidget {
  const DiscoveryView({super.key});

  @override
  State<DiscoveryView> createState() => _DiscoveryViewState();
}

class _DiscoveryViewState extends State<DiscoveryView> {
  TextEditingController txtSearch = TextEditingController();

  bool isLoading = false;

  @override
  Widget build(BuildContext context) {
    // var media = MediaQuery.of(context).size;
    return Selector<CommonProvider, bool>(
        selector: (context, commom) => commom.isConnect,
        builder: (context, isConnect, child) {
          if (isConnect) {
            return Stack(
              children: [
                Scaffold(
                  backgroundColor: TColor.bg,
                  body: NestedScrollView(
                    headerSliverBuilder: (context, innerBoxIsScrolled) {
                      return [
                        SliverAppBar(
                          backgroundColor: Colors.white,
                          elevation: 0,
                          automaticallyImplyLeading: false,
                          pinned: true,
                          floating: false,
                          centerTitle: false,
                          leadingWidth: 0,
                          title: Row(
                            children: [
                              Image.asset(
                                "assets/img/discovery_icon.png",
                                width: 30,
                                height: 30,
                                fit: BoxFit.contain,
                              ),
                              const SizedBox(
                                width: 8,
                              ),
                              Text(
                                "Khám phá",
                                textAlign: TextAlign.left,
                                style: TextStyle(
                                    color: TColor.text,
                                    fontSize: 32,
                                    fontWeight: FontWeight.w700),
                              ),
                            ],
                          ),
                        ),
                      ];
                    },
                    body: GridView.builder(
                        padding: const EdgeInsets.symmetric(
                            vertical: 15, horizontal: 12),
                        gridDelegate:
                            const SliverGridDelegateWithFixedCrossAxisCount(
                                crossAxisCount: 2,
                                crossAxisSpacing: 12,
                                mainAxisSpacing: 12,
                                childAspectRatio: 1),
                        itemCount: restaurantTypes.keys.toList().length,
                        itemBuilder: (context, index) {
                          return GestureDetector(
                              onTap: () async {
                                setState(() {
                                  isLoading = true;
                                });
                                var result = await context
                                    .read<RestaurantProvider>()
                                    .getRestaurantByCategory(
                                        restaurantTypes.keys.toList()[index]);
                                setState(() {
                                  isLoading = false;
                                });
                                if(result.isEmpty){
                                  return;
                                }
                                Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                        builder: (context) =>
                                            ListRestaurantView(
                                              resDatas: result,
                                            )));
                              },
                              child: DiscoveryCell(
                                name: restaurantTypes.keys.toList()[index],
                              ));
                        }),
                  ),
                ),
                if (isLoading)
                  Material(
                    color: Colors.green.withOpacity(0.2),
                    child: Container(
                      height: 50,
                      width: 80,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(12),
                        color: Colors.white,
                      ),
                    ),
                  )
              ],
            );
          } else {
            return const NoInternetPage();
          }
        });
  }
}
