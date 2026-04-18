/* global localStorage */

export const getBrowserId = () => {
    let id = localStorage.getItem("gandi_device_id");
    if (!id) {
        id = "DEV-" + Math.random().toString(36).substr(2, 9).toUpperCase();
        localStorage.setItem("gandi_device_id", id);
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
    const deviceId = getBrowserId();

    // GANTI TULISAN DI BAWAH INI dengan URL Web App (Deploy) Anda.
    const GAS_WEB_APP_URL = "https://script.google.com/macros/s/AKfycbz20PVvv3_oqO1QB4xTxO5q9bmM3YfnG27SHY36i7d-t7Ev2xradRigxfuNuBLeQAax/exec";

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
