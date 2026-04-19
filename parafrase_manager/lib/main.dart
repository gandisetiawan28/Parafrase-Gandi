import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'backend_service.dart';

void main() {
  runApp(const ParafraseGandiApp());
}

class ParafraseGandiApp extends StatelessWidget {
  const ParafraseGandiApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Parafrase Gandi Manager',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: const ColorScheme.light(
          primary: Color(0xFF0058BC),
          primaryContainer: Color(0xFFD8E2FF),
          secondary: Color(0xFF585E71),
          surface: Color(0xFFF4F6FB),
          surfaceContainer: Color(0xFFEEF0F6),
          onSurface: Color(0xFF1A1C1F),
          onSurfaceVariant: Color(0xFF5A5E6E),
          outline: Color(0xFFBEC3D4),
        ),
        fontFamily: 'Segoe UI',
        pageTransitionsTheme: const PageTransitionsTheme(builders: {
          TargetPlatform.windows: FadeUpwardsPageTransitionsBuilder(),
        }),
      ),
      home: const ManagerShell(),
    );
  }
}

class ManagerShell extends StatefulWidget {
  const ManagerShell({super.key});

  @override
  State<ManagerShell> createState() => _ManagerShellState();
}

class _ManagerShellState extends State<ManagerShell>
    with SingleTickerProviderStateMixin {
  int _tab = 0;
  late AnimationController _fadeCtrl;
  late Animation<double> _fadeAnim;

  @override
  void initState() {
    super.initState();
    _fadeCtrl = AnimationController(vsync: this, duration: const Duration(milliseconds: 200));
    _fadeAnim = CurvedAnimation(parent: _fadeCtrl, curve: Curves.easeOut);
    _fadeCtrl.forward();
  }

  void _switchTab(int idx) {
    if (idx == _tab) return;
    _fadeCtrl.reverse().then((_) {
      setState(() => _tab = idx);
      _fadeCtrl.forward();
    });
  }

  @override
  void dispose() {
    _fadeCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    const pages = [HomePage(), InstallPage(), UpdatePage(), DiagnosaPage()];

    return Scaffold(
      backgroundColor: cs.surface,
      body: Column(
        children: [
          // ─── Header ───
          Container(
            height: 62,
            color: cs.surface.withOpacity(0.95),
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: Row(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(6),
                  child: Image.asset('assets/icon-32.png', width: 26, height: 26),
                ),
                const SizedBox(width: 10),
                Text('Parafrase Gandi',
                    style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: cs.onSurface)),
                const Spacer(),
                Text('v2.4.0',
                    style: TextStyle(
                        fontSize: 12,
                        color: cs.onSurfaceVariant)),
              ],
            ),
          ),
          const Divider(height: 1, thickness: 1, color: Color(0xFFE0E4EE)),

          // ─── Page Content ───
          Expanded(
            child: FadeTransition(
              opacity: _fadeAnim,
              child: pages[_tab],
            ),
          ),

          // ─── Bottom Nav Dock ───
          Padding(
            padding: const EdgeInsets.fromLTRB(28, 0, 28, 20),
            child: Material(
              elevation: 4,
              borderRadius: BorderRadius.circular(32),
              shadowColor: Colors.black26,
              color: Colors.white.withOpacity(0.97),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(32),
                child: SizedBox(
                  height: 64,
                  child: Row(
                    children: [
                      _navItem(0, Icons.home_rounded, Icons.home_outlined, 'Home'),
                      _navItem(1, Icons.download_rounded, Icons.download_outlined, 'Install'),
                      _navItem(2, Icons.system_update_rounded, Icons.system_update_outlined, 'Update'),
                      _navItem(3, Icons.monitor_heart_rounded, Icons.monitor_heart_outlined, 'Diagnose'),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _navItem(int idx, IconData activeIcon, IconData inactiveIcon, String label) {
    final cs = Theme.of(context).colorScheme;
    final active = _tab == idx;
    return Expanded(
      child: InkWell(
        onTap: () => _switchTab(idx),
        borderRadius: BorderRadius.circular(32),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          curve: Curves.easeOut,
          padding: const EdgeInsets.symmetric(vertical: 10),
          decoration: active
              ? BoxDecoration(
                  color: cs.primaryContainer.withOpacity(0.4),
                  borderRadius: BorderRadius.circular(32))
              : null,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(active ? activeIcon : inactiveIcon,
                  color: active ? cs.primary : cs.onSurfaceVariant,
                  size: 22),
              const SizedBox(height: 3),
              Text(label,
                  style: TextStyle(
                      fontSize: 10.5,
                      fontWeight: active ? FontWeight.w700 : FontWeight.w500,
                      color: active ? cs.primary : cs.onSurfaceVariant)),
            ],
          ),
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════
//  SHARED WIDGETS
// ═══════════════════════════════════════

class BentoCard extends StatelessWidget {
  final Widget child;
  final Color? color;
  final double radius;
  final EdgeInsetsGeometry padding;

  const BentoCard({
    super.key,
    required this.child,
    this.color,
    this.radius = 20,
    this.padding = const EdgeInsets.all(20),
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Container(
      decoration: BoxDecoration(
        color: color ?? cs.surface,
        borderRadius: BorderRadius.circular(radius),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.06),
            blurRadius: 12,
            offset: const Offset(0, 4),
          )
        ],
        border: Border.all(color: Colors.black.withOpacity(0.05)),
      ),
      padding: padding,
      child: child,
    );
  }
}

class GradientButton extends StatefulWidget {
  final String label;
  final VoidCallback? onTap;
  final IconData? icon;
  final double height;
  final double fontSize;

  const GradientButton({
    super.key,
    required this.label,
    this.onTap,
    this.icon,
    this.height = 48,
    this.fontSize = 14,
  });

  @override
  State<GradientButton> createState() => _GradientButtonState();
}

class _GradientButtonState extends State<GradientButton>
    with SingleTickerProviderStateMixin {
  late AnimationController _hov;
  late Animation<double> _scale;

  @override
  void initState() {
    super.initState();
    _hov = AnimationController(vsync: this, duration: const Duration(milliseconds: 140));
    _scale = Tween(begin: 1.0, end: 1.02).animate(
        CurvedAnimation(parent: _hov, curve: Curves.easeOut));
  }

  @override
  void dispose() { _hov.dispose(); super.dispose(); }

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => _hov.forward(),
      onExit: (_) => _hov.reverse(),
      child: ScaleTransition(
        scale: _scale,
        child: GestureDetector(
          onTap: widget.onTap,
          child: AnimatedBuilder(
            animation: _hov,
            builder: (ctx, _) => Container(
              height: widget.height,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(widget.height / 2),
                gradient: LinearGradient(
                  colors: [
                    Color.lerp(const Color(0xFF0058BC), const Color(0xFF1A75D2), _hov.value)!,
                    Color.lerp(const Color(0xFF0070EB), const Color(0xFF2A8AF8), _hov.value)!,
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                boxShadow: [
                  BoxShadow(
                    color: const Color(0xFF0058BC).withOpacity(0.28 + _hov.value * 0.15),
                    blurRadius: 12 + _hov.value * 6,
                    offset: const Offset(0, 4),
                  )
                ],
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (widget.icon != null) ...[
                    Icon(widget.icon, color: Colors.white, size: widget.fontSize + 2),
                    const SizedBox(width: 8)
                  ],
                  Text(widget.label,
                      style: TextStyle(
                          color: Colors.white,
                          fontSize: widget.fontSize,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 0.3)),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════
//  HOME PAGE
// ═══════════════════════════════════════

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 0),
      children: [
        // Hero card
        Container(
          height: 185,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(22),
            gradient: const LinearGradient(
              colors: [Color(0xFF0058BC), Color(0xFF0070EB)],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
            boxShadow: const [BoxShadow(color: Color(0x440058BC), blurRadius: 18, offset: Offset(0, 6))],
          ),
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Parafrase Gandi',
                  style: TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.w800)),
              const SizedBox(height: 10),
              const Opacity(
                opacity: 0.85,
                child: Text('AI-powered semantic engine for professional academic writing.',
                    style: TextStyle(color: Colors.white, fontSize: 13)),
              ),
              const Spacer(),
              SizedBox(
                height: 38,
                width: 148,
                child: GradientButton(
                  label: 'Get Started',
                  icon: Icons.download_rounded,
                  height: 38,
                  fontSize: 12.5,
                  onTap: () {},
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),

        // Status row
        Row(children: [
          Expanded(
            flex: 5,
            child: BentoCard(
              color: Colors.white,
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Row(children: [
                  CircleAvatar(radius: 18, backgroundColor: cs.primaryContainer,
                      child: Icon(Icons.settings_rounded, color: cs.primary, size: 18)),
                  const SizedBox(width: 12),
                  const Text('System Status', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                ]),
                const SizedBox(height: 12),
                Text('All core services synchronized.\nEngine optimized for v2.4.0.',
                    style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant, height: 1.5)),
                const SizedBox(height: 10),
                Row(children: [
                  Icon(Icons.check_circle_rounded, color: const Color(0xFF15803D), size: 16),
                  const SizedBox(width: 6),
                  const Text('Active Runtime',
                      style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: Color(0xFF15803D))),
                ]),
              ]),
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            flex: 3,
            child: BentoCard(
              color: const Color(0xFFF3F6FF),
              child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
                Icon(Icons.cloud_sync_rounded, color: cs.primary, size: 32),
                const SizedBox(height: 8),
                Text('Cloud Sync', style: TextStyle(fontWeight: FontWeight.w700, color: cs.onSurface, fontSize: 12)),
                const SizedBox(height: 4),
                Text('Encrypted\nConnection', textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 10.5, color: cs.onSurfaceVariant, height: 1.4)),
              ]),
            ),
          ),
        ]),
        const SizedBox(height: 14),

        // Word detection
        BentoCard(
          color: Colors.white,
          padding: const EdgeInsets.all(16),
          child: Row(children: [
            Container(width: 50, height: 50, decoration: BoxDecoration(
                color: const Color(0xFFEFF6FF), borderRadius: BorderRadius.circular(12)),
                child: const Icon(Icons.description_rounded, color: Color(0xFF2563EB), size: 24)),
            const SizedBox(width: 14),
            Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              const Text('Add-in Environment', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
              const SizedBox(height: 4),
              Text('Plugin detected and ready for integration.',
                  style: TextStyle(fontSize: 11.5, color: Theme.of(context).colorScheme.onSurfaceVariant)),
            ])),
            Container(padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                decoration: BoxDecoration(color: const Color(0xFFDCFCE7), borderRadius: BorderRadius.circular(20)),
                child: const Text('Active', style: TextStyle(fontSize: 10.5, fontWeight: FontWeight.w700, color: Color(0xFF15803D)))),
          ]),
        ),
        const SizedBox(height: 14),

        // Diagnostics
        BentoCard(
          color: const Color(0xFFEEF0F6),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('System Health', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
            const SizedBox(height: 6),
            Text('Run deep diagnostics to ensure the highest fidelity of the paraphrasing engine.',
                style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant, height: 1.5)),
            const SizedBox(height: 14),
            OutlinedButton.icon(
              onPressed: () {},
              icon: const Icon(Icons.monitor_heart_rounded, size: 16),
              label: const Text('Run Diagnostics'),
              style: OutlinedButton.styleFrom(
                foregroundColor: cs.primary,
                side: BorderSide(color: cs.primary.withOpacity(0.4)),
                shape: const StadiumBorder(),
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 12.5),
              ),
            ),
          ]),
        ),
        const SizedBox(height: 100),
      ],
    );
  }
}

