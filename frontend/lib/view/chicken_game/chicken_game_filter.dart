import 'dart:math';

import 'package:cp_restaurants/view/chicken_game/game.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';

final List<Map<String, List<double>>> gameMap = [
    {
      "customWidths": [300.0, 100.0, 200.0, 100.0, 200.0],
      "customRoadHeights": [164.0, 252.0, 200.0, 252.0, 248.0],
    },
    {
      "customWidths": [300.0, 100.0, 200.0, 100.0, 200.0],
      "customRoadHeights": [164.0, 116.0, 200.0, 252.0, 200.0],
    },
    {
      "customWidths": [300.0, 100.0, 100.0, 200.0, 200.0],
      "customRoadHeights": [164.0, 252.0, 180.0, 216.0, 321.0],
    },
  ];

class ChickenGameFilter extends StatefulWidget {
  const ChickenGameFilter({super.key});

  @override
  State<ChickenGameFilter> createState() => _ChickenGameFilterState();
}

class _ChickenGameFilterState extends State<ChickenGameFilter> {
  bool isRecording = false;
  bool isComplete = false;
  int limitTime = 60;
  int gameIndex = 0;

  @override
  void initState() {
    loadGameMap();
    super.initState();
  }

  void loadGameMap() {
    Random random = Random();
    gameIndex = random.nextInt(2);
  }

  @override
  void dispose() {
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          SizedBox(
            height: double.infinity,
            width: double.infinity,
            child: GameWidget(
              game: MyFlameGame(
                mapIndex: gameIndex,
                onCompleteGame: () {
                  // setState(() {
                  //   isComplete = true;
                  // });
                },
              ),
            ),
          ),
          if (!isRecording || isComplete)
            Container(
              height: double.infinity,
              width: double.infinity,
              color: Colors.white.withOpacity(0.01),
            )
        ],
      ),
    );
  }
}
