// Data for one user-created game (walk goal or quiz).

import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:flutter/material.dart';

import '../game_stories.dart';

// journey = a sequence of native mini-games (walk, plate, water, meds, ...),
// each a real playable step wired to its own health hook. This is the type the
// "build your own game" screen creates now.
// walk = GPS-tracked walk (like Dog Quest); quiz = answer some questions;
// story = a multi-step branching Twine story generated from the scenes below.
enum CustomGameType { journey, story, walk, quiz }

// Helpers: display name, tagline, and icon for each game type.
extension CustomGameTypeX on CustomGameType {
  String get label {
    switch (this) {
      case CustomGameType.journey:
        return 'Health journey';
      case CustomGameType.story:
        return 'Story game';
      case CustomGameType.walk:
        return 'Walking quest';
      case CustomGameType.quiz:
        return 'Quick quiz';
    }
  }

  String get tagline {
    switch (this) {
      case CustomGameType.journey:
        return 'A run of little games, one for each healthy thing you pick.';
      case CustomGameType.story:
        return 'A few steps that can branch based on your answers, like the catalog games.';
      case CustomGameType.walk:
        return 'Pick a distance and take a walk, tracked by GPS.';
      case CustomGameType.quiz:
        return 'Ask yourself one question, pick from 2-4 answers.';
    }
  }

  IconData get icon {
    switch (this) {
      case CustomGameType.journey:
        return Icons.map_outlined;
      case CustomGameType.story:
        return Icons.auto_stories_outlined;
      case CustomGameType.walk:
        return Icons.directions_walk;
      case CustomGameType.quiz:
        return Icons.quiz_outlined;
    }
  }
}

// One playable step in a health journey. `kind` picks which native mini-game
// runs and which health hook it fires:
//   walk    - GPS walk with a progress bar, logs exercise + movement
//   plate   - build-a-healthy-plate, logs a meal
//   water   - fill-the-glass, logs water and can set a drink reminder
//   pill    - take-your-medicine check-in, logs medication
//   bp      - blood-pressure entry, logs a reading
//   breathe - paced breathing, a calm break (no log)
//   mood    - a simple yes/no wellbeing check-in (no log)
// `goal` means meters for walk, glasses for water, and breathing rounds for
// breathe; it is ignored for the others.
class GameSegment {
  final String kind;
  final String label;
  final String emoji;
  final int goal;

  const GameSegment({
    required this.kind,
    required this.label,
    this.emoji = '',
    this.goal = 0,
  });

  Map<String, dynamic> toMap() => {
        'kind': kind,
        'label': label,
        'emoji': emoji,
        'goal': goal,
      };

  static GameSegment fromMap(Map<dynamic, dynamic> m) => GameSegment(
        kind: (m['kind'] as String?) ?? 'mood',
        label: (m['label'] as String?) ?? '',
        emoji: (m['emoji'] as String?) ?? '',
        goal: (m['goal'] as num?)?.toInt() ?? 0,
      );
}

// One answer choice in a story scene. `nextScene` is the index of the scene to
// jump to next, or -1 to finish the story (go to the submit/thank-you step).
class StoryOption {
  final String label;
  final int nextScene;

  const StoryOption({required this.label, this.nextScene = -1});

  Map<String, dynamic> toMap() => {
        'label': label,
        'nextScene': nextScene,
      };

  static StoryOption fromMap(Map<dynamic, dynamic> m) => StoryOption(
        label: (m['label'] as String?) ?? '',
        nextScene: (m['nextScene'] as num?)?.toInt() ?? -1,
      );
}

// One step in a story game. `kind` selects how the step plays: 'choice'
// (default) shows the answer buttons; 'bp' shows a blood-pressure entry.
// `logKind` picks which health hook a "Yes" answer fires ('none', 'exercise',
// 'meal', 'medication'); `logLabel` is the human-readable activity saved with it.
class StoryScene {
  final String prompt;
  final List<StoryOption> options;
  final String kind;
  final String logKind;
  final String logLabel;

  const StoryScene({
    required this.prompt,
    this.options = const <StoryOption>[],
    this.kind = 'choice',
    this.logKind = 'none',
    this.logLabel = '',
  });

  Map<String, dynamic> toMap() => {
        'prompt': prompt,
        'options': options.map((o) => o.toMap()).toList(),
        'kind': kind,
        'logKind': logKind,
        'logLabel': logLabel,
      };

  static StoryScene fromMap(Map<dynamic, dynamic> m) {
    final raw = m['options'];
    return StoryScene(
      prompt: (m['prompt'] as String?) ?? '',
      options: (raw is List)
          ? raw.whereType<Map>().map(StoryOption.fromMap).toList()
          : const <StoryOption>[],
      kind: (m['kind'] as String?) ?? 'choice',
      logKind: (m['logKind'] as String?) ?? 'none',
      logLabel: (m['logLabel'] as String?) ?? '',
    );
  }
}

// One quiz question with its answer choices.
class QuizQuestion {
  final String prompt;
  final List<String> options;

  const QuizQuestion({
    required this.prompt,
    this.options = const <String>[],
  });

  // Convert to a map for cloud storage.
  Map<String, dynamic> toMap() => {
        'prompt': prompt,
        'options': options,
      };