// ═══════════════════════════════════════
//  INSTALL PAGE
// ═══════════════════════════════════════

class InstallPage extends StatefulWidget {
  const InstallPage({super.key});
  @override
  State<InstallPage> createState() => _InstallPageState();
}

class _InstallPageState extends State<InstallPage> {
  final List<String> _logs = ['> System ready...'];
  bool _running = false;

  void _log(String msg) {
    setState(() => _logs.add('[${DateTime.now().toLocal().toString().substring(11, 19)}] $msg'));
  }

  Future<void> _runInstall() async {
    if (_running) return;
    setState(() => _running = true);
    try {
      await for (final line in runFullInstall()) {
        _log(line);
      }
    } catch (e) {
      _log('ERROR: $e');
    }
    setState(() => _running = false);
  }

  Future<void> _runUninstall() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Konfirmasi Hapus'),
        content: const Text('Hapus Add-in Parafrase Gandi dari Microsoft Word?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Batal')),
          TextButton(onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Hapus', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirmed != true) return;
    setState(() => _running = true);
    try {
      await for (final line in runUninstall()) {
        _log(line);
      }
    } catch (e) {
      _log('ERROR: $e');
    }
    setState(() => _running = false);
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 0),
      children: [
        // Title
        BentoCard(
          color: const Color(0xFFEEF0F6),
          padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 18),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('Deployment', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
            const SizedBox(height: 6),
            Text('Seamlessly integrate the engine into your Word environment.',
                style: TextStyle(fontSize: 12.5, color: cs.onSurfaceVariant)),
          ]),
        ),
        const SizedBox(height: 14),

        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          // Activation card
          Expanded(
            flex: 57,
            child: BentoCard(
              color: Colors.white,
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Row(children: [
                  Container(width: 36, height: 36, decoration: BoxDecoration(
                      color: const Color(0xFFD8E2FF), borderRadius: BorderRadius.circular(10)),
                      child: Icon(Icons.power_settings_new_rounded, color: cs.primary, size: 20)),
                  const SizedBox(width: 12),
                  const Text('Activation Manager', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13.5)),
                ]),
                const SizedBox(height: 16),
                _step('1', 'Close Microsoft Word before proceeding.'),
                const SizedBox(height: 8),
                _step('2', 'Click "Deploy Engine" to install.'),
                const SizedBox(height: 8),
                _step('3', 'Authorize the UAC prompt if it appears.'),
                const SizedBox(height: 20),
                SizedBox(
                  width: double.infinity,
                  height: 46,
                  child: GradientButton(
                    label: _running ? 'Deploying…' : 'DEPLOY ENGINE',
                    icon: Icons.rocket_launch_rounded,
                    onTap: _running ? null : _runInstall,
                  ),
                ),
                const SizedBox(height: 10),
                SizedBox(
                  width: double.infinity,
                  height: 38,
                  child: OutlinedButton.icon(
                    onPressed: _runUninstall,
                    icon: const Icon(Icons.delete_outline_rounded, size: 16),
                    label: const Text('Undeploy Engine'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: Colors.red,
                      side: const BorderSide(color: Color(0xFFBA1A1A), width: 0.8),
                      shape: const StadiumBorder(),
                      textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 12),
                    ),
                  ),
                ),
              ]),
            ),
          ),
          const SizedBox(width: 14),
          // Log terminal
          Expanded(
            flex: 43,
            child: Container(
              height: 336,
              decoration: BoxDecoration(
                color: const Color(0xFF16181D),
                borderRadius: BorderRadius.circular(20),
                boxShadow: const [BoxShadow(color: Colors.black26, blurRadius: 12, offset: Offset(0, 4))],
              ),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(14, 12, 14, 4),
                  child: Row(children: [
                    const CircleAvatar(radius: 5, backgroundColor: Color(0xFF4CAF50)),
                    const SizedBox(width: 6),
                    Text('PROCESS LOG',
                        style: TextStyle(color: Colors.green[300], fontSize: 9.5, fontFamily: 'Consolas', letterSpacing: 1.5)),
                  ]),
                ),
                const Divider(color: Color(0xFF2A2D35), height: 1),
                Expanded(
                  child: ListView.builder(
                    padding: const EdgeInsets.fromLTRB(14, 8, 14, 8),
                    itemCount: _logs.length,
                    itemBuilder: (ctx, i) => Text(_logs[i],
                        style: const TextStyle(fontSize: 11, color: Color(0xFFADC6FF), fontFamily: 'Consolas', height: 1.6)),
                  ),
                ),
              ]),
            ),
          ),
        ]),
        const SizedBox(height: 100),
      ],
    );
  }

  Widget _step(String num, String text) {
    return Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Container(width: 22, height: 22, decoration: BoxDecoration(
          color: const Color(0xFFD8E2FF), borderRadius: BorderRadius.circular(11)),
          child: Center(child: Text(num, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: Color(0xFF0058BC))))),
      const SizedBox(width: 10),
      Expanded(child: Text(text, style: TextStyle(fontSize: 12, color: Theme.of(context).colorScheme.onSurfaceVariant, height: 1.5))),
    ]);
  }
}

