import 'dart:developer';
import 'package:flame/components.dart';
import 'game.dart';


class Chicken extends SpriteComponent {
  static const double maxJumpHeight = 500.0;
  double velocityY = 0;
  bool isJumping = false;
  bool isFalling = false;
  bool isOutOfScreen = false;

  // Thêm các thuộc tính để lưu các hình ảnh
  late Sprite chickenStandSprite;
  late Sprite chickenFlySprite;

  Chicken(Vector2 position)
      : super(
          position: position,
          size: Vector2(64, 86),
          anchor: Anchor.center,
        );

  @override
  Future<void> onLoad() async {
    super.onLoad();

    // Tải hình ảnh cho các trạng thái
    chickenStandSprite = await Sprite.load('filter/chicken/chicken_stand.png');
    chickenFlySprite = await Sprite.load('filter/chicken/chicken_fly.png');

    // Mặc định là con gà đứng im
    sprite = chickenStandSprite;
  }

  void stopJump(double tapDuration) {
    isJumping = false;
    isFalling = true;

    if (tapDuration < 0.1) {
      velocityY = 0;
    } else {
      if (tapDuration < 0.3) {
        velocityY = -300;
      } else if (tapDuration < 0.6) {
        velocityY = -500;
      } else {
        velocityY = -700;
      }
    }
  }

  void startJump() {
    if (!isJumping && !isFalling && !isOutOfScreen) {
      isJumping = true;
      velocityY = -500;
      isFalling = false;
      sprite = chickenFlySprite; // Đổi sang hình ảnh khi nhảy
    }
  }

  void resetPosition(Vector2 startPosition) {
    position = startPosition;
    velocityY = 0;
    isJumping = false;
    isFalling = false;
    isOutOfScreen = false;
    sprite = chickenStandSprite; // Đặt lại hình ảnh khi reset
  }

  @override
  void update(double dt) {
    super.update(dt);

    if (isJumping) {
      velocityY += 500 * dt;
      position.y += velocityY * dt;

      if (position.y <= (parent as MyFlameGame).size.y - 100 - maxJumpHeight) {
        position.y = (parent as MyFlameGame).size.y - 100 - maxJumpHeight;
        velocityY = 0;
        isFalling = true;
      }
    } else if (isFalling) {
      velocityY += 600 * dt;
      position.y += velocityY * dt;

      double groundHeight =
          (parent as MyFlameGame).road.getGroundHeightAt(position.x);

      if (position.y >= groundHeight) {
        position.y = groundHeight;
        velocityY = 0;
        isFalling = false;

        if ((parent as MyFlameGame).road.isChickenInGap(this)) {
          log("Game Over! Chicken fell into the gap.");
          (parent as MyFlameGame).showGameOver();
          isOutOfScreen = true;
        }
      }
    } else {
      sprite = chickenStandSprite; // Đặt lại hình ảnh khi đứng im
    }

    if (isOutOfScreen) {
      velocityY += 600 * dt;
      position.y += velocityY * dt;
    }
  }
}
