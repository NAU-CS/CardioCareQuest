// Plays a "health journey": the sequence of native mini-games a patient built.
// Each picked part is a real playable step wired to its own health hook (walk,
// plate, water, meds, BP, breathing). The journey handles the shared session
// bookkeeping once at the end; each step logs its own health data as it plays.

import 'dart:async';
import 'dart:math' as math;

import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:get_it/get_it.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/firestore_paths.dart';
import '../../../core/hooks/hooks.dart';
import '../../../core/providers/user_data_manager.dart';
import '../../../core/services/location_service.dart';
import '../../../core/services/offline_queue.dart';
import 'custom_game.dart';
import 'custom_games_repository.dart';
import 'water_reminder_service.dart';

// Shared palette so every step looks like one game.
const _bgGradient = LinearGradient(
  begin: Alignment.topCenter,
  end: Alignment.bottomCenter,
  colors: [Color(0xFF1a3a5c), Color(0xFF2a5074), Color(0xFF3a6a94)],
);
const _accent = Color(0xFFfde725);

// Where in the journey we are.
enum _Phase { intro, playing, done }

class CustomJourneyPlayer extends StatefulWidget {
  final CustomGame game;
  const CustomJourneyPlayer({super.key, required this.game});

  @override
  State<CustomJourneyPlayer> createState() => _CustomJourneyPlayerState();
}

class _CustomJourneyPlayerState extends State<CustomJourneyPlayer> {
  _Phase _phase = _Phase.intro;
  int _index = 0; // which step is showing
  late final DateTime _startedAt;
  late final String _sessionId;
  bool _finished = false; // so completion hooks fire once

  List<GameSegment> get _segments => widget.game.segments;
  String get _surveyId => 'custom_${widget.game.id}';

  @override
  void initState() {
    super.initState();
    _startedAt = DateTime.now();
    _sessionId = '${_surveyId}_${_startedAt.millisecondsSinceEpoch}';

    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    // ignore: unawaited_futures
    TelemetryHooks.logEvent(
      'custom_game_opened',
      parameters: {
        'gameId': widget.game.id,
        'sessionId': _sessionId,
        'gameType': 'journey',
        'stepCount': _segments.length,
      },
      userId: uid.isEmpty ? null : uid,
    );
  }

  // Move to the next step, or finish if this was the last one.
  void _advance() {
    if (_index >= _segments.length - 1) {
      setState(() => _phase = _Phase.done);
      _finishJourney();
    } else {
      setState(() => _index++);
    }
  }

