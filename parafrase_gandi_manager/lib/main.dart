import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api_service.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Parafrase Gandi Manager',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
        useMaterial3: true,
      ),
      home: const ManagerHomePage(),
    );
  }
}

class ManagerHomePage extends StatefulWidget {
  const ManagerHomePage({super.key});

  @override
  State<ManagerHomePage> createState() => _ManagerHomePageState();
}

class _ManagerHomePageState extends State<ManagerHomePage> {
  String _licenseKey = "";
  bool _isVerified = false;
  
  final TextEditingController _licenseController = TextEditingController();
  final TextEditingController _geminiController = TextEditingController();
  final TextEditingController _openaiController = TextEditingController();
  final TextEditingController _claudeController = TextEditingController();

  String _status = "";
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      _licenseKey = prefs.getString('licenseKey') ?? '';
      _isVerified = prefs.getBool('isVerified') ?? false;
      _licenseController.text = _licenseKey;
      
      _geminiController.text = prefs.getString('geminiKey') ?? '';
      _openaiController.text = prefs.getString('openaiKey') ?? '';
      _claudeController.text = prefs.getString('claudeKey') ?? '';
    });
  }

  Future<void> _verifyLicense() async {
    setState(() {
      _isLoading = true;
      _status = "Memverifikasi...";
    });

    String key = _licenseController.text.trim();
    String deviceId = "DESKTOP-APP-123"; // Dummy device ID for now

    final result = await ApiService.verifyLicense(key, deviceId);

    if (result['success']) {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('licenseKey', key);
      await prefs.setBool('isVerified', true);
      setState(() {
        _licenseKey = key;
        _isVerified = true;
        _status = "Lisensi Valid!";
      });
    } else {
      setState(() {
        _status = result['message'];
      });
    }

    setState(() {
      _isLoading = false;
    });
  }

  Future<void> _syncKeys() async {
    if (!_isVerified) return;

    setState(() {
      _isLoading = true;
      _status = "Menyimpan ke Cloud...";
    });

    Map<String, String> keys = {
      "gemini": _geminiController.text.trim(),
      "openai": _openaiController.text.trim(),
      "claude": _claudeController.text.trim(),
    };

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('geminiKey', keys['gemini']!);
    await prefs.setString('openaiKey', keys['openai']!);
    await prefs.setString('claudeKey', keys['claude']!);

    final result = await ApiService.syncKeysToCloud(_licenseKey, keys);

    setState(() {
      _isLoading = false;
      _status = result['success'] ? "Tersinkronisasi dengan Word Add-in!" : result['message'];
    });
  }

  Future<void> _logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
    setState(() {
      _isVerified = false;
      _licenseKey = "";
      _status = "";
      _licenseController.text = "";
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Parafrase Gandi Manager"),
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        actions: [
          if (_isVerified)
            IconButton(
              icon: const Icon(Icons.logout),
              onPressed: _logout,
              tooltip: "Logout",
            )
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (!_isVerified) ...[
              const Text("Aktivasi Lisensi", style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
              const SizedBox(height: 16),
              TextField(
                controller: _licenseController,
                decoration: const InputDecoration(
                  labelText: "License Key",
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _isLoading ? null : _verifyLicense,
                child: _isLoading ? const CircularProgressIndicator() : const Text("Verifikasi"),
              )
            ] else ...[
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text("Pengaturan API Keys (Otomatis Sync ke Word)", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                      const SizedBox(height: 16),
                      TextField(
                        controller: _geminiController,
                        obscureText: true,
                        decoration: const InputDecoration(labelText: "Google Gemini API Key", border: OutlineInputBorder()),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: _openaiController,
                        obscureText: true,
                        decoration: const InputDecoration(labelText: "OpenAI API Key", border: OutlineInputBorder()),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: _claudeController,
                        obscureText: true,
                        decoration: const InputDecoration(labelText: "Claude API Key", border: OutlineInputBorder()),
                      ),
                      const SizedBox(height: 16),
                      ElevatedButton.icon(
                        icon: const Icon(Icons.cloud_upload),
                        label: const Text("Simpan & Sync"),
                        onPressed: _isLoading ? null : _syncKeys,
                      )
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text("Manajemen Dokumen Jurnal", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                      const Text("Upload PDF/Docx di sini agar dapat digunakan di Word Add-in.", style: TextStyle(color: Colors.grey)),
                      const SizedBox(height: 16),
                      ElevatedButton.icon(
                        icon: const Icon(Icons.upload_file),
                        label: const Text("Pilih Dokumen..."),
                        onPressed: () {
                          // TODO: Implement file picking and extraction locally in Flutter
                          setState(() {
                            _status = "Fitur upload PDF/Docx akan segera diimplementasikan!";
                          });
                        },
                      )
                    ],
                  ),
                ),
              ),
            ],
            const SizedBox(height: 24),
            Text(
              _status,
              style: TextStyle(
                color: _status.contains("Valid") || _status.contains("Tersinkronisasi") ? Colors.green : Colors.red,
                fontWeight: FontWeight.bold
              ),
              textAlign: TextAlign.center,
            )
          ],
        ),
      ),
    );
  }
}
