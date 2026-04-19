// backend_service.dart
// All registry/system operations are executed directly via Process.run
// — NO BAT files, NO visible CMD windows —
// Results stream live to the Flutter log UI.

import 'dart:io';
import 'dart:convert';
import 'package:http/http.dart' as http;

const String _rawId      = '9BB7A975-B568-4B2D-9683-39FD2900118F';
const String _catalogId  = '{C1234567-ABCD-1234-ABCD-1234567890AB}';
const String _appName    = 'ParafraseGandi';
const String _remoteBase = 'https://gandisetiawan28.github.io/Parafrase-Gandi';
const String _version    = '2.4.0';

String get _appId => '{$_rawId}';

// ─── Paths ────────────────────────────────────────────────
String getAppFolder() {
  final localApp = Platform.environment['LOCALAPPDATA'] ?? '';
  return '$localApp\\$_appName';
}

// ─── Silent Process Helper ────────────────────────────────
// Runs any exe silently (no window flicker)
Future<_CmdResult> _run(String exe, List<String> args) async {
  final result = await Process.run(
    exe, args,
    runInShell: false,       // never spawns cmd.exe
    includeParentEnvironment: true,
    stdoutEncoding: const SystemEncoding(),
    stderrEncoding: const SystemEncoding(),
  );
  return _CmdResult(result.exitCode, result.stdout.toString().trim(), result.stderr.toString().trim());
}

class _CmdResult {
  final int code; final String out; final String err;
  _CmdResult(this.code, this.out, this.err);
  bool get ok => code == 0;
}

// ─── Registry helper wrappers ─────────────────────────────
Future<_CmdResult> _regAdd(String key, String name, String type, String value) =>
    _run('reg', ['add', key, '/v', name, '/t', type, '/d', value, '/f']);

Future<_CmdResult> _regAddDword(String key, String name, int value) =>
    _regAdd(key, name, 'REG_DWORD', value.toString());

Future<_CmdResult> _regAddStr(String key, String name, String value) =>
    _regAdd(key, name, 'REG_SZ', value);

Future<_CmdResult> _regDelete(String key) =>
    _run('reg', ['delete', key, '/f']);

Future<_CmdResult> _regQuery(String key) =>
    _run('reg', ['query', key]);

// ─── Manifest XML ─────────────────────────────────────────
String getManifestXml(String apd) {
  final icon32 = 'file:///${apd.replaceAll('\\', '/')}/icon-32.png';
  final icon64 = 'file:///${apd.replaceAll('\\', '/')}/icon-64.png';
  return '''<?xml version="1.0" encoding="UTF-8"?>
<OfficeApp xmlns="http://schemas.microsoft.com/office/appforoffice/1.1"
           xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
           xsi:type="TaskPaneApp">
  <Id>$_rawId</Id>
  <Version>1.0.0.0</Version>
  <ProviderName>Parafrase Gandi</ProviderName>
  <DefaultLocale>id-ID</DefaultLocale>
  <DisplayName DefaultValue="Parafrase Gandi"/>
  <Description DefaultValue="AI Writing Assistant for Microsoft Word"/>
  <IconUrl DefaultValue="$icon32"/>
  <HighResolutionIconUrl DefaultValue="$icon64"/>
  <SupportUrl DefaultValue="$_remoteBase"/>
  <Hosts>
    <Host Name="Document"/>
  </Hosts>
  <DefaultSettings>
    <SourceLocation DefaultValue="$_remoteBase/taskpane.html"/>
  </DefaultSettings>
  <Permissions>ReadWriteDocument</Permissions>
</OfficeApp>''';
}