  // Fires once when the whole journey is done: marks the game complete and
  // writes the same session bookkeeping the other custom games use. The
  // per-step health logs (exercise, meal, water, meds, BP) already fired inside
  // each step as it played.
  Future<void> _finishJourney() async {
    if (_finished) return;
    _finished = true;

    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    final game = widget.game;

    if (uid.isNotEmpty) {
      // ignore: unawaited_futures
      CustomGamesRepository.instance.markCompleted(uid: uid, gameId: game.id);

      // ignore: unawaited_futures
      SurveyHooks.submitResponse(
        uid: uid,
        surveyId: _surveyId,
        answers: {
          'steps': _segments
              .map((s) => {'kind': s.kind, 'label': s.label, 'goal': s.goal})
              .toList(),
          'stepCount': _segments.length,
        },
      );

      // ignore: unawaited_futures
      HealthHooks.logSnapshot(
        uid: uid,
        gameId: _surveyId,
        sessionId: _sessionId,
      );

      // ignore: unawaited_futures
      GetIt.instance<OfflineQueue>().enqueue(PendingOp.set(
        '${FirestorePaths.userData}/$uid/gameSessions/$_sessionId',
        {
          'sessionId': _sessionId,
          'userId': uid,
          'gameId': _surveyId,
          'gameTitle': game.title,
          'category': game.category.name,
          'hostType': 'CustomJourneyPlayer',
          'startedAt': OfflineFieldValue.timestampFrom(_startedAt),
          'endedAt': OfflineFieldValue.nowTimestamp(),
          'durationMs': DateTime.now().difference(_startedAt).inMilliseconds,
          'exitReason': 'completed',
          'stepCount': _segments.length,
          'isCustomGame': true,
        },
        merge: true,
      ));
    }

    if (mounted) {
      PointsHooks.applyIncrements(context, {'totalSessions': 1});
    }

    // ignore: unawaited_futures
    TelemetryHooks.logEvent(
      'custom_game_session_completed',
      parameters: {
        'gameId': game.id,
        'sessionId': _sessionId,
        'gameType': 'journey',
        'stepCount': _segments.length,
        'durationMs': DateTime.now().difference(_startedAt).inMilliseconds,
      },
      userId: uid.isEmpty ? null : uid,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF1a1b2e),
      body: Container(
        decoration: const BoxDecoration(gradient: _bgGradient),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _Header(
              title: widget.game.title,
              stepText: _phase == _Phase.playing
                  ? 'Step ${_index + 1} of ${_segments.length}'
                  : null,
              onClose: () => Navigator.of(context).maybePop(),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(24, 24, 24, 24),
                child: _body(),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _body() {
    switch (_phase) {
      case _Phase.intro:
        return _IntroView(
          game: widget.game,
          segments: _segments,
          onBegin: () => setState(() => _phase = _Phase.playing),
        );
      case _Phase.done:
        return _DoneView(
          stepCount: _segments.length,
          onDone: () => Navigator.of(context).maybePop(),
        );
      case _Phase.playing:
        final segment = _segments[_index];
        // A key per step so each mini-game starts fresh.
        final key = ValueKey('${segment.kind}_$_index');
        switch (segment.kind) {
          case 'walk':
            return _WalkSegment(
                key: key, segment: segment, onDone: _advance);
          case 'plate':
            return _PlateSegment(
                key: key, segment: segment, onDone: _advance);
          case 'water':
            return _WaterSegment(
                key: key, segment: segment, onDone: _advance);
          case 'pill':
            return _PillSegment(
                key: key, segment: segment, onDone: _advance);
          case 'bp':
            return _BpSegment(key: key, segment: segment, onDone: _advance);
          case 'breathe':
            return _BreatheSegment(
                key: key, segment: segment, onDone: _advance);
          default:
            return _MoodSegment(
                key: key, segment: segment, onDone: _advance);
        }
    }
  }
}

// ── Top bar ─────────────────────────────────────────────────────────────────

class _Header extends StatelessWidget {
  final String title;
  final String? stepText;
  final VoidCallback onClose;
  const _Header({required this.title, required this.stepText, required this.onClose});

  @override
  Widget build(BuildContext context) {
    final topInset = MediaQuery.of(context).padding.top;
    return Container(
      padding: EdgeInsets.fromLTRB(20, topInset + 14, 12, 14),
      color: Colors.black.withValues(alpha: 0.18),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (stepText != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(
                      stepText!,
                      style: TextStyle(
                        color: Colors.white.withValues(alpha: 0.7),
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 0.6,
                      ),
                    ),
                  ),
              ],
            ),
          ),
          IconButton(
            onPressed: onClose,
            icon: const Icon(Icons.close, color: Colors.white),
            tooltip: 'Close',
          ),
        ],
      ),
    );
  }
}

// ── Intro + done ────────────────────────────────────────────────────────────

class _IntroView extends StatelessWidget {
  final CustomGame game;
  final List<GameSegment> segments;
  final VoidCallback onBegin;
  const _IntroView(
      {required this.game, required this.segments, required this.onBegin});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: 8),
        Text(
          game.title,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 26,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          segments.length == 1
              ? 'One little game to play.'
              : '${segments.length} little games to play, one after another.',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.8),
            fontSize: 14,
          ),
        ),
        const SizedBox(height: 20),
        Expanded(
          child: ListView.separated(
            itemCount: segments.length,
            separatorBuilder: (context, index) => const SizedBox(height: 10),
            itemBuilder: (context, i) {
              final s = segments[i];
              return Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: Colors.white.withValues(alpha: 0.15)),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 30,
                      height: 30,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.15),
                        shape: BoxShape.circle,
                      ),
                      child: Text(
                        '${i + 1}',
                        style: const TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.w900,
                          fontSize: 13,
                        ),
                      ),
                    ),
                    const SizedBox(width: 14),
                    Text(s.emoji.isEmpty ? '🎯' : s.emoji,
                        style: const TextStyle(fontSize: 24)),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        s.label,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        ),
        const SizedBox(height: 12),
        _PrimaryButton(label: 'START', onPressed: onBegin),
        const SizedBox(height: 8),
      ],
    );
  }
}