  // Rebuild a question from a saved map. Missing fields get safe defaults.
  static QuizQuestion fromMap(Map<dynamic, dynamic> m) {
    final raw = m['options'];
    return QuizQuestion(
      prompt: (m['prompt'] as String?) ?? '',
      options: (raw is List)
          ? raw.whereType<String>().toList()
          : const <String>[],
    );
  }
}

// A user-created game and its saved play stats.
class CustomGame {
  final String id;
  final String title;
  final String description;
  final GameCategory category;
  final CustomGameType gameType;

  // Quiz questions. Empty for walk games. Old saves used `prompt`/`options`
  // instead; `effectiveQuestions` handles both.
  final List<QuizQuestion> questions;

  // Story scenes. Empty for walk/quiz games. Used to generate the Twine story.
  final List<StoryScene> scenes;

  // Journey steps. One native mini-game each. Empty for other game types.
  final List<GameSegment> segments;

  // Legacy single-question fields kept for backward compatibility.
  final String prompt;
  final List<String> options;

  // Walk goal in meters. 0 for quiz games.
  final int targetDistance;

  final DateTime? createdAt;
  final int completedCount;
  final DateTime? lastCompletedAt;

  const CustomGame({
    required this.id,
    required this.title,
    required this.description,
    required this.category,
    this.gameType = CustomGameType.quiz,
    this.questions = const <QuizQuestion>[],
    this.scenes = const <StoryScene>[],
    this.segments = const <GameSegment>[],
    this.prompt = '',
    this.options = const <String>[],
    this.targetDistance = 0,
    this.createdAt,
    this.completedCount = 0,
    this.lastCompletedAt,
  });

  IconData get iconData => category.icon;

  // Returns the questions to play. Falls back to the old single-question
  // fields for saves from before multi-question support was added.
  List<QuizQuestion> get effectiveQuestions {
    if (questions.isNotEmpty) return questions;
    if (gameType != CustomGameType.quiz) return const <QuizQuestion>[];
    return <QuizQuestion>[
      QuizQuestion(prompt: prompt, options: options),
    ];
  }

  // Convert the game to a map for cloud storage.
  Map<String, dynamic> toMap() => {
        'id': id,
        'title': title,
        'description': description,
        'category': category.name,
        'gameType': gameType.name,
        'questions': questions.map((q) => q.toMap()).toList(),
        'scenes': scenes.map((s) => s.toMap()).toList(),
        'segments': segments.map((s) => s.toMap()).toList(),
        // Also write old-style fields so older app versions can still open it.
        'prompt': questions.isNotEmpty ? questions.first.prompt : prompt,
        'options': questions.isNotEmpty
            ? questions.first.options
            : options,
        'targetDistance': targetDistance,
        'createdAt': createdAt == null
            ? FieldValue.serverTimestamp()
            : Timestamp.fromDate(createdAt!),
        'completedCount': completedCount,
        if (lastCompletedAt != null)
          'lastCompletedAt': Timestamp.fromDate(lastCompletedAt!),
      };

  // Rebuild a game from a cloud record. Missing fields get safe defaults.
  static CustomGame fromDoc(DocumentSnapshot<Map<String, dynamic>> doc) {
    final data = doc.data() ?? const <String, dynamic>{};
    final rawOptions = data['options'];
    final options = (rawOptions is List)
        ? rawOptions.whereType<String>().toList()
        : const <String>[];
    final rawQuestions = data['questions'];
    final questions = (rawQuestions is List)
        ? rawQuestions
            .whereType<Map>()
            .map(QuizQuestion.fromMap)
            .toList()
        : const <QuizQuestion>[];
    final rawScenes = data['scenes'];
    final scenes = (rawScenes is List)
        ? rawScenes
            .whereType<Map>()
            .map(StoryScene.fromMap)
            .toList()
        : const <StoryScene>[];
    final rawSegments = data['segments'];
    final segments = (rawSegments is List)
        ? rawSegments
            .whereType<Map>()
            .map(GameSegment.fromMap)
            .toList()
        : const <GameSegment>[];
    return CustomGame(
      id: doc.id,
      title: (data['title'] as String?) ?? 'Untitled goal',
      description: (data['description'] as String?) ?? '',
      category: _categoryFromName(data['category'] as String?),
      gameType: _gameTypeFromName(data['gameType'] as String?),
      questions: questions,
      scenes: scenes,
      segments: segments,
      prompt: (data['prompt'] as String?) ?? '',
      options: options,
      targetDistance: (data['targetDistance'] as num?)?.toInt() ?? 0,
      createdAt: (data['createdAt'] as Timestamp?)?.toDate(),
      completedCount: (data['completedCount'] as num?)?.toInt() ?? 0,
      lastCompletedAt: (data['lastCompletedAt'] as Timestamp?)?.toDate(),
    );
  }

  // Parse a saved category name. Defaults to "exercise" if unknown.
  static GameCategory _categoryFromName(String? name) {
    if (name == null) return GameCategory.exercise;
    for (final c in GameCategory.values) {
      if (c.name == name) return c;
    }
    return GameCategory.exercise;
  }

  // Parse a saved game type name. Defaults to quiz if unknown.
  static CustomGameType _gameTypeFromName(String? name) {
    if (name == null) return CustomGameType.quiz;
    for (final t in CustomGameType.values) {
      if (t.name == name) return t;
    }
    return CustomGameType.quiz;
  }
}
