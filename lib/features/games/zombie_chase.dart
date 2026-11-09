import 'dart:async';

import 'package:flutter/material.dart';
import 'package:cardio_care_quest/core/services/health_service.dart';
import 'package:cardio_care_quest/core/widgets/twine_game_host.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ZombieChaseGame extends StatefulWidget {
  final double targetDistance;

  const ZombieChaseGame({super.key, required this.targetDistance});

  @override
  State<ZombieChaseGame> createState() => _ZombieChaseGameState();
}

class _ZombieChaseGameState extends State<ZombieChaseGame> {
  Timer? _stepTimer;

  int? _lastKnownSteps;

  bool _isPolling = false;

  TwineGameHostController? _controller;

  // Called when the Twine game sends a message to Flutter.
  Future<bool> _handleBridgeMessage(
    Map<String, dynamic> data,
    TwineGameHostController controller,
  ) async {
    debugPrint('Zombie Chase: Received bridge message: ${data['type']}');
    switch (data['type']) {
      case 'START_STEP_TRACKING':
        await _startStepTracking(controller);
        return true;

      case 'STOP_STEP_TRACKING':
        _stopStepTracking();
        return true;

      case 'LOAD_ZOMBIE_DISTANCE':
        await _loadZombieDistance(controller);
        return true;

      case 'GET_ZOMBIE_DISTANCE':
        await _getZombieDistance(controller);
        return true;

      case 'SAVE_ZOMBIE_DISTANCE':
        await _saveZombieDistance(data);
        return true;

      case 'RESET_ZOMBIE_DISTANCE':
        await _resetZombieDistance();
        return true;

      default:
        return false;
    }
  }

  // Loads the saved zombie distance and sends it back to Twine.
  Future<void> _loadZombieDistance(TwineGameHostController controller) async {
    final prefs = await SharedPreferences.getInstance();

    // Use 20,000 if no saved distance exists.
    final savedDistance = prefs.getInt('zombie_distance') ?? 20000;

    debugPrint('Zombie Chase: Loaded distance = $savedDistance');

    // Send the saved distance to the Twine game.
    await controller.webView.runJavaScript(
      'SugarCube.setup.setZombieDistance($savedDistance)',
    );
  }

  // Loads the saved zombie distance and sends it back to Twine.
  Future<void> _getZombieDistance(TwineGameHostController controller) async {
    final prefs = await SharedPreferences.getInstance();

    // Use 20,000 if no saved distance exists.
    final savedDistance = prefs.getInt('zombie_distance') ?? 20000;

    debugPrint('Zombie Chase: Loaded distance = $savedDistance');

    // Send the saved distance to the Twine game.
    await controller.webView.runJavaScript(
      'SugarCube.setup.getZombieDistance($savedDistance)',
    );
  }

  // Saves the zombie distance received from Twine.
  Future<void> _saveZombieDistance(Map<String, dynamic> data) async {
    final distance = int.tryParse(data['distance'].toString());

    if (distance == null) {
      debugPrint('Zombie Chase: Invalid distance received for saving.');
      return;
    }

    final prefs = await SharedPreferences.getInstance();

    await prefs.setInt('zombie_distance', distance);

    debugPrint('Zombie Chase: Saved distance = $distance');
  }

  // Resets the saved distance to the starting value.
  Future<void> _resetZombieDistance() async {
    final prefs = await SharedPreferences.getInstance();

    await prefs.setInt('zombie_distance', 20000);

    debugPrint('Zombie Chase: Distance reset to 20000');
  }

  // Records the player's starting step count.
  Future<void> _startStepTracking(TwineGameHostController controller) async {
    // Prevent multiple tracking timers from starting.
    if (_stepTimer != null) return;

    _controller = controller;

    final startingSteps = await HealthService.instance.getStepsToday();

    if (!mounted) return;

    if (startingSteps == null) {
      debugPrint('Zombie Chase: Could not retrieve starting steps.');
      return;
    }

    _lastKnownSteps = startingSteps;

    debugPrint('Zombie Chase: Starting steps = $startingSteps');

    // Check for new steps every 10 seconds.
    _stepTimer = Timer.periodic(
      const Duration(seconds: 10),
      (_) => _pollSteps(),
    );
  }

  // Checks today's step count and calculates newly recorded steps.
  Future<void> _pollSteps() async {
    // Avoid overlapping requests.
    if (_isPolling || _lastKnownSteps == null) return;

    _isPolling = true;

    try {
      final currentSteps = await HealthService.instance.getStepsToday();

      debugPrint(
        'Zombie Chase: Step check. '
        'Previous: $_lastKnownSteps, Current: $currentSteps',
      );

      if (!mounted || currentSteps == null) return;

      final newSteps = currentSteps - _lastKnownSteps!;

      debugPrint('Zombie Chase: New steps detected = $newSteps');

      // Handle step-count corrections or a new day.
      if (newSteps < 0) {
        _lastKnownSteps = currentSteps;
        return;
      }

      if (newSteps == 0) return;

      // Send newly recorded steps to the Twine game.

      final result = await _controller?.webView.runJavaScriptReturningResult(
        'SugarCube.setup.incZombieDist($newSteps)',
      );

      debugPrint('Zombie Chase: Twine function response: $result');

      _lastKnownSteps = currentSteps;

      debugPrint('Zombie Chase: Added $newSteps steps.');
    } catch (e) {
      debugPrint('Zombie Chase step tracking error: $e');
    } finally {
      _isPolling = false;
    }
  }

  // Stops the step-tracking timer.
  void _stopStepTracking() {
    _stepTimer?.cancel();
    _stepTimer = null;

    _lastKnownSteps = null;

    debugPrint('Zombie Chase: Step tracking stopped.');
  }

  @override
  Widget build(BuildContext context) {
    return TwineGameHost(
      gameId: 'Zombie-chase',
      gameTitle: 'Zombie Chase',
      htmlAsset: 'assets/game/Zombie-chase.html',
      targetDistance: widget.targetDistance,
      enableGpsTracking: false,
      onCustomBridgeMessage: _handleBridgeMessage,
    );
  }

  @override
  void dispose() {
    _stopStepTracking();
    super.dispose();
  }
}