class _DoneView extends StatelessWidget {
  final int stepCount;
  final VoidCallback onDone;
  const _DoneView({required this.stepCount, required this.onDone});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const Spacer(),
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.10),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: Colors.white.withValues(alpha: 0.15)),
          ),
          child: Column(
            children: [
              const Icon(Icons.emoji_events, color: _accent, size: 56),
              const SizedBox(height: 8),
              const Text(
                'All done!',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 24,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 12),
              Text(
                stepCount == 1
                    ? 'You finished your game. Great job.'
                    : 'You finished all $stepCount games. Great job.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: Colors.white.withValues(alpha: 0.85),
                  fontSize: 14,
                  height: 1.45,
                ),
              ),
            ],
          ),
        ),
        const Spacer(),
        _PrimaryButton(label: 'DONE', onPressed: onDone),
        const SizedBox(height: 8),
      ],
    );
  }
}

// ── Walk step (GPS + progress bar, logs movement + exercise) ─────────────────

class _WalkSegment extends StatefulWidget {
  final GameSegment segment;
  final VoidCallback onDone;
  const _WalkSegment(
      {super.key, required this.segment, required this.onDone});

  @override
  State<_WalkSegment> createState() => _WalkSegmentState();
}

class _WalkSegmentState extends State<_WalkSegment> {
  StreamSubscription<Position>? _sub;
  Position? _last;
  double _meters = 0;
  final List<GeoPoint> _path = [];
  int _pings = 0;
  bool _walking = false;
  bool _done = false;
  String? _error;
  late final String _sessionId;

  int get _goal => widget.segment.goal > 0 ? widget.segment.goal : 200;
  String get _gameId => 'custom_walk';

  @override
  void initState() {
    super.initState();
    _sessionId = MovementHooks.generateSessionId(_gameId);
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }

  Future<void> _start() async {
    final messenger = ScaffoldMessenger.of(context);
    if (!await Geolocator.isLocationServiceEnabled()) {
      setState(() => _error = 'Turn on Location Services to start walking.');
      return;
    }
    var perm = await Geolocator.checkPermission();
    if (perm == LocationPermission.denied) {
      perm = await Geolocator.requestPermission();
    }
    if (perm == LocationPermission.denied ||
        perm == LocationPermission.deniedForever) {
      setState(() => _error = 'Location permission is needed to walk.');
      return;
    }
    setState(() {
      _walking = true;
      _error = null;
      _meters = 0;
      _path.clear();
      _pings = 0;
    });
    _sub = LocationDispatcher.stream.listen(_onPosition, onError: (e) {
      messenger.showSnackBar(SnackBar(content: Text('GPS error: $e')));
    });
  }

  void _onPosition(Position p) {
    if (_done) return;
    final last = _last;
    if (last != null) {
      final delta = Geolocator.distanceBetween(
          last.latitude, last.longitude, p.latitude, p.longitude);
      if (delta < 40 && p.accuracy < 25) _meters += delta;
    }
    _last = p;
    _path.add(GeoPoint(p.latitude, p.longitude));
    _pings++;

    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    if (_pings % 5 == 0 && uid.isNotEmpty) {
      // ignore: unawaited_futures
      MovementHooks.pushPing(
        uid: uid,
        sessionId: _sessionId,
        gameId: _gameId,
        position: p,
        distanceWalked: _meters,
        targetDistance: _goal.toDouble(),
        pathCoordinates: List<GeoPoint>.from(_path),
      );
    }
    setState(() {});
    if (_meters >= _goal) _finish();
  }

