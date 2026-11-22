using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace TamgaApp
{
    /// <summary>
    /// SQLite veritabanı bağlantılarını yöneten, dosya yollarını belirleyen ve ilk kurulumları gerçekleştiren ana yardımcı sınıftır.
    /// </summary>
    public static class DbHelper
    {
        #region 📂 BAĞLANTI VE DOSYA YOLLARI (APPDATA ZIRHLI)

        private const string DbFileName = "TamgaApp_Data.db";

        /// <summary>Veritabanının Windows güncellemelerinden etkilenmeyeceği Güvenli AppData Klasörü</summary>
        public static string DataFolder
        {
            get
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "Veritabani");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>Veritabanının tam fiziksel dosya yolu (AppData Klasörü + Dosya Adı)</summary>
        public static string DbPath => Path.Combine(DataFolder, DbFileName);

        /// <summary>SQLite motorunun veritabanına erişmek için kullanacağı bağlantı dizesi</summary>
        public static string ConnectionString => $"Data Source={DbPath}";

        #endregion

        // ==============================================================================================

        #region 🛠️ VERİTABANI KURULUMU VE GÜNCELLEME MOTORU

        /// <summary>
        /// Program ilk çalıştığında veritabanı dosyasının ve gerekli tabloların var olup olmadığını kontrol eder. 
        /// Yoksa kurulum klasöründen kopyalar veya sıfırdan oluşturur, eksik sütunları (Örn: Renk) otomatik ekler.
        /// </summary>
        private static readonly object kurulumKilidi = new object();
        private static bool kurulumTamam;

        public static void EnsureDatabase()
        {
            // Şema kontrolü uygulama ömründe bir kez yapılır. (Eskiden her sorguda CREATE + 4 ALTER
            // çalışıyor ve her ALTER istisna fırlatıyordu; barkod döngülerinde ciddi yavaşlık yaratıyordu.)
            // Dosya silinirse/geri yükleme yapılırsa kontrol yeniden çalışır.
            if (kurulumTamam && File.Exists(DbPath)) return;
            lock (kurulumKilidi)
            {
                if (kurulumTamam && File.Exists(DbPath)) return;
                SemayiKur();
                kurulumTamam = true;
            }
        }

        private static void SemayiKur()
        {
            // 🛡️ ADIM 1: GÜVENLİ BÖLGEYE (APPDATA) TAŞIMA ZIRHI
            // Eğer AppData'da veritabanı yoksa, programın Data klasöründeki orijinal, dolu veritabanını kopyala
            if (!File.Exists(DbPath))
            {
                string kurulumDbYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", DbFileName);
                if (File.Exists(kurulumDbYolu))
                {
                    File.Copy(kurulumDbYolu, DbPath);
                }
            }

            // 🛡️ ADIM 2: EKSİK TABLO VE SÜTUNLARI OTOMATİK TAMAMLAMA
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Firmalar (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FirmaAdi TEXT NOT NULL,
                            Adres TEXT,
                            Il TEXT,
                            Telefon1 TEXT,
                            Telefon2 TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Urunler (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UrunKodu TEXT,
                            Aciklama TEXT,
                            IngilizceAciklama TEXT,
                            Barkod TEXT,
                            Renk TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Kullanicilar (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            KullaniciAdi TEXT UNIQUE NOT NULL,
                            SifreHash TEXT NOT NULL,
                            Yetkiler TEXT
                        );

                        CREATE TABLE IF NOT EXISTS IslemGunlugu (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Tarih TEXT NOT NULL,
                            Kullanici TEXT,
                            Bilgisayar TEXT,
                            Modul TEXT,
                            Islem TEXT,
                            Detay TEXT
                        );
                        CREATE INDEX IF NOT EXISTS IX_IslemGunlugu_Tarih ON IslemGunlugu (Tarih);
                        CREATE INDEX IF NOT EXISTS IX_Urunler_Barkod ON Urunler (Barkod);";

                    cmd.ExecuteNonQuery();

                    // 🔄 GÜNCELLEME MOTORU: Eski tablolara yeni eklenen sütunlar yalnızca eksikse eklenir.
                    SutunYoksaEkle(conn, "Kullanicilar", "BitisTarihi", "DATETIME");
                    SutunYoksaEkle(conn, "Kullanicilar", "SonSifreDegistirme", "DATETIME");
                    SutunYoksaEkle(conn, "Kullanicilar", "SifreGecerlilikAyi", "INTEGER DEFAULT 0");
                    SutunYoksaEkle(conn, "Urunler", "Renk", "TEXT DEFAULT ''");
                }
            }
        }

        private static void SutunYoksaEkle(SqliteConnection conn, string tablo, string sutun, string tanim)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({tablo});";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        if (string.Equals(r.GetString(1), sutun, StringComparison.OrdinalIgnoreCase)) return;
                cmd.CommandText = $"ALTER TABLE {tablo} ADD COLUMN {sutun} {tanim};";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Geri yükleme gibi veritabanı dosyasının değiştiği durumlarda şema kontrolünü yeniden tetikler.</summary>
        public static void SemaKontrolunuSifirla() { kurulumTamam = false; }

        #endregion

        // ==============================================================================================

        #region 🔌 BAĞLANTI SAĞLAYICI (PROVIDER)

        /// <summary>
        /// Veritabanı (DataAccess) işlemleri için kullanılacak hazır ve yapılandırılmış bir SQLite bağlantısı döndürür.
        /// </summary>
        public static SqliteConnection GetConnection()
        {
            // Her bağlantı açıldığında tabloların ve sütunların sağlam olduğundan emin ol
            EnsureDatabase();
            return new SqliteConnection(ConnectionString);
        }

        #endregion
    }
}