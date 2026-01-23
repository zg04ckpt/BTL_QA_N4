import 'package:flutter/material.dart';

class NoInternetPage extends StatefulWidget {
  static const String routeName = '/NoInternetPage';
  const NoInternetPage({super.key});

  @override
  State<NoInternetPage> createState() => _NoInternetPageState();
}

class _NoInternetPageState extends State<NoInternetPage> {
  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
  }

  bool reconnecting = false;

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      child: SizedBox(
        child: Center(
          child: Container(
            padding: const EdgeInsets.all(32),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(16),
              color: const Color(0xffb2dcf2),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Image.asset(
                  "assets/img/no_internet.jpg",
                  width: 285,
                ),
                const SizedBox(height: 16),
                reconnecting
                    ? const CircularProgressIndicator(
                        color: Colors.red,
                      )
                    : InkWell(
                        onTap: () async {
                          setState(() {
                            reconnecting = true;
                          });
                          Future.delayed(const Duration(milliseconds: 1000))
                              .then(
                            (value) {
                              if (context.mounted) {
                                setState(() {
                                  reconnecting = false;
                                });
                              }
                            },
                          );
                        },
                        child: Container(
                          height: 50,
                          width: 285,
                          decoration: BoxDecoration(
                              borderRadius: BorderRadius.circular(12),
                              gradient: const LinearGradient(
                                  colors: [
                                    Color(0xffFFAE00),
                                    Color(0xffFF9500)
                                  ],
                                  begin: Alignment.centerLeft,
                                  end: Alignment.centerRight)),
                          child: const Center(
                            child: Text(
                              'Thử lại',
                              style: TextStyle(
                                  fontSize: 14, fontWeight: FontWeight.bold),
                            ),
                          ),
                        ),
                      ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