  Future<void> _finish() async {
    if (_done) return;
    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    setState(() => _done = true);
    await _sub?.cancel();
    _sub = null;

    if (uid.isNotEmpty && _meters > 0) {
      // ignore: unawaited_futures
      MovementHooks.endSession(
        uid: uid,
        sessionId: _sessionId,
        gameId: _gameId,
        distanceWalked: _meters,
        targetDistance: _goal.toDouble(),
        buddyName: '',
        pathCoordinates: _path,
        completionEventName: 'custom_journey_walk_completed',
      );
      // Also log it as exercise minutes (rough estimate at a gentle pace).
      final minutes = math.max(1, (_meters / 70).round());
      // ignore: unawaited_futures
      DailyLogHooks.logExercise(
        uid: uid,
        activity: widget.segment.label.isEmpty ? 'Walk' : widget.segment.label,
        minutes: minutes,
      );
    }
    if (mounted && _meters > 0) {
      PointsHooks.applyIncrements(context, {'totalDistance': _meters.toInt()});
    }
    // Brief pause so the patient sees the bar fill, then move on.
    await Future.delayed(const Duration(milliseconds: 600));
    if (mounted) widget.onDone();
  }

  @override
  Widget build(BuildContext context) {
    final progress = (_meters / _goal).clamp(0.0, 1.0);
    if (!_walking) {
      return Column(
        children: [
          const Spacer(),
          const Text('🚶', style: TextStyle(fontSize: 64)),
          const SizedBox(height: 12),
          Text(
            widget.segment.label.isEmpty ? 'Time to walk' : widget.segment.label,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Walk $_goal meters. Your phone counts the steps for you.',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Colors.white.withValues(alpha: 0.85),
              fontSize: 15,
              height: 1.4,
            ),
          ),
          if (_error != null) ...[
            const SizedBox(height: 16),
            _ErrorBox(_error!),
          ],
          const Spacer(),
          _PrimaryButton(label: 'START WALK', onPressed: _start),
          const SizedBox(height: 8),
        ],
      );
    }
    return Column(
      children: [
        const Spacer(),
        Text(
          '${_meters.toInt()} m',
          style: const TextStyle(
            color: Colors.white,
            fontSize: 56,
            fontWeight: FontWeight.bold,
          ),
        ),
        Text(
          'of $_goal m',
          style: TextStyle(
              color: Colors.white.withValues(alpha: 0.7), fontSize: 16),
        ),
        const SizedBox(height: 24),
        ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: LinearProgressIndicator(
            value: progress,
            minHeight: 12,
            backgroundColor: Colors.white.withValues(alpha: 0.15),
            valueColor: const AlwaysStoppedAnimation<Color>(_accent),
          ),
        ),
        const SizedBox(height: 12),
        Text(
          _meters >= _goal
              ? 'Target reached'
              : '${(_goal - _meters).toInt()} m to go',
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.85),
            fontSize: 14,
            fontWeight: FontWeight.w600,
          ),
        ),
        const Spacer(),
        _SecondaryButton(
            label: "I'M DONE", onPressed: _done ? null : _finish),
        const SizedBox(height: 8),
      ],
    );
  }
}

// ── Plate step (build a healthy plate, logs a meal) ──────────────────────────

class _PlateFood {
  final String emoji;
  final String name;
  const _PlateFood(this.emoji, this.name);
}

const _plateFoods = [
  _PlateFood('🥦', 'Broccoli'),
  _PlateFood('🥕', 'Carrots'),
  _PlateFood('🍎', 'Apple'),
  _PlateFood('🐟', 'Fish'),
  _PlateFood('🥗', 'Salad'),
  _PlateFood('🫐', 'Berries'),
];

class _PlateSegment extends StatefulWidget {
  final GameSegment segment;
  final VoidCallback onDone;
  const _PlateSegment(
      {super.key, required this.segment, required this.onDone});

  @override
  State<_PlateSegment> createState() => _PlateSegmentState();
}

class _PlateSegmentState extends State<_PlateSegment> {
  final Set<int> _chosen = {};
  bool _saving = false;

  static const _need = 3; // healthy items to fill the plate

