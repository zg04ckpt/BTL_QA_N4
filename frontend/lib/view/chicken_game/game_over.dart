import 'package:flame/components.dart';
import 'package:flame/input.dart';
import 'package:flutter/material.dart';

class GameOverComponent extends PositionComponent {
  final VoidCallback onRestart;

  GameOverComponent({required this.onRestart});

  @override
  Future<void> onLoad() async {
    super.onLoad();
    size = Vector2(296, 200); // Kích thước của thông báo game over
    position = Vector2(
      (size.x - size.x) / 2,
      (size.y - size.y) / 2,
    );

    // Thêm ảnh Game Over
    final gameOverImage = SpriteComponent(
      sprite: await Sprite.load("assets/img/chicken/game_over.png"),
      size: size,
    );
    add(gameOverImage);

    // Thêm nút RePlay
    final replayButton = SpriteButtonComponent(
      button: await Sprite.load("assets/img/chicken/replay.png"),
      // pressedSprite: await Sprite.load(Assets.imRePlay), // Tạo hiệu ứng nhấn nếu muốn
      size: Vector2(160, 50),
      position: Vector2((size.x - 160) / 2, size.y + 20),
      onPressed: onRestart,
    );
    add(replayButton);
  }
}
