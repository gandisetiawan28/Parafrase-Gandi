// backend_service.dart
// Registry/system operations executed via visible BAT script for user transparency.
// Results stream live to both the Flutter Log UI and a visible CMD window.

import 'dart:io';
import 'dart:convert';
import 'package:http/http.dart' as http;

const String _rawId      = '9BB7A975-B568-4B2D-9683-39FD2900118F';
const String _catalogId  = '{C1234567-ABCD-1234-ABCD-1234567890AB}';
const String _appName    = 'ParafraseGandi';
const String _remoteBase = 'https://gandisetiawan28.github.io/Parafrase-Gandi';
const String _version    = '2.4.0';

String get _appId => '{$_rawId}';

String getAppFolder() {
  final localApp = Platform.environment['LOCALAPPDATA'] ?? '';
  return '$localApp\\$_appName';
}

// ─── Visible Execution Helper ─────────────────────────────
// Generates a temp .bat and runs it in a visible CMD window
Future<void> _runVisible(String title, List<String> commands) async {
  final tempDir = Directory.systemTemp;
  final batFile = File('${tempDir.path}\\gandi_op_${DateTime.now().millisecondsSinceEpoch}.bat');
  
  final script = [
    '@echo off',
    'title $title',
    'color 0A',
    'echo ===================================================',
    'echo   $title',
    'echo ===================================================',
    ...commands,
    'echo.',
    'echo Operasi Selesai!',
    'timeout /t 3 >nul',
    'exit'
  ].join('\r\n');

  await batFile.writeAsString(script, encoding: utf8);
  
  // Use 'start /wait' to show a visible window and wait for completion
  await Process.run('cmd.exe', ['/c', 'start', '/wait', '', batFile.path]);
  
  try { await batFile.delete(); } catch (_) {}
}

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

// ─── Install (Visible CMD) ────────────────────────────────
Stream<String> runFullInstall() async* {
  yield '⚙ Memulai instalasi Parafrase Gandi v$_version...';

  final apd = getAppFolder();
  final dir = Directory(apd);
  if (!dir.existsSync()) {
    dir.createSync(recursive: true);
    yield '📁 Folder dibuat: $apd';
  }

  // Copy icons
  try {
    final exeDir = File(Platform.resolvedExecutable).parent.path;
    final assetsDir = '$exeDir\\data\\flutter_assets\\assets';
    for (final icon in ['icon-32.png', 'icon-64.png', 'icon-128.png']) {
      final src = File('$assetsDir\\$icon');
      if (src.existsSync()) src.copySync('$apd\\$icon');
    }
    yield '🖼 Ikon aplikasi disinkronkan';
  } catch (_) {}

  final manifestPath = '$apd\\manifest.xml';
  File(manifestPath).writeAsStringSync(getManifestXml(apd), encoding: utf8);
  yield '📄 Manifest ditulis: $manifestPath';

  final fileUrl = 'file:///${manifestPath.replaceAll('\\', '/')}';
  final privacy  = r'HKCU\Software\Microsoft\Office\16.0\Common\Privacy';
  final trustCent= r'HKCU\Software\Microsoft\Office\16.0\WEF\TrustCenter';
  final trustLoc = r'HKCU\Software\Microsoft\Office\16.0\Word\Security\Trusted Locations\ParafraseGandi';
  final wef16    = r'HKCU\Software\Microsoft\Office\16.0\Wef';
  final wordWef  = r'HKCU\Software\Microsoft\Office\16.0\Word\Wef';
  final cacheDir = '${Platform.environment['LOCALAPPDATA']}\\Microsoft\\Office\\16.0\\Wef';

  yield '🚀 Membuka jendela teknis (CMD)...';

  final commands = [
    'echo [1/6] Menutup Microsoft Word...',
    'taskkill /f /im winword.exe 2>nul',
    'echo [2/6] Membuka blokir keamanan Office...',
    'reg add "$privacy" /v "ConnectedExperiencesAllowed" /t REG_DWORD /d 1 /f',
    'reg add "$privacy" /v "OptionalConnectedExperiencesAllowed" /t REG_DWORD /d 1 /f',
    'reg add "$trustCent" /v "DisableAllWefAddins" /t REG_DWORD /d 0 /f',
    'reg add "$trustCent" /v "BlockWebAddins" /t REG_DWORD /d 0 /f',
    'echo [3/6] Membuat Network Share lokal...',
    'net share GandiAddin /delete /y 2>nul',
    'net share GandiAddin="$apd" /grant:Everyone,READ',
    'echo [4/6] Mendaftarkan lokasi terpercaya...',
    'reg add "$trustLoc" /v "Path" /t REG_SZ /d "$apd" /f',
    'reg add "$trustLoc" /v "AllowSubfolders" /t REG_DWORD /d 1 /f',
    'echo [5/6] Mendaftarkan Add-in ke Office registry...',
    'reg delete "$wef16\\Developer\\$_appId" /f 2>nul',
    'reg delete "$wef16\\TrustedCatalogs\\$_catalogId" /f 2>nul',
    'reg add "$wef16\\TrustedCatalogs\\$_catalogId" /v "Id" /t REG_SZ /d "$_catalogId" /f',
    'reg add "$wef16\\TrustedCatalogs\\$_catalogId" /v "Url" /t REG_SZ /d "\\\\127.0.0.1\\GandiAddin" /f',
    'reg add "$wef16\\TrustedCatalogs\\$_catalogId" /v "Flags" /t REG_DWORD /d 1 /f',
    'reg add "$wef16\\TrustedCatalogs\\$_catalogId" /v "ShowInMenu" /t REG_DWORD /d 1 /f',
    'reg add "$wordWef\\TrustedCatalogs\\$_catalogId" /v "Id" /t REG_SZ /d "$_catalogId" /f',
    'reg add "$wordWef\\TrustedCatalogs\\$_catalogId" /v "Url" /t REG_SZ /d "\\\\127.0.0.1\\GandiAddin" /f',
    'reg add "$wordWef\\TrustedCatalogs\\$_catalogId" /v "Flags" /t REG_DWORD /d 1 /f',
    'reg add "$wef16\\Developer\\$_appId" /v "Url" /t REG_SZ /d "$fileUrl" /f',
    'reg add "$wordWef\\Developer\\$_appId" /v "Url" /t REG_SZ /d "$fileUrl" /f',
    'echo [6/6] Membersihkan cache Office...',
    'rmdir /s /q "$cacheDir" 2>nul',
    'echo.',
    'echo Menjalankan ulang Microsoft Word...',
    'start "" winword.exe'
  ];

  await _runVisible('Installer Parafrase Gandi', commands);

  yield '   ✓ Operasi CMD selesai';
  yield '';
  yield '✅ SUKSES! Buka Word → Insert → My Add-ins → Shared Folder';
}