  Future<void> _serve() async {
    if (_chosen.length < _need || _saving) return;
    setState(() => _saving = true);
    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    final foods = _chosen.map((i) => _plateFoods[i].name).join(', ');
    if (uid.isNotEmpty) {
      // A full plate of good foods reads as a happy meal (rating 4 of 4).
      // ignore: unawaited_futures
      DailyLogHooks.logMeal(
        uid: uid,
        mealNotes: foods,
        mealRating: 4,
        hasMealPhoto: false,
      );
      if (mounted) {
        PointsHooks.applyIncrements(context, {'mealsLogged': 1});
      }
    }
    await Future.delayed(const Duration(milliseconds: 400));
    if (mounted) widget.onDone();
  }

  @override
  Widget build(BuildContext context) {
    final ready = _chosen.length >= _need;
    return Column(
      children: [
        const SizedBox(height: 4),
        const Text(
          'Build a healthy plate',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white,
            fontSize: 24,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'Tap $_need good foods to fill your plate.',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.85),
            fontSize: 15,
          ),
        ),
        const SizedBox(height: 20),
        Expanded(
          child: GridView.count(
            crossAxisCount: 3,
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            children: List.generate(_plateFoods.length, (i) {
              final selected = _chosen.contains(i);
              return GestureDetector(
                onTap: _saving
                    ? null
                    : () => setState(() {
                          if (selected) {
                            _chosen.remove(i);
                          } else {
                            _chosen.add(i);
                          }
                        }),
                child: AnimatedContainer(
                  duration: const Duration(milliseconds: 150),
                  decoration: BoxDecoration(
                    color: selected
                        ? _accent.withValues(alpha: 0.25)
                        : Colors.white.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(
                      color: selected
                          ? _accent
                          : Colors.white.withValues(alpha: 0.15),
                      width: 2,
                    ),
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(_plateFoods[i].emoji,
                          style: const TextStyle(fontSize: 34)),
                      const SizedBox(height: 6),
                      Text(
                        _plateFoods[i].name,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            }),
          ),
        ),
        const SizedBox(height: 8),
        Text(
          '${_chosen.length} of $_need chosen',
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.8),
            fontSize: 13,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 10),
        _PrimaryButton(
          label: _saving ? 'SAVING...' : 'SERVE IT UP',
          onPressed: (ready && !_saving) ? _serve : null,
        ),
        const SizedBox(height: 8),
      ],
    );
  }
}

// ── Water step (fill the glass, logs water, optional reminder) ───────────────

class _WaterSegment extends StatefulWidget {
  final GameSegment segment;
  final VoidCallback onDone;
  const _WaterSegment(
      {super.key, required this.segment, required this.onDone});

  @override
  State<_WaterSegment> createState() => _WaterSegmentState();
}

class _WaterSegmentState extends State<_WaterSegment> {
  int _filled = 0;
  bool _remind = true;
  bool _saving = false;

  int get _goal => widget.segment.goal > 0 ? widget.segment.goal : 8;

  Future<void> _done() async {
    if (_filled <= 0 || _saving) return;
    setState(() => _saving = true);
    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    if (uid.isNotEmpty) {
      // ignore: unawaited_futures
      DailyLogHooks.logWater(uid: uid, glasses: _filled);
      if (mounted) {
        PointsHooks.applyIncrements(context, {'waterGlassesLogged': _filled});
      }
    }
    if (_remind) {
      WaterReminderService.instance.scheduleReminder();
    }
    await Future.delayed(const Duration(milliseconds: 400));
    if (mounted) widget.onDone();
  }