// ═══════════════════════════════════════
//  UPDATE PAGE
// ═══════════════════════════════════════

class UpdatePage extends StatelessWidget {
  const UpdatePage({super.key});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 0),
      children: [
        BentoCard(
          color: Colors.white,
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Icon(Icons.system_update_rounded, color: cs.primary, size: 36),
            const SizedBox(height: 14),
            const Text('AI Version Control', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
            const SizedBox(height: 10),
            Text('Keep your engine synchronized with the latest linguistic improvements and bypass logic for highest quality paraphrasing.',
                style: TextStyle(fontSize: 13, color: cs.onSurfaceVariant, height: 1.6)),
            const SizedBox(height: 20),
            Row(children: [
              Expanded(
                child: GradientButton(
                  label: 'Check for Updates',
                  icon: Icons.refresh_rounded,
                  onTap: () async {
                    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Mengecek pembaruan...')));
                    final result = await runCheckUpdate();
                    if (!context.mounted) return;
                    if (result.isLatest) {
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(result.message)));
                    } else {
                      final doUpdate = await showDialog<bool>(
                        context: context,
                        builder: (ctx) => AlertDialog(
                          title: const Text('Update Tersedia'),
                          content: Text('${result.message}\nUnduh manifest terbaru?'),
                          actions: [
                            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Nanti')),
                            TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Update')),
                          ],
                        ),
                      );
                      if (doUpdate == true && context.mounted) {
                        final msg = await downloadLatestManifest();
                        if (context.mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
                      }
                    }
                  },
                ),
              ),
              const SizedBox(width: 12),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                decoration: BoxDecoration(color: const Color(0xFFDCFCE7), borderRadius: BorderRadius.circular(20)),
                child: const Text('\u2713 v2.4.0  STABLE',
                    style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: Color(0xFF15803D))),
              ),
            ]),
          ]),
        ),
        const SizedBox(height: 14),
        BentoCard(
          color: const Color(0xFFEEF0F6),
          padding: const EdgeInsets.all(16),
          child: Row(children: [
            Container(width: 40, height: 40, decoration: BoxDecoration(
                color: Colors.white, borderRadius: BorderRadius.circular(12)),
                child: Icon(Icons.wifi_tethering_rounded, color: cs.primary, size: 22)),
            const SizedBox(width: 14),
            Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              const Text('Status System', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
              const SizedBox(height: 3),
              Text('Last checked: just now', style: TextStyle(fontSize: 11.5, color: cs.onSurfaceVariant)),
            ])),
            Text('Latest', style: TextStyle(fontWeight: FontWeight.w700, color: cs.primary, fontSize: 12)),
          ]),
        ),
        const SizedBox(height: 100),
      ],
    );
  }
}

