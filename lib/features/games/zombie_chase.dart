import 'package:flutter/material.dart';

import 'package:cardio_care_quest/core/widgets/twine_game_host.dart';

// Loads the Dog Walking HTML game in TwineGameHost, which handles GPS tracking.
class ZombieChaseGame extends StatelessWidget {
  final double targetDistance;

  const ZombieChaseGame({super.key, required this.targetDistance});

  @override
  Widget build(BuildContext context) {
    return TwineGameHost(
      gameId: 'Zombie-chase',
      gameTitle: 'Zombie Chase',
      htmlAsset: 'assets/game/Zombie-chase.html',
      targetDistance: targetDistance,
    );
  }
}