  @override
  Widget build(BuildContext context) {
    final level = (_filled / _goal).clamp(0.0, 1.0);
    return Column(
      children: [
        const SizedBox(height: 4),
        const Text(
          'Drink some water',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white,
            fontSize: 24,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'Tap the glass each time you take a sip. Aim for $_goal.',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.85),
            fontSize: 15,
          ),
        ),
        const Spacer(),
        GestureDetector(
          onTap: _saving
              ? null
              : () => setState(
                  () => _filled = math.min(_goal, _filled + 1)),
          child: _GlassView(level: level),
        ),
        const SizedBox(height: 14),
        Text(
          '$_filled of $_goal glasses',
          style: const TextStyle(
            color: Colors.white,
            fontSize: 18,
            fontWeight: FontWeight.bold,
          ),
        ),
        const Spacer(),
        SwitchListTile(
          value: _remind,
          onChanged: _saving ? null : (v) => setState(() => _remind = v),
          activeThumbColor: _accent,
          contentPadding: EdgeInsets.zero,
          title: const Text(
            'Remind me to drink again in a while',
            style: TextStyle(color: Colors.white, fontSize: 14),
          ),
        ),
        const SizedBox(height: 6),
        _PrimaryButton(
          label: _saving ? 'SAVING...' : 'ALL DONE',
          onPressed: (_filled > 0 && !_saving) ? _done : null,
        ),
        const SizedBox(height: 8),
      ],
    );
  }
}

// A simple glass that fills up with water as [level] goes 0 -> 1.
class _GlassView extends StatelessWidget {
  final double level;
  const _GlassView({required this.level});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 120,
      height: 160,
      decoration: BoxDecoration(
        border: Border.all(color: Colors.white.withValues(alpha: 0.7), width: 3),
        borderRadius: const BorderRadius.vertical(
          top: Radius.circular(10),
          bottom: Radius.circular(20),
        ),
      ),
      child: ClipRRect(
        borderRadius: const BorderRadius.vertical(
          top: Radius.circular(8),
          bottom: Radius.circular(18),
        ),
        child: Align(
          alignment: Alignment.bottomCenter,
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 250),
            height: 154 * level,
            width: double.infinity,
            color: const Color(0xFF4fc3f7).withValues(alpha: 0.85),
          ),
        ),
      ),
    );
  }
}

// ── Medicine step (check-in, logs medication) ────────────────────────────────

class _PillSegment extends StatefulWidget {
  final GameSegment segment;
  final VoidCallback onDone;
  const _PillSegment(
      {super.key, required this.segment, required this.onDone});

  @override
  State<_PillSegment> createState() => _PillSegmentState();
}

class _PillSegmentState extends State<_PillSegment> {
  int _streak = 0;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _loadStreak();
  }

  Future<void> _loadStreak() async {
    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    if (uid.isEmpty) return;
    try {
      final doc = await FirebaseFirestore.instance
          .collection(FirestorePaths.userData)
          .doc(uid)
          .get();
      if (doc.exists && mounted) {
        setState(() => _streak = (doc.data()?['medicationStreak'] as num?)?.toInt() ?? 0);
      }
    } catch (_) {}
  }

  Future<void> _answer(bool taken) async {
    if (_busy) return;
    setState(() => _busy = true);
    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    if (uid.isNotEmpty) {
      // ignore: unawaited_futures
      DailyLogHooks.logMedication(
          uid: uid, taken: taken, currentStreak: _streak);
      if (mounted) {
        final newStreak = taken ? _streak + 1 : 0;
        PointsHooks.applySets(context, {'medicationStreak': newStreak});
      }
    }
    await Future.delayed(const Duration(milliseconds: 350));
    if (mounted) widget.onDone();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const Spacer(),
        const Text('💊', style: TextStyle(fontSize: 64)),
        const SizedBox(height: 12),
        const Text(
          'Time for your medicine',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white,
            fontSize: 24,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 10),
        Text(
          'Have you taken it today?',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.85),
            fontSize: 16,
          ),
        ),
        const Spacer(),
        _PrimaryButton(
          label: 'YES, I TOOK IT',
          onPressed: _busy ? null : () => _answer(true),
        ),
        const SizedBox(height: 12),
        _SecondaryButton(
          label: 'NOT YET',
          onPressed: _busy ? null : () => _answer(false),
        ),
        const SizedBox(height: 8),
      ],
    );
  }
}

// ── Blood pressure step (entry, logs a reading) ──────────────────────────────

class _BpSegment extends StatefulWidget {
  final GameSegment segment;
  final VoidCallback onDone;
  const _BpSegment({super.key, required this.segment, required this.onDone});

