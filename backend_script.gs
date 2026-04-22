/**
 * PARAFRASE GANDI BACKEND V3.0
 * Fitur: Validasi Lisensi & Cloud Key Sync
 */

var SHEET_NAME = "Licenses";

function doGet(e) {
  var action = e.parameter.action;
  var licenseKey = e.parameter.licenseKey;
  var deviceId = e.parameter.deviceId;
  
  if (action === "getKeys") {
    return handleGetKeys(licenseKey);
  }
  
  // Default validation
  return handleVerify(licenseKey, deviceId);
}

function doPost(e) {
  var body = JSON.parse(e.postData.contents);
  var action = body.action;
  
  if (action === "setKeys") {
    return handleSetKeys(body.licenseKey, body.keys);
  }
  
  return jsonResponse({success: false, message: "Invalid Action"});
}

function handleVerify(licenseKey, deviceId) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(SHEET_NAME);
  var data = sheet.getDataRange().getValues();
  
  for (var i = 1; i < data.length; i++) {
    if (data[i][0] == licenseKey) {
      if (!data[i][1] || data[i][1] == "" || data[i][1] == deviceId) {
        if (!data[i][1]) sheet.getRange(i + 1, 2).setValue(deviceId);
        return jsonResponse({success: true, message: "Lisensi Valid."});
      } else {
        return jsonResponse({success: false, message: "Lisensi sudah digunakan di perangkat lain."});
      }
    }
  }
  return jsonResponse({success: false, message: "Lisensi tidak ditemukan."});
}

function handleGetKeys(licenseKey) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(SHEET_NAME);
  var data = sheet.getDataRange().getValues();
  
  for (var i = 1; i < data.length; i++) {
    if (data[i][0] == licenseKey) {
      var keys = data[i][2] ? JSON.parse(data[i][2]) : {};
      return jsonResponse({success: true, keys: keys});
    }
  }
  return jsonResponse({success: false, message: "Lisensi tidak ditemukan."});
}

function handleSetKeys(licenseKey, keys) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(SHEET_NAME);
  var data = sheet.getDataRange().getValues();
  
  for (var i = 1; i < data.length; i++) {
    if (data[i][0] == licenseKey) {
      sheet.getRange(i + 1, 3).setValue(JSON.stringify(keys));
      return jsonResponse({success: true, message: "Keys updated successfully."});
    }
  }
  return jsonResponse({success: false, message: "Lisensi tidak ditemukan."});
}

function jsonResponse(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj))
    .setMimeType(ContentService.MimeType.JSON);
}
