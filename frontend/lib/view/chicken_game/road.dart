import 'chicken.dart';
import 'game.dart';
import 'package:flame/components.dart';

class Road extends PositionComponent {
  final double totalWidth;
  final double segmentWidth;
  final int mapIndex;

  double offsetX = 0.0;
  double speed = 50.0;
  List<SpriteComponent> roadSegments = [];
  List<double> customSegmentWidths = [];
  
  List<double> customSegmentSizeH = [];

  late SpriteComponent winFlag;

  Road(this.totalWidth, this.segmentWidth, this.customSegmentWidths,
      this.customSegmentSizeH, this.mapIndex) {
    size = Vector2(totalWidth, 20);
  }

  double get roadYPosition => position.y;

  @override
  void onGameResize(Vector2 size) {
    super.onGameResize(size);
    position = Vector2(0, size.y - 252);
  }

  @override
  Future<void> onLoad() async {
    super.onLoad();

    final sprite1 = await Sprite.load('filter/chicken/road1.png');
    final sprite2 = await Sprite.load('filter/chicken/road2_$mapIndex.png');
    final sprite3 = await Sprite.load('filter/chicken/road3_$mapIndex.png');
    final sprite4 = await Sprite.load('filter/chicken/road4_$mapIndex.png');
    final sprite5 = await Sprite.load('filter/chicken/road5_$mapIndex.png');

    List<Sprite> sprites = [sprite1, sprite2, sprite3, sprite4, sprite5];

    double xPos = 0;
    double yPos = 0;

    for (int i = 0; i < sprites.length; i++) {
      
      double segmentWidth = i < customSegmentWidths.length
          ? customSegmentWidths[i]
          : this.segmentWidth;

      
      
      
      
      yPos = 252 - customSegmentSizeH[i];
      var segment = SpriteComponent(
        sprite: sprites[i],
        position: Vector2(xPos, yPos),
        size: Vector2(segmentWidth, customSegmentSizeH[i]),
      );

      roadSegments.add(segment);
      add(segment);

      xPos += segmentWidth + 80; 
      yPos -= 0; 
    }
    winFlag = SpriteComponent(
      sprite: await Sprite.load('filter/chicken/win_flag.png'),
      position: Vector2(xPos - 100, yPos -45), 
      size: Vector2(50, 50),
    );
    add(winFlag);
  }

  void checkWin(Chicken chicken) {
    
    final winFlagRect = winFlag.toAbsoluteRect();
    final chickenRect = chicken.toAbsoluteRect();

    
    if (chickenRect.overlaps(winFlagRect)) {
      (parent as MyFlameGame).showWinDialog();
    }
  }

  void moveRoad(Chicken chicken) {
    if (chicken.isJumping) {
      speed = 100.0;
    } else if (chicken.isFalling) {
      speed = 30.0;
    } else {  
      speed = 50.0;
    }

    offsetX += speed * 0.016;
    position = Vector2(-offsetX, position.y);

    if (offsetX >= totalWidth - segmentWidth) {
      offsetX = 0;
    }
    checkWin(chicken);
  }

  bool isChickenInGap(Chicken chicken) {
    double chickenLocalX = chicken.position.x + offsetX;

    for (var segment in roadSegments) {
      
      bool isChickenOnSegment = (chickenLocalX >= segment.position.x &&
              chickenLocalX <= segment.position.x + segment.size.x) &&
          (chicken.position.y >=
                  position.y + segment.position.y - segment.size.y &&
              chicken.position.y <= position.y + segment.position.y);

      if (isChickenOnSegment) {
        return false; 
      }
    }
    return true; 
  }

  double getGroundHeightAt(double chickenX) {
    
    double chickenLocalX = chickenX + offsetX;

    for (var segment in roadSegments) {
      
      if (chickenLocalX >= segment.position.x &&
          chickenLocalX <= segment.position.x + segment.size.x) {
        
        return position.y + segment.position.y - 35;
      }
    }
    
    return position.y;
  }
}