  @override
  State<_BpSegment> createState() => _BpSegmentState();
}

class _BpSegmentState extends State<_BpSegment> {
  final _sysCtrl = TextEditingController();
  final _diaCtrl = TextEditingController();
  int _mood = 2;
  bool _saving = false;
  String? _error;

  @override
  void dispose() {
    _sysCtrl.dispose();
    _diaCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final sys = int.tryParse(_sysCtrl.text.trim());
    final dia = int.tryParse(_diaCtrl.text.trim());
    if (sys == null || dia == null || sys < 60 || sys > 260 || dia < 40 || dia > 200) {
      setState(() => _error = 'Enter numbers like 120 over 80.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    final uid = Provider.of<UserDataProvider>(context, listen: false).uid;
    if (uid.isNotEmpty) {
      // ignore: unawaited_futures
      DailyLogHooks.logBP(
          uid: uid, systolic: sys, diastolic: dia, mood: _mood);
      if (mounted) {
        PointsHooks.applySets(context, {'lastSystolic': sys, 'lastDiastolic': dia});
      }
    }
    await Future.delayed(const Duration(milliseconds: 400));
    if (mounted) widget.onDone();
  }

  @override
  Widget build(BuildContext context) {
    final moods = ['😞', '😕', '😐', '🙂', '😄'];
    return SingleChildScrollView(
      child: Column(
        children: [
          const SizedBox(height: 4),
          const Text('🩺', style: TextStyle(fontSize: 52)),
          const SizedBox(height: 8),
          const Text(
            'Blood pressure',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(child: _BpField(controller: _sysCtrl, label: 'Top (systolic)')),
              const SizedBox(width: 14),
              Expanded(child: _BpField(controller: _diaCtrl, label: 'Bottom (diastolic)')),
            ],
          ),
          const SizedBox(height: 20),
          const Text(
            'How do you feel?',
            style: TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 10),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: List.generate(moods.length, (i) {
              final sel = _mood == i;
              return GestureDetector(
                onTap: _saving ? null : () => setState(() => _mood = i),
                child: Opacity(
                  opacity: sel ? 1 : 0.45,
                  child: Text(moods[i],
                      style: TextStyle(fontSize: sel ? 36 : 28)),
                ),
              );
            }),
          ),
          if (_error != null) ...[
            const SizedBox(height: 16),
            _ErrorBox(_error!),
          ],
          const SizedBox(height: 24),
          _PrimaryButton(
            label: _saving ? 'SAVING...' : 'SAVE READING',
            onPressed: _saving ? null : _save,
          ),
          const SizedBox(height: 8),
        ],
      ),
    );
  }
}

class _BpField extends StatelessWidget {
  final TextEditingController controller;
  final String label;
  const _BpField({required this.controller, required this.label});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
              color: Colors.white.withValues(alpha: 0.85),
              fontSize: 12,
              fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 6),
        TextField(
          controller: controller,
          keyboardType: TextInputType.number,
          textAlign: TextAlign.center,
          style: const TextStyle(
              color: Colors.white, fontSize: 28, fontWeight: FontWeight.bold),
          decoration: InputDecoration(
            filled: true,
            fillColor: Colors.white.withValues(alpha: 0.10),
            hintText: '--',
            hintStyle: TextStyle(color: Colors.white.withValues(alpha: 0.4)),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(14),
              borderSide: BorderSide(color: Colors.white.withValues(alpha: 0.3)),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(14),
              borderSide: const BorderSide(color: _accent, width: 2),
            ),
          ),
        ),
      ],
    );
  }
}

// ── Breathing step (paced calm break, no log) ────────────────────────────────

class _BreatheSegment extends StatefulWidget {
  final GameSegment segment;
  final VoidCallback onDone;
  const _BreatheSegment(
      {super.key, required this.segment, required this.onDone});

  @override
  State<_BreatheSegment> createState() => _BreatheSegmentState();
}