// ═══════════════════════════════════════
//  DIAGNOSA PAGE
// ═══════════════════════════════════════

class DiagnosaPage extends StatelessWidget {
  const DiagnosaPage({super.key});

  // Removed _checkServer: replaced by backend_service.checkServerConnection()

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final items = [
      {'icon': Icons.article_rounded,   'title': 'Manifest File',  'sub': 'SYSTEM INTEGRITY', 'status': 'Installed',  'ok': true},
      {'icon': Icons.app_registration, 'title': 'Word Registry',  'sub': 'LINGUISTIC DATA',  'status': 'Connected',  'ok': true},
      {'icon': Icons.cloud_done_rounded,'title': 'Server',         'sub': 'CLOUD SYNC',      'status': 'Stable',     'ok': true},
    ];

    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 0),
      children: [
        BentoCard(
          color: const Color(0xFFEEF0F6),
          padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 18),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('Optimization', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
            const SizedBox(height: 6),
            Text('Registry health and stability tracking.', style: TextStyle(fontSize: 12.5, color: cs.onSurfaceVariant)),
          ]),
        ),
        const SizedBox(height: 14),

        SizedBox(
          width: double.infinity,
          height: 54,
          child: GradientButton(
            label: 'REPAIR & OPTIMIZE',
            icon: Icons.build_rounded,
            fontSize: 15,
            height: 54,
            onTap: () async {
              final sm = ScaffoldMessenger.of(context);
              sm.showSnackBar(const SnackBar(content: Text('Memulai repair...')));
              await for (final _ in runFixRegistry()) {}
              if (context.mounted) sm.showSnackBar(const SnackBar(content: Text('Repair & Optimize selesai!')));
            },
          ),
        ),
        const SizedBox(height: 12),

        SizedBox(
          height: 46,
          child: OutlinedButton.icon(
            onPressed: () async {
                final ok = await checkServerConnection();
                if (!context.mounted) return;
                ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                  content: Text(ok ? 'Server terhubung!' : 'Gagal terhubung ke server.'),
                  backgroundColor: ok ? Colors.green : Colors.red,
                ));
              },
            icon: const Icon(Icons.wifi_tethering_rounded, size: 17),
            label: const Text('Check Server Connection'),
            style: OutlinedButton.styleFrom(
              foregroundColor: cs.primary,
              backgroundColor: const Color(0xFFEEF0F6),
              side: BorderSide(color: cs.primary.withOpacity(0.35)),
              shape: const StadiumBorder(),
              textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
            ),
          ),
        ),
        const SizedBox(height: 22),

        ...items.map((item) => Padding(
          padding: const EdgeInsets.only(bottom: 12),
          child: BentoCard(
            color: Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
            child: Row(children: [
              Container(width: 42, height: 42, decoration: BoxDecoration(
                  color: const Color(0xFFDCFCE7), borderRadius: BorderRadius.circular(12)),
                  child: Icon(item['icon'] as IconData, color: const Color(0xFF15803D), size: 22)),
              const SizedBox(width: 14),
              Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(item['title'] as String, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
                const SizedBox(height: 3),
                Text(item['sub'] as String, style: TextStyle(fontSize: 10.5, color: cs.onSurfaceVariant, letterSpacing: 0.5)),
              ])),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(color: const Color(0xFFDCFCE7), borderRadius: BorderRadius.circular(20)),
                child: Row(mainAxisSize: MainAxisSize.min, children: [
                  const Icon(Icons.check_circle_rounded, color: Color(0xFF15803D), size: 13),
                  const SizedBox(width: 4),
                  Text(item['status'] as String, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: Color(0xFF15803D))),
                ]),
              ),
            ]),
          ),
        )),
        const SizedBox(height: 100),
      ],
    );
  }
}
