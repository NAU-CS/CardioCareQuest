// Game Catalog - a grid of category tiles, one per health topic.
// Flat tiles instead of expandable sections (easier for older users).
// "Your Goals" tile appears only if the user has made custom games.
// The BP-logging game is reached from the dashboard, not here.

import 'dart:math';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/providers/user_data_manager.dart';
import '../../../core/theme/app_colors.dart';
import '../../games/custom_games/custom_game.dart';
import '../../games/custom_games/custom_games_repository.dart';
import '../../games/game_stories.dart';
import 'category_intro_screen.dart';
import 'custom_games_screen.dart';

// "1 game" / "3 games" for the row subtitle.
String _gameCountLabel(int count) => '$count ${count == 1 ? "game" : "games"}';

class GameCatalogScreen extends StatefulWidget {
  const GameCatalogScreen({super.key});

  @override
  State<GameCatalogScreen> createState() => _GameCatalogScreenState();
}

class _GameCatalogScreenState extends State<GameCatalogScreen> {
  // Category tiles, shuffled fresh each visit but stable during rebuilds.
  late final Map<GameCategory, List<GameStory>> _byCategory;
  late final List<GameCategory> _categories;

  // Remembers last visit's order so each new visit looks visibly different.
  static List<GameCategory>? _lastOrder;
  static final Random _rng = Random();

  @override
  void initState() {
    super.initState();
    _byCategory = GameCatalog.getCatalogGamesByCategory();
    _categories = _freshShuffledOrder(_byCategory.keys.toList());
    _lastOrder = List<GameCategory>.from(_categories);
  }

  // Shuffles categories, retrying until the order differs from last time.
  // Stops after 8 tries so a 1-item list doesn't loop forever.
  List<GameCategory> _freshShuffledOrder(List<GameCategory> cats) {
    if (cats.length < 2) return cats;
    final order = List<GameCategory>.from(cats);
    for (var attempt = 0; attempt < 8; attempt++) {
      order.shuffle(_rng);
      final prev = _lastOrder;
      // Accept the first order that isn't identical to last time's.
      if (prev == null || !_sameOrder(order, prev)) return order;
    }
    return order;
  }

  bool _sameOrder(List<GameCategory> a, List<GameCategory> b) {
    if (a.length != b.length) return false;
    for (var i = 0; i < a.length; i++) {
      if (a[i] != b[i]) return false;
    }
    return true;
  }

  // Builds a tile per category plus "Your Goals" when the user has custom games.
  @override
  Widget build(BuildContext context) {
    final byCategory = _byCategory;
    final categories = _categories;
    final uid = context.select<UserDataProvider, String>((p) => p.uid);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Game Catalog',
          style: TextStyle(color: AppColors.title),
        ),
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppColors.title),
      ),
      // Watches custom games live; "Your Goals" tile appears once they've made one.
      body: StreamBuilder<List<CustomGame>>(
        stream: uid.isEmpty
            ? const Stream<List<CustomGame>>.empty()
            : CustomGamesRepository.instance.watch(uid),
        builder: (context, snap) {
          final customGames = snap.data ?? const <CustomGame>[];

          final tiles = <Widget>[
            for (final cat in categories)
              _CatalogRow(
                image: cat.image,
                icon: cat.icon,
                label: cat.label,
                // Diet and Education are one game with chapters; the rest count
                // their games directly.
                subtitle: GameCatalog.chapterGameFor(cat) != null
                    ? '${byCategory[cat]!.length} chapters'
                    : _gameCountLabel(byCategory[cat]!.length),
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => CategoryIntroScreen(category: cat),
                  ),
                ),
              ),
            if (customGames.isNotEmpty)
              _CatalogRow(
                image: null,
                icon: Icons.auto_awesome_outlined,
                label: 'Your Goals',
                subtitle: _gameCountLabel(customGames.length),
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => const CustomGamesScreen(),
                  ),
                ),
              ),
          ];

          return ListView.separated(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            itemCount: tiles.length,
            separatorBuilder: (context, i) => const SizedBox(height: 14),
            itemBuilder: (context, i) => tiles[i],
          );
        },
      ),
    );
  }
}

// A wide row: photo on the left, topic name and game count on the right.
// The whole row is tappable; screen reader reads it as one line.
class _CatalogRow extends StatelessWidget {
  final String? image; // Photo asset path, or null to fall back to the icon.
  final IconData icon;
  final String label;
  final String subtitle; // e.g. "2 chapters" or "1 game".
  final VoidCallback onTap;

  const _CatalogRow({
    required this.image,
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    const rowHeight = 96.0;
    return Semantics(
      button: true,
      label: '$label, $subtitle',
      child: Material(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(20),
          child: Container(
            height: rowHeight,
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
                // Left: the topic photo, filling a rounded square.
                ClipRRect(
                  borderRadius: const BorderRadius.horizontal(
                    left: Radius.circular(20),
                  ),
                  child: SizedBox(
                    width: rowHeight,
                    height: rowHeight,
                    child: image == null
                        ? _IconTile(icon: icon)
                        : Image.asset(
                            image!,
                            fit: BoxFit.cover,
                            // If the photo is missing, show the icon instead of
                            // a broken image so the list still works.
                            errorBuilder: (context, error, stack) =>
                                _IconTile(icon: icon),
                          ),
                  ),
                ),
                const SizedBox(width: 16),
                // Right: the factor name and game count.
                Expanded(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        label,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: AppColors.title,
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        subtitle,
                        style: const TextStyle(
                          color: AppColors.subtitle,
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
                const Padding(
                  padding: EdgeInsets.only(right: 16),
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

// Fallback shown when a topic has no photo (or the photo fails to load).
class _IconTile extends StatelessWidget {
  final IconData icon;
  const _IconTile({required this.icon});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.primary.withValues(alpha: 0.1),
      child: Icon(icon, color: AppColors.primary, size: 36),
    );
  }
}
