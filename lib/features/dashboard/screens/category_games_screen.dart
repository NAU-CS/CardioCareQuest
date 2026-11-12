// One factor's games. You land here after the factor's intro story.
// Most factors have a single game, shown as a tile that opens the preview +
// Play popup. Diet and Education have two games stitched into one game with a
// chapter menu, so those show a numbered chapter list instead.

import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../games/game_launcher.dart';
import '../../games/game_stories.dart';
import '../widgets/game_detail_dialog.dart';

// The screen: takes one factor and shows its game(s).
class CategoryGamesScreen extends StatelessWidget {
  final GameCategory category;

  const CategoryGamesScreen({super.key, required this.category});

  @override
  Widget build(BuildContext context) {
    final chapterGame = GameCatalog.chapterGameFor(category);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          chapterGame?.title ?? category.label,
          style: const TextStyle(color: AppColors.title),
        ),
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppColors.title),
      ),
      body: chapterGame != null
          ? _ChapterList(chapterGame: chapterGame)
          : _GameGrid(category: category),
    );
  }
}

// A numbered list of chapters for a combined game (Diet Quest, Heart Smarts).
// Each row plays one of the underlying games in order.
class _ChapterList extends StatelessWidget {
  final ChapterGame chapterGame;

  const _ChapterList({required this.chapterGame});

  @override
  Widget build(BuildContext context) {
    final chapters = <GameStory>[
      for (final id in chapterGame.chapterIds)
        if (GameCatalog.getGame(id) != null) GameCatalog.getGame(id)!,
    ];

    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
      children: [
        Text(
          chapterGame.shortDescription,
          style: const TextStyle(
            color: AppColors.subtitle,
            fontSize: 15,
            height: 1.4,
          ),
        ),
        const SizedBox(height: 16),
        for (var i = 0; i < chapters.length; i++) ...[
          _ChapterCard(
            number: i + 1,
            game: chapters[i],
            onTap: () => launchGameStory(context, chapters[i]),
          ),
          const SizedBox(height: 14),
        ],
      ],
    );
  }
}

// One chapter row: "Chapter N" pill, the game title and blurb, and a chevron.
class _ChapterCard extends StatelessWidget {
  final int number;
  final GameStory game;
  final VoidCallback onTap;

  const _ChapterCard({
    required this.number,
    required this.game,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: 'Chapter $number, ${game.title}',
      child: Material(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(20),
          child: Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.cardBorder),
              boxShadow: [
                BoxShadow(
                  color: AppColors.accent.withValues(alpha: 0.08),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Row(
              children: [
                // Chapter number badge.
                Container(
                  width: 52,
                  height: 52,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text(
                        'CH',
                        style: TextStyle(
                          color: AppColors.primary,
                          fontSize: 10,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1,
                        ),
                      ),
                      Text(
                        '$number',
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                          height: 1,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        game.title,
                        style: const TextStyle(
                          color: AppColors.title,
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        game.shortDescription,
                        style: const TextStyle(
                          color: AppColors.subtitle,
                          fontSize: 13,
                          height: 1.4,
                        ),
                      ),
                    ],
                  ),
                ),
                const Padding(
                  padding: EdgeInsets.only(left: 8),
                  child: Icon(Icons.chevron_right, color: AppColors.subtitle),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// A 2-across grid of a factor's games, for factors with a single game (or more
// than one that aren't chaptered). Tapping a tile opens the preview + Play popup.
class _GameGrid extends StatelessWidget {
  final GameCategory category;

  const _GameGrid({required this.category});

  @override
  Widget build(BuildContext context) {
    final games =
        GameCatalog.getCatalogGamesByCategory()[category] ?? const [];

    return GridView.builder(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 16,
        crossAxisSpacing: 16,
        childAspectRatio: 1,
      ),
      itemCount: games.length,
      itemBuilder: (context, i) {
        final g = games[i];
        return _GameSquareCard(
          game: g,
          onTap: () => showGameDetailDialog(context, g),
        );
      },
    );
  }
}

// One square tile for a single game - icon + title, tap to open it.
class _GameSquareCard extends StatelessWidget {
  final GameStory game;
  final VoidCallback onTap;

  const _GameSquareCard({required this.game, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: game.title,
      child: Material(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(20),
          child: Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.cardBorder),
              boxShadow: [
                BoxShadow(
                  color: AppColors.accent.withValues(alpha: 0.08),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(game.iconData, color: AppColors.primary, size: 44),
                  const SizedBox(height: 12),
                  Text(
                    game.title,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      color: AppColors.title,
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