// ─── Install (fully silent, no CMD window) ────────────────
Stream<String> runFullInstall() async* {
  yield '⚙ Memulai instalasi Parafrase Gandi v$_version...';

  // 1. Create app folder + write manifest
  final apd = getAppFolder();
  final dir = Directory(apd);
  if (!dir.existsSync()) {
    dir.createSync(recursive: true);
    yield '📁 Folder dibuat: $apd';
  }

  // Copy icons from app assets to apd for Word to use
  try {
    final exeDir = File(Platform.resolvedExecutable).parent.path;
    final assetsDir = '$exeDir\\data\\flutter_assets\\assets';
    for (final icon in ['icon-32.png', 'icon-64.png', 'icon-128.png']) {
      final src = File('$assetsDir\\$icon');
      if (src.existsSync()) {
        src.copySync('$apd\\$icon');
      }
    }
    yield '🖼 Ikon aplikasi disinkronkan';
  } catch (_) {}

  final manifestPath = '$apd\\manifest.xml';
  File(manifestPath).writeAsStringSync(getManifestXml(apd), encoding: utf8);
  yield '📄 Manifest ditulis: $manifestPath';

  final fileUrl = 'file:///${manifestPath.replaceAll('\\', '/')}';

  // 2. Kill Word silently
  yield '📌 Menutup Microsoft Word...';
  await _run('taskkill', ['/f', '/im', 'winword.exe']);
  await Future.delayed(const Duration(milliseconds: 600));

  // 3. Trust Center
  yield '🔓 [1/5] Membuka blokir keamanan Office...';
  final privacy  = r'HKCU\Software\Microsoft\Office\16.0\Common\Privacy';
  final trustCent= r'HKCU\Software\Microsoft\Office\16.0\WEF\TrustCenter';
  await _regAddDword(privacy, 'ConnectedExperiencesAllowed', 1);
  await _regAddDword(privacy, 'OptionalConnectedExperiencesAllowed', 1);
  await _regAddDword(trustCent, 'DisableAllWefAddins', 0);
  await _regAddDword(trustCent, 'BlockWebAddins', 0);
  yield '   ✓ Trust Center unlocked';

  // 4. Network Share
  yield '🌐 [2/5] Membuat Network Share lokal...';
  await _run('net', ['share', 'GandiAddin', '/delete', '/y']);
  final shareResult = await _run('net', ['share', 'GandiAddin=$apd', '/grant:Everyone,READ']);
  yield shareResult.ok ? '   ✓ Share GandiAddin aktif' : '   ⚠ Share: ${shareResult.err}';

  // 5. Trusted Location
  yield '🔐 [3/5] Mendaftarkan lokasi terpercaya...';
  final trustLoc = r'HKCU\Software\Microsoft\Office\16.0\Word\Security\Trusted Locations\ParafraseGandi';
  await _regAddStr(trustLoc, 'Path', apd);
  await _regAddDword(trustLoc, 'AllowSubfolders', 1);
  yield '   ✓ Trusted Location registered';

  // 6. WEF / Add-in Registration
  yield '💉 [4/5] Mendaftarkan Add-in ke Office registry...';
  final wef16   = r'HKCU\Software\Microsoft\Office\16.0\Wef';
  final wordWef = r'HKCU\Software\Microsoft\Office\16.0\Word\Wef';

  await _regDelete('$wef16\\Developer\\$_appId');
  await _regDelete('$wef16\\TrustedCatalogs\\$_catalogId');

  // Trusted Catalog (Wef)
  await _regAddStr('$wef16\\TrustedCatalogs\\$_catalogId', 'Id', _catalogId);
  await _regAddStr('$wef16\\TrustedCatalogs\\$_catalogId', 'Url', '\\\\127.0.0.1\\GandiAddin');
  await _regAddDword('$wef16\\TrustedCatalogs\\$_catalogId', 'Flags', 1);
  await _regAddDword('$wef16\\TrustedCatalogs\\$_catalogId', 'ShowInMenu', 1);

  // Trusted Catalog (Word.Wef)
  await _regAddStr('$wordWef\\TrustedCatalogs\\$_catalogId', 'Id', _catalogId);
  await _regAddStr('$wordWef\\TrustedCatalogs\\$_catalogId', 'Url', '\\\\127.0.0.1\\GandiAddin');
  await _regAddDword('$wordWef\\TrustedCatalogs\\$_catalogId', 'Flags', 1);

  // Developer sideload
  await _regAddStr('$wef16\\Developer\\$_appId', 'Url', fileUrl);
  await _regAddStr('$wordWef\\Developer\\$_appId', 'Url', fileUrl);
  yield '   ✓ Add-in terdaftar di registry';

  // 7. Clear Office cache
  yield '🧹 [5/5] Membersihkan cache Office...';
  final cacheDir = '${Platform.environment['LOCALAPPDATA']}\\Microsoft\\Office\\16.0\\Wef';
  try {
    final cd = Directory(cacheDir);
    if (cd.existsSync()) cd.deleteSync(recursive: true);
    yield '   ✓ Cache Office dibersihkan';
  } catch (_) {
    yield '   ⚠ Cache tidak ditemukan (normal)';
  }

  // 8. Re-open Word
  yield '🚀 Membuka Microsoft Word...';
  await _run('cmd', ['/c', 'start', '', 'winword.exe']);

  yield '';
  yield '✅ SUKSES! Buka Word → Insert → My Add-ins → Shared Folder → Parafrase Gandi';
}

