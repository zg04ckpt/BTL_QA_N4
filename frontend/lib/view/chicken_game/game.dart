import 'dart:developer';

import 'package:cp_restaurants/view/chicken_game/chicken_game_filter.dart';
import 'package:flame/components.dart';
import 'package:flame/events.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';

import 'chicken.dart';
import 'road.dart';

class MyFlameGame extends FlameGame with TapCallbacks {
  late Chicken chicken;
  late Road road;
  late SpriteComponent winMessage;
  late SpriteComponent loseMessage;

  static const double roadSegmentWidth = 100.0;
  double tapDuration = 0.0;
  final VoidCallback onCompleteGame;
  final int mapIndex;

  late SpriteComponent wave1;
  late SpriteComponent wave2;
  late SpriteComponent wave3;

  static const double waveHeight = 30.0;
  static const double waveSpeed = 100.0;

  MyFlameGame({
    required this.onCompleteGame,
    required this.mapIndex,
  });

  bool isGameWon = false;
  @override
  Future<void> onLoad() async {
    log("map index: $mapIndex");
    List<double> customWidths =
        gameMap[mapIndex]["customWidths"]!;
    List<double> customRoadHeights =
         gameMap[mapIndex]["customRoadHeights"]!;

    wave3 = SpriteComponent(
      sprite: await Sprite.load('filter/chicken/wave_3.png'),
      position: Vector2(0, size.y - waveHeight - 40),
      size: Vector2(size.x * 2, waveHeight + 40),
    );

    wave2 = SpriteComponent(
      sprite: await Sprite.load('filter/chicken/wave_2.png'),
      position: Vector2(0, size.y - waveHeight - 20),
      size: Vector2(size.x * 2, waveHeight + 20),
    );

    wave1 = SpriteComponent(
      sprite: await Sprite.load('filter/chicken/wave_1.png'),
      position: Vector2(0, size.y - waveHeight),
      size: Vector2(size.x * 2, waveHeight),
    );

    road = Road(
      size.x * 4,
      roadSegmentWidth,
      customWidths,
      customRoadHeights,
      mapIndex + 1,
    );
    add(wave3);

    add(road);
    await road.onLoad();

    add(wave2);
    add(wave1);

    double chickenYPosition = road.roadYPosition;
    chicken = Chicken(Vector2(size.x / 4, chickenYPosition - 100));
    add(chicken);

    camera.follow(road);

    winMessage = SpriteComponent(
      sprite: await Sprite.load('filter/chicken/win.png'),
      position: Vector2(size.x / 4, size.y / 3),
      size: Vector2(200, 200),
    );
    loseMessage = SpriteComponent(
      sprite: await Sprite.load('filter/chicken/game_over.png'),
      position: Vector2(size.x / 4, size.y / 3),
      size: Vector2(200, 200),
    );
  }

  @override
  void onTapDown(TapDownEvent event) {
    if (isGameWon) return;
    super.onTapDown(event);
    tapDuration = 0.0;
    road.moveRoad(chicken);
    chicken.startJump();
  }

  @override
  void onTapUp(TapUpEvent event) {
    if (isGameWon) return;
    super.onTapUp(event);
    chicken.stopJump(tapDuration);
  }

  @override
  void update(double dt) {
    super.update(dt);

    if (tapDuration > 0.0) {
      tapDuration += dt;
    }

    if (chicken.isJumping || chicken.isFalling) {
      road.moveRoad(chicken);
    }

    const double waveSpeed = 60;

    final waves = [
      {'component': wave1, 'direction': -1},
      {'component': wave2, 'direction': 1},
      {'component': wave3, 'direction': -1},
    ];

    for (var waveData in waves) {
      SpriteComponent wave = waveData['component'] as SpriteComponent;
      int direction = waveData['direction'] as int;

      wave.position.x += direction * waveSpeed * dt;

      if (direction == -1 && wave.position.x <= -size.x) {
        wave.position.x = 0;
      } else if (direction == 1 && wave.position.x >= 0) {
        wave.position.x = -size.x;
      }
    }
  }

  Future<void> showWinDialog() async {
    await Future.delayed(const Duration(seconds: 1));
    isGameWon = true;
    add(winMessage);
    onCompleteGame.call();
  }

  Future<void> showGameOver() async {
    await Future.delayed(const Duration(seconds: 1));
    isGameWon = true;
    add(loseMessage);
    onCompleteGame.call();
  }
}
