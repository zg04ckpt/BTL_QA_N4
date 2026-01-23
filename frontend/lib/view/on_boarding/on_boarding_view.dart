import 'package:cp_restaurants/view/auth/login_view.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:cp_restaurants/view/main_tab/main_tab_view.dart';
import 'package:flutter/material.dart';
import '../../common/color_extension.dart';
import '../../common_widget/round_button.dart';

class OnBoardingView extends StatefulWidget {
  const OnBoardingView({super.key});

  @override
  State<OnBoardingView> createState() => _OnBoardingViewState();
}

class _OnBoardingViewState extends State<OnBoardingView> {
  int selectPage = 0;

  PageController? controller = PageController();

  List infoArr = [
    {
      "title": "Tìm kiếm nhanh chóng",
      "sub_title": "Tìm kiếm nhà hàng gần vị trí của bạn",
      "icon": "assets/img/2.png"
    },
    {
      "title": "Đánh giá trực quan",
      "sub_title":
          "Hệ thống đánh giá trực quan, công khai và minh bạch",
      "icon": "assets/img/1.png"
    },
    {
      "title": "Đa dạng món ăn",
      "sub_title":
          "Đa dạng kiểu nhà hàng phù hợp với mọi nhu cầu",
      "icon": "assets/img/3.png"
    },
    {
      "title": "Quản lý dễ dàng",
      "sub_title":
          "Dễ dàng quản lý và quảng bá nhà hàng của bạn !",
      "icon": "assets/img/4.png"
    }
  ];

  @override
  void initState() {
    controller?.addListener(() {
      selectPage = controller?.page?.round() ?? 0;
      if (mounted) {
        setState(() {});
      }
    });
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    var media = MediaQuery.of(context).size;
    return Scaffold(
      backgroundColor: TColor.primary,
      body: SafeArea(
        child: Stack(children: [
          PageView.builder(
              controller: controller,
              itemCount: infoArr.length,
              itemBuilder: (context, index) {
                var iObj = infoArr[index] as Map? ?? {};

                return Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Image.asset(
                      iObj["icon"].toString(),
                      width: media.width * 0.56,
                      height: media.width * 0.5,
                      fit: BoxFit.fill,
                    ),
                    SizedBox(
                      height: media.width * 0.13,
                    ),
                    Text(
                      iObj["title"].toString(),
                      style: const TextStyle(
                          color: Colors.white,
                          fontSize: 24,
                          fontWeight: FontWeight.w700),
                    ),
                    SizedBox(
                      height: media.width * 0.03,
                    ),
                    Text(
                      iObj["sub_title"].toString(),
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                          color: Colors.white,
                          fontSize: 16,
                          fontWeight: FontWeight.w700),
                    ),
                  ],
                );
              }),
          Column(
            mainAxisAlignment: MainAxisAlignment.end,
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              RoundButton(
                title: "Đăng nhập",
                onPressed: () {
                  Navigator.push(
                      context,
                      MaterialPageRoute(
                          builder: (context) => const LoginView()));
                },
              ),
              SizedBox(height: media.width * 0.01),
              TextButton(
                child: const Text(
                  'Đăng ký tài khoản',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 18,
                    decoration: TextDecoration.underline,
                    decorationColor: Colors.white,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                onPressed: () {
                  Navigator.push(
                      context,
                      MaterialPageRoute(
                          builder: (context) => const SignUpView()));
                },
              ),
              SizedBox(height: media.width * 0.05),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: infoArr.map((iObj) {
                  var index = infoArr.indexOf(iObj);

                  return Container(
                    margin: const EdgeInsets.all(8),
                    width: 15,
                    height: 15,
                    decoration: BoxDecoration(
                      color:
                          selectPage == index ? Colors.white : Colors.white54,
                      borderRadius: BorderRadius.circular(7.5),
                    ),
                  );
                }).toList(),
              ),
            ],
          ),
        ]),
      ),
    );
  }
}
