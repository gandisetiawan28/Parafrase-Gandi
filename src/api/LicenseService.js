/* global localStorage */

/**
 * Fungsi pembantu untuk menyimpan ID ke IndexedDB agar lebih awet dibanding localStorage
 */
const saveToIndexedDB = async (id) => {
    try {
        const request = indexedDB.open("GandiDB", 1);
        request.onupgradeneeded = (e) => {
            const db = e.target.result;
            if (!db.objectStoreNames.contains("settings")) {
                db.createObjectStore("settings");
            }
        };
        request.onsuccess = (e) => {
            const db = e.target.result;
            const transaction = db.transaction("settings", "readwrite");
            transaction.objectStore(settings).put(id, "deviceId");
        };
    } catch (e) {
        console.warn("IndexedDB not supported or failed", e);
    }
};

/**
 * Fungsi pembantu untuk mengambil ID dari IndexedDB
 */
const getFromIndexedDB = () => {
    return new Promise((resolve) => {
        try {
            const request = indexedDB.open("GandiDB", 1);
            request.onsuccess = (e) => {
                const db = e.target.result;
                if (!db.objectStoreNames.contains("settings")) return resolve(null);
                const transaction = db.transaction("settings", "readonly");
                const getReq = transaction.objectStore("settings").get("deviceId");
                getReq.onsuccess = () => resolve(getReq.result);
                getReq.onerror = () => resolve(null);
            };
            request.onerror = () => resolve(null);
        } catch (e) {
            resolve(null);
        }
    });
};

export const getBrowserId = async () => {
    // 1. Coba dari LocalStorage
    let id = localStorage.getItem("gandi_device_id");
    
    // 2. Jika tidak ada, coba dari IndexedDB (Cadangan Utama)
    if (!id) {
        id = await getFromIndexedDB();
    }

    if (!id) {
        // Jika benar-benar baru, buat ID unik (Campuran Random + Fingerprint)
        try {
            const screenInfo = (window.screen.width * window.screen.height).toString(16);
            const hardwareInfo = (navigator.hardwareConcurrency || 4).toString(16);
            const randomSuffix = Math.random().toString(36).substr(2, 4).toUpperCase();
            
            const seed = `${screenInfo}-${hardwareInfo}-${randomSuffix}`;
            const hash = btoa(seed).replace(/=/g, "").replace(/\+/g, "X").replace(/\//g, "Y").toUpperCase();
            id = "CORE-" + hash.substr(0, 12);
        } catch (e) {
            id = "CORE-" + Math.random().toString(36).substr(2, 9).toUpperCase();
        }
        
        // Simpan ke kedua tempat
        localStorage.setItem("gandi_device_id", id);
        saveToIndexedDB(id);
    } else {
        // Pastikan keduanya tersinkron jika salah satu hilang
        localStorage.setItem("gandi_device_id", id);
        saveToIndexedDB(id);
    }
    
    return id;
};

export const getLicenseStatus = () => {
    const key = localStorage.getItem("gandi_license_key");
    const isVerified = localStorage.getItem("gandi_license_verified") === "true";
    return { key: key || "", isVerified };
};

export const saveLicenseKey = (key) => {
    localStorage.setItem("gandi_license_key", key);
    localStorage.setItem("gandi_license_verified", "false");
};

/**
 * Validasi Lisensi ke Google Apps Script (Web App)
 * Super Safe Fetch with GET
 */
export const verifyLicense = async (licenseKey) => {
    const deviceId = await getBrowserId();

    // GANTI TULISAN DI BAWAH INI dengan URL Web App (Deploy) Anda.
    // Encoded for extra security
    const _0x51c2 = "aHR0cHM6Ly9zY3JpcHQuZ29vZ2xlLmNvbS9tYWNyb3Mvcy9BS2Z5Y2J6MjBQVnZ2M19vcU8xUUI0eFR4TzVxOWJtTTNZZm5HMjdTSFkzNmk3ZC10N0V2MnhyYWRSaWd4ZnVOdUJMZVFBYXgvZXhlYw==";
    const GAS_WEB_APP_URL = atob(_0x51c2);

    try {
        if (GAS_WEB_APP_URL.includes("GANTI_DENGAN_URL")) {
            throw new Error("URL Apps Script belum diisi di LicenseService.js");
        }

        const url = `${GAS_WEB_APP_URL}?licenseKey=${encodeURIComponent(licenseKey)}&deviceId=${encodeURIComponent(deviceId)}&_cache=${Date.now()}`;

        console.log("Menghubungi Server Lisensi...");

        const response = await fetch(url, {
            method: "GET",
            mode: "cors",
            headers: {
                "Accept": "application/json"
            },
            redirect: "follow"
        });

        const rawText = await response.text();
        console.log("Respon Mentah:", rawText);

        try {
            const data = JSON.parse(rawText);
            if (data.success) {
                localStorage.setItem("gandi_license_verified", "true");
                return { success: true, message: data.message };
            } else {
                return { success: false, message: data.message || "Lisensi tidak valid." };
            }
        } catch (jsonErr) {
            console.error("Gagal parse JSON. Respon server:", rawText);
            return {
                success: false,
                message: "Format respon server salah. Mohon update kode Apps Script ke V2.0."
            };
        }

    } catch (error) {
        console.error("Network Error:", error);
        return { success: false, message: `Koneksi Gagal: ${error.message}` };
    }
};
