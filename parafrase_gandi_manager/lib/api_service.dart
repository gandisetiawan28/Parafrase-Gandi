import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiService {
  // GANTI DENGAN URL DEPLOY GOOGLE APPS SCRIPT ANDA (V3.0)
  static const String gasUrl = "https://script.google.com/macros/s/AKfycbycbz20PVvv3_oqO1QB4xTxO5q9bmM3YfnG27SHY36i7d-t7Ev2xradRigxfuNuBLeQAax/exec"; // Placeholder

  static Future<Map<String, dynamic>> verifyLicense(String licenseKey, String deviceId) async {
    try {
      final url = Uri.parse("$gasUrl?action=verify&licenseKey=$licenseKey&deviceId=$deviceId");
      final response = await http.get(url);

      if (response.statusCode == 200) {
        return jsonDecode(response.body);
      }
      return {"success": false, "message": "Server error: ${response.statusCode}"};
    } catch (e) {
      return {"success": false, "message": "Connection error: $e"};
    }
  }

  static Future<Map<String, dynamic>> syncKeysToCloud(String licenseKey, Map<String, String> keys) async {
    try {
      final url = Uri.parse(gasUrl);
      final response = await http.post(
        url,
        headers: {"Content-Type": "application/json"},
        body: jsonEncode({
          "action": "setKeys",
          "licenseKey": licenseKey,
          "keys": keys
        })
      );

      if (response.statusCode == 200 || response.statusCode == 302) {
        // Apps Script may return 302 redirect on POST, http package usually follows it.
        try {
          return jsonDecode(response.body);
        } catch (e) {
           return {"success": true, "message": "Keys synced (ignoring redirect body)"};
        }
      }
      return {"success": false, "message": "Server error: ${response.statusCode}"};
    } catch (e) {
      return {"success": false, "message": "Connection error: $e"};
    }
  }
}
