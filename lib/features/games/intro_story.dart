// Reads a factor's intro story (assets/game-src/intro/*.tw) into plain data so
// Flutter can draw it directly.
//
// These stories are still authored in Twine. They used to be compiled into a
// SugarCube page and shown in a WebView, which meant the screen carried two
// designs at once: the app's own header and, underneath it, the web page's
// header and buttons. Parsing the source instead keeps Twine as the place the
// words are written while the screen stays native, consistent, and quick to
// open.
//
// The intro files are deliberately plain: three passages, each a heading and a
// few paragraphs, chained by a single button. Anything richer belongs in a real
// game, not an intro, so this reader stays small on purpose.
library;

// One screen of an intro story.
class IntroPage {
  final String? heading;

  // Paragraph text, still carrying <b> and <i> tags. Drawn by _RichLine.
  final List<String> paragraphs;

  // Label on the button that moves forward, e.g. "Next" or "See the games".
  final String buttonLabel;

  // True on the passage that hands off to the games list.
  final bool isLast;

  const IntroPage({
    this.heading,
    required this.paragraphs,
    required this.buttonLabel,
    required this.isLast,
  });
}

class IntroStory {
  final String title;
  final List<IntroPage> pages;

  const IntroStory({required this.title, required this.pages});

  bool get isEmpty => pages.isEmpty;
}

// Splits a .tw file into passage name -> body.
Map<String, String> _splitPassages(String source) {
  final out = <String, String>{};
  final lines = source.split('\n');
  String? name;
  final buf = <String>[];

  void flush() {
    final current = name;
    if (current != null) out[current] = buf.join('\n').trim();
    buf.clear();
  }

  for (final line in lines) {
    if (line.startsWith(':: ')) {
      flush();
      // Strip the tag and metadata that follow the name:
      //   ":: Start [nobr] {"position": ...}"
      var raw = line.substring(3).trim();
      final cut = raw.indexOf(RegExp(r'\s*[\[{]'));
      if (cut != -1) raw = raw.substring(0, cut);
      name = raw.trim();
    } else if (name != null) {
      buf.add(line);
    }
  }
  flush();
  return out;
}

// Pulls the start passage out of the StoryData block. Falls back to "Start".
String _startPassage(Map<String, String> passages) {
  final data = passages['StoryData'];
  if (data == null) return 'Start';
  final m = RegExp(r'"start"\s*:\s*"([^"]+)"').firstMatch(data);
  return m?.group(1) ?? 'Start';
}

final _tagRe = RegExp(r'<[^>]+>');
final _wsRe = RegExp(r'\s+');

// Collapses newlines and indentation from the source into single spaces, and
// turns the handful of entities these files use back into characters.
String _tidy(String s) => s
    .replaceAll(_wsRe, ' ')
    .replaceAll('&nbsp;', ' ')
    .replaceAll('&amp;', '&')
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>')
    .trim();

IntroPage? _parsePage(String body) {
  // The header div repeats the story title, which the app bar already shows.
  final withoutHeader =
      body.replaceAll(RegExp(r'<div class="header">.*?</div>', dotAll: true), '');

  final heading = RegExp(r'<h1>(.*?)</h1>', dotAll: true)
      .firstMatch(withoutHeader)
      ?.group(1);

  final paragraphs = RegExp(r'<p class="lead">(.*?)</p>', dotAll: true)
      .allMatches(withoutHeader)
      .map((m) => _tidy(m.group(1)!))
      .where((p) => p.isNotEmpty)
      .toList();

  final button = RegExp(r'<<button\s+"([^"]+)"').firstMatch(withoutHeader);

  if (heading == null && paragraphs.isEmpty) return null;

  return IntroPage(
    heading: heading == null ? null : _tidy(heading.replaceAll(_tagRe, '')),
    paragraphs: paragraphs,
    buttonLabel: button?.group(1) ?? 'Next',
    // The last passage posts CCQ_OPEN_GAMES instead of moving to another one.
    isLast: withoutHeader.contains('CCQ_OPEN_GAMES'),
  );
}

// Walks the story from its start passage, following each <<goto>>, and returns
// the pages in the order a player sees them. Following the chain rather than
// file order means reordering passages in Twine cannot scramble the screens.
IntroStory parseIntroStory(String source, {required String fallbackTitle}) {
  final passages = _splitPassages(source);
  final title = passages['StoryTitle']?.trim();

  final pages = <IntroPage>[];
  final seen = <String>{};
  var current = _startPassage(passages);

  // The bound is a safety net: a story that loops back on itself would
  // otherwise spin here.
  while (passages.containsKey(current) && seen.add(current) && pages.length < 20) {
    final body = passages[current]!;
    final page = _parsePage(body);
    if (page != null) pages.add(page);

    final next = RegExp(r'<<goto\s+"([^"]+)"').firstMatch(body)?.group(1);
    if (next == null) break;
    current = next;
  }

  return IntroStory(
    title: (title == null || title.isEmpty) ? fallbackTitle : title,
    pages: pages,
  );
}