// ─── Uninstall (fully silent) ─────────────────────────────
Stream<String> runUninstall() async* {
  yield '🗑 Menghapus Parafrase Gandi...';

  yield '📌 Menutup Microsoft Word...';
  await _run('taskkill', ['/f', '/im', 'winword.exe']);
  await Future.delayed(const Duration(milliseconds: 600));

  final wef16 = r'HKCU\Software\Microsoft\Office\16.0\Wef';

  yield '🔧 Menghapus entri registry...';
  await _regDelete('$wef16\\Developer\\$_appId');
  await _regDelete('$wef16\\TrustedCatalogs\\$_catalogId');
  await _regDelete('$wef16\\TrustedCatalogs\\$_appId');
  yield '   ✓ Registry dibersihkan';

  yield '🌐 Menghapus Network Share...';
  await _run('net', ['share', 'GandiAddin', '/delete', '/y']);
  yield '   ✓ Share dihapus';

  yield '🧹 Membersihkan cache Office...';
  final cacheDir = '${Platform.environment['LOCALAPPDATA']}\\Microsoft\\Office\\16.0\\Wef';
  try {
    final cd = Directory(cacheDir);
    if (cd.existsSync()) cd.deleteSync(recursive: true);
    yield '   ✓ Cache dibersihkan';
  } catch (_) {}

  yield '';
  yield '✅ Selesai! Add-in telah dihapus dari Microsoft Word.';
}

// ─── Fix / Repair ─────────────────────────────────────────
Stream<String> runFixRegistry() async* {
  yield '🔧 Memulai repair & optimize...';
  yield* runFullInstall();
}

// ─── Check Update ─────────────────────────────────────────
class UpdateResult {
  final bool isLatest;
  final String latestVersion;
  final String message;
  UpdateResult({required this.isLatest, required this.latestVersion, required this.message});
}

Future<UpdateResult> runCheckUpdate() async {
  try {
    final uri = Uri.parse('$_remoteBase/version.json');
    final response = await http
        .get(uri, headers: {'User-Agent': 'GandiManager/$_version'})
        .timeout(const Duration(seconds: 10));

    if (response.statusCode == 200) {
      final body = response.body;
      String latest = _version;
      try {
        final data = jsonDecode(body) as Map<String, dynamic>;
        latest = data['version']?.toString() ?? _version;
      } catch (_) {}

      if (latest == _version || body.contains(_version)) {
        return UpdateResult(isLatest: true, latestVersion: latest,
            message: 'Anda sudah menggunakan versi terbaru ($_version).');
      } else {
        return UpdateResult(isLatest: false, latestVersion: latest,
            message: 'Versi baru tersedia: $latest');
      }
    }
    return UpdateResult(isLatest: true, latestVersion: _version,
        message: 'Tidak dapat memeriksa update (HTTP ${response.statusCode}).');
  } catch (e) {
    return UpdateResult(isLatest: true, latestVersion: _version,
        message: 'Gagal cek update: $e');
  }
}

Future<String> downloadLatestManifest() async {
  try {
    final uri = Uri.parse('$_remoteBase/manifest.xml');
    final response = await http
        .get(uri, headers: {'User-Agent': 'GandiManager/$_version'})
        .timeout(const Duration(seconds: 15));
    if (response.statusCode == 200) {
      final apd = getAppFolder();
      Directory(apd).createSync(recursive: true);
      File('$apd\\manifest.xml').writeAsStringSync(response.body, encoding: utf8);
      return '✅ Manifest diperbarui!';
    }
    return '⚠ Gagal unduh manifest (HTTP ${response.statusCode}).';
  } catch (e) {
    return '❌ Error: $e';
  }
}

// ─── Word Detection ───────────────────────────────────────
Future<String> detectWordVersion() async {
  try {
    final r = await _run('reg', [
      'query',
      r'HKLM\Software\Microsoft\Office\ClickToRun\Configuration',
      '/v', 'VersionToReport'
    ]);
    final match = RegExp(r'VersionToReport\s+REG_SZ\s+(\S+)').firstMatch(r.out);
    if (match != null) {
      final ver = match.group(1)!;
      if (ver.startsWith('16.')) return 'Word 365/2021 (v$ver)';
    }
  } catch (_) {}

  for (final entry in [
    (r'HKLM\Software\Microsoft\Office\16.0\Word\InstallRoot', 'Word 2016/2019/2021/365'),
    (r'HKLM\Software\Microsoft\Office\15.0\Word\InstallRoot', 'Word 2013'),
  ]) {
    final r = await _run('reg', ['query', entry.$1, '/v', 'Path']);
    if (r.ok) return entry.$2;
  }
  return 'Tidak Ditemukan';
}

// ─── Add-in Installed Status ──────────────────────────────
Future<bool> isAddinInstalled() async {
  final r = await _regQuery(
      r'HKCU\Software\Microsoft\Office\16.0\Wef\Developer\' + _appId);
  return r.ok;
}

// ─── Server Connectivity ─────────────────────────────────
Future<bool> checkServerConnection() async {
  try {
    final result = await InternetAddress.lookup('gandisetiawan28.github.io');
    return result.isNotEmpty && result.first.rawAddress.isNotEmpty;
  } catch (_) {
    return false;
  }
}