class _BreatheSegmentState extends State<_BreatheSegment>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;
  int _rounds = 0;
  bool _finished = false;

  int get _goal => widget.segment.goal > 0 ? widget.segment.goal : 3;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 4),
    )..addStatusListener(_onStatus);
    _ctrl.repeat(reverse: true);
  }

  void _onStatus(AnimationStatus status) {
    // One full round = grow then shrink (a complete reverse cycle).
    if (status == AnimationStatus.dismissed) {
      _rounds++;
      if (_rounds >= _goal && !_finished) {
        setState(() => _finished = true);
        _ctrl.stop();
      } else {
        setState(() {});
      }
    }
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const SizedBox(height: 4),
        const Text(
          'Take a calm breath',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white,
            fontSize: 24,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          _finished
              ? 'Nicely done.'
              : 'Breathe in as the circle grows, out as it shrinks.',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.85),
            fontSize: 15,
          ),
        ),
        const Spacer(),
        AnimatedBuilder(
          animation: _ctrl,
          builder: (context, _) {
            final t = Curves.easeInOut.transform(_ctrl.value);
            final size = 120 + 120 * t;
            return Container(
              width: size,
              height: size,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: _accent.withValues(alpha: 0.15 + 0.25 * t),
                border: Border.all(color: _accent, width: 3),
              ),
              alignment: Alignment.center,
              child: Text(
                _ctrl.status == AnimationStatus.reverse ? 'Out' : 'In',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                ),
              ),
            );
          },
        ),
        const SizedBox(height: 16),
        Text(
          'Round ${math.min(_rounds + (_finished ? 0 : 1), _goal)} of $_goal',
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.8),
            fontSize: 14,
            fontWeight: FontWeight.w600,
          ),
        ),
        const Spacer(),
        _PrimaryButton(
          label: _finished ? 'CONTINUE' : 'SKIP',
          onPressed: widget.onDone,
        ),
        const SizedBox(height: 8),
      ],
    );
  }
}

// ── Mood / simple wellbeing check-in (no log) ────────────────────────────────

class _MoodSegment extends StatelessWidget {
  final GameSegment segment;
  final VoidCallback onDone;
  const _MoodSegment(
      {super.key, required this.segment, required this.onDone});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const Spacer(),
        Text(segment.emoji.isEmpty ? '😊' : segment.emoji,
            style: const TextStyle(fontSize: 64)),
        const SizedBox(height: 16),
        Text(
          segment.label.isEmpty ? 'How are you feeling?' : segment.label,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 24,
            fontWeight: FontWeight.bold,
          ),
        ),
        const Spacer(),
        _PrimaryButton(label: 'GOOD 👍', onPressed: onDone),
        const SizedBox(height: 12),
        _SecondaryButton(label: 'NOT GREAT', onPressed: onDone),
        const SizedBox(height: 8),
      ],
    );
  }
}

// ── Shared buttons + bits ────────────────────────────────────────────────────

class _ErrorBox extends StatelessWidget {
  final String text;
  const _ErrorBox(this.text);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.redAccent.withValues(alpha: 0.18),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Text(text, style: const TextStyle(color: Colors.white, fontSize: 13)),
    );
  }
}

class _PrimaryButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  const _PrimaryButton({required this.label, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 54,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: Colors.white,
          foregroundColor: const Color(0xFF1a3a5c),
          disabledBackgroundColor: Colors.white.withValues(alpha: 0.35),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          elevation: 0,
        ),
        child: Text(
          label,
          style: const TextStyle(
            fontWeight: FontWeight.w900,
            letterSpacing: 1.2,
            fontSize: 15,
          ),
        ),
      ),
    );
  }
}

class _SecondaryButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  const _SecondaryButton({required this.label, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 54,
      child: OutlinedButton(
        onPressed: onPressed,
        style: OutlinedButton.styleFrom(
          backgroundColor: Colors.white.withValues(alpha: 0.08),
          foregroundColor: Colors.white,
          side: BorderSide(color: Colors.white.withValues(alpha: 0.4), width: 1.5),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        ),
        child: Text(
          label,
          style: const TextStyle(
            fontWeight: FontWeight.w900,
            letterSpacing: 1.2,
            fontSize: 15,
          ),
        ),
      ),
    );
  }
}
