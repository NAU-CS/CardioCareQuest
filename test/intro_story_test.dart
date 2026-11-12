import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:cardio_care_quest/features/games/intro_story.dart';

void main() {
  final files = Directory('assets/game-src/intro')
      .listSync()
      .whereType<File>()
      .where((f) => f.path.endsWith('.tw'))
      .toList()
    ..sort((a, b) => a.path.compareTo(b.path));

  test('every factor intro parses into readable pages', () {
    expect(files.length, 5, reason: 'expected one intro per factor');

    for (final f in files) {
      final story = parseIntroStory(f.readAsStringSync(), fallbackTitle: 'X');
      final name = f.uri.pathSegments.last;

      expect(story.title, isNot('X'), reason: '$name: title not read');
      expect(story.pages.length, 3, reason: '$name: expected 3 pages');

      for (final p in story.pages) {
        expect(p.paragraphs, isNotEmpty, reason: '$name: page with no text');
        expect(p.buttonLabel.trim(), isNotEmpty, reason: '$name: empty button');
        for (final para in p.paragraphs) {
          expect(para.contains('<p'), isFalse, reason: '$name: <p> leaked');
          expect(para.contains('<div'), isFalse, reason: '$name: <div> leaked');
          expect(para.contains('<<'), isFalse, reason: '$name: macro leaked');
          expect(para.contains('\n'), isFalse, reason: '$name: raw newline');
        }
      }

      // Exactly one page ends the story, and it is the final one.
      final lastFlags = story.pages.map((p) => p.isLast).toList();
      expect(lastFlags.where((v) => v).length, 1, reason: '$name: end count');
      expect(lastFlags.last, isTrue, reason: '$name: end is not last page');
      expect(story.pages.first.heading, isNotNull, reason: '$name: no heading');
    }
  });
}