// ─── Uninstall (Visible CMD) ─────────────────────────────
Stream<String> runUninstall() async* {
  yield '🗑 Menghapus Parafrase Gandi...';
  yield '🚀 Membuka jendela teknis (CMD)...';

  final wef16 = r'HKCU\Software\Microsoft\Office\16.0\Wef';
  final cacheDir = '${Platform.environment['LOCALAPPDATA']}\\Microsoft\\Office\\16.0\\Wef';

  final commands = [
    'echo Menutup Microsoft Word...',
    'taskkill /f /im winword.exe 2>nul',
    'echo Menghapus entri registry...',
    'reg delete "$wef16\\Developer\\$_appId" /f 2>nul',
    'reg delete "$wef16\\TrustedCatalogs\\$_catalogId" /f 2>nul',
    'echo Menghapus Network Share...',
    'net share GandiAddin /delete /y 2>nul',
    'echo Membersihkan cache Office...',
    'rmdir /s /q "$cacheDir" 2>nul'
  ];

  await _runVisible('Uninstaller Parafrase Gandi', commands);

  yield '   ✓ Operasi CMD selesai';
  yield '';
  yield '✅ Selesai! Add-in telah dihapus.';
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
    final response = await http.get(uri).timeout(const Duration(seconds: 10));
    if (response.statusCode == 200) {
      final body = response.body;
      String latest = _version;
      try {
        final data = jsonDecode(body) as Map<String, dynamic>;
        latest = data['version']?.toString() ?? _version;
      } catch (_) {}
      if (latest == _version || body.contains(_version)) {
        return UpdateResult(isLatest: true, latestVersion: latest, message: 'Versi terbaru ($_version).');
      } else {
        return UpdateResult(isLatest: false, latestVersion: latest, message: 'Versi baru: $latest');
      }
    }
    return UpdateResult(isLatest: true, latestVersion: _version, message: 'Cek update gagal (HTTP ${response.statusCode}).');
  } catch (e) {
    return UpdateResult(isLatest: true, latestVersion: _version, message: 'Error: $e');
  }
}

Future<String> downloadLatestManifest() async {
  try {
    final uri = Uri.parse('$_remoteBase/manifest.xml');
    final response = await http.get(uri).timeout(const Duration(seconds: 15));
    if (response.statusCode == 200) {
      final apd = getAppFolder();
      Directory(apd).createSync(recursive: true);
      File('$apd\\manifest.xml').writeAsStringSync(response.body, encoding: utf8);
      return '✅ Manifest diperbarui!';
    }
    return '⚠ Download gagal.';
  } catch (e) {
    return '❌ Error: $e';
  }
}

// ─── Helpers ──────────────────────────────────────────────
Future<String> detectWordVersion() async {
  try {
    final r = await Process.run('reg', ['query', r'HKLM\Software\Microsoft\Office\ClickToRun\Configuration', '/v', 'VersionToReport']);
    if (r.exitCode == 0) {
      final match = RegExp(r'VersionToReport\s+REG_SZ\s+(\S+)').firstMatch(r.stdout.toString());
      if (match != null) return 'Word 365/2021 (v${match.group(1)})';
    }
  } catch (_) {}
  return 'Microsoft Word';
}

Future<bool> isAddinInstalled() async {
  final r = await Process.run('reg', ['query', r'HKCU\Software\Microsoft\Office\16.0\Wef\Developer\' + _appId]);
  return r.exitCode == 0;
}

Future<bool> checkServerConnection() async {
  try {
    final result = await InternetAddress.lookup('gandisetiawan28.github.io');
    return result.isNotEmpty;
  } catch (_) { return false; }
}
