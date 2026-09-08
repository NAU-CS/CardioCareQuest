// The short story you land on after tapping a factor in the Game Catalog.
//
// Every factor uses this one screen: home on the left, the factor name in the
// middle, a menu on the right, then the topic photo, the story, and the
// buttons. It used to draw the app's header and then hand the rest of the
// screen to a WebView that brought its own header and buttons, so a player saw
// two different designs stacked on one page. The story is still written in
// Twine; it is read straight from the .tw source now (see intro_story.dart) and
// drawn with normal widgets, so there is a single layout and no browser to
// start up.

import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;

import '../../../core/hooks/telemetry_hooks.dart';
import '../../../core/theme/app_colors.dart';
import '../../games/game_stories.dart';
import '../../games/intro_story.dart';
import '../widgets/intro_story_view.dart';
import 'category_games_screen.dart';

class CategoryIntroScreen extends StatefulWidget {
  final GameCategory category;

  const CategoryIntroScreen({super.key, required this.category});

  @override
  State<CategoryIntroScreen> createState() => _CategoryIntroScreenState();
}

class _CategoryIntroScreenState extends State<CategoryIntroScreen> {
  Future<IntroStory>? _story;
  final _pageController = PageController();
  int _page = 0;

  @override
  void initState() {
    super.initState();
    _story = _load();
    TelemetryHooks.logEvent(
      'factor_intro_opened',
      parameters: {'factor': widget.category.name},
    );
  }

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  Future<IntroStory> _load() async {
    final source = await rootBundle.loadString(widget.category.introTwAsset);
    return parseIntroStory(source, fallbackTitle: widget.category.label);
  }

  void _openGames({required String how}) {
    if (!mounted) return;
    TelemetryHooks.logEvent(
      'factor_intro_$how',
      parameters: {'factor': widget.category.name, 'page': _page + 1},
    );
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (_) => CategoryGamesScreen(category: widget.category),
      ),
    );
  }

  // Back to the dashboard, however deep the player has gone.
  void _goHome() => Navigator.of(context).popUntil((r) => r.isFirst);

  void _next(int total) {
    if (_page >= total - 1) {
      _openGames(how: 'finished');
      return;
    }
    _pageController.nextPage(
      duration: const Duration(milliseconds: 220),
      curve: Curves.easeOut,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppColors.title),
        leading: IconButton(
          icon: const Icon(Icons.home_outlined),
          tooltip: 'Home',
          onPressed: _goHome,
        ),
        title: Text(
          widget.category.label,
          style: const TextStyle(
            color: AppColors.title,
            fontWeight: FontWeight.bold,
          ),
        ),
        centerTitle: true,
        actions: [
          PopupMenuButton<String>(
            icon: const Icon(Icons.more_vert),
            tooltip: 'More',
            onSelected: (value) {
              if (value == 'skip') _openGames(how: 'skipped');
              if (value == 'home') _goHome();
            },
            itemBuilder: (context) => const [
              PopupMenuItem(
                value: 'skip',
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.skip_next_outlined),
                  title: Text('Skip to the games'),
                ),
              ),
              PopupMenuItem(
                value: 'home',
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.home_outlined),
                  title: Text('Back to dashboard'),
                ),
              ),
            ],
          ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: FutureBuilder<IntroStory>(
          future: _story,
          builder: (context, snap) {
            if (snap.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            final story = snap.data;
            // A missing or unreadable story should never strand the player.
            if (story == null || story.isEmpty) {
              return _SkipOnly(onGo: () => _openGames(how: 'unavailable'));
            }
            return IntroStoryView(
              category: widget.category,
              story: story,
              controller: _pageController,
              page: _page,
              onPageChanged: (i) => setState(() => _page = i),
              onNext: () => _next(story.pages.length),
              onSkip: () => _openGames(how: 'skipped'),
            );
          },
        ),
      ),
    );
  }
}

// Shown only if the story cannot be read, so the games stay reachable.
class _SkipOnly extends StatelessWidget {
  final VoidCallback onGo;
  const _SkipOnly({required this.onGo});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: FilledButton(
          onPressed: onGo,
          style: FilledButton.styleFrom(
            backgroundColor: AppColors.primary,
            minimumSize: const Size.fromHeight(52),
          ),
          child: const Text('See the games'),
        ),
      ),
    );
  }
}
