using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace TamgaApp
{
    /// <summary>
    /// Uygulamanın veritabanı (SQLite) ile haberleştiği; ekleme, silme, okuma ve güncelleme (CRUD) operasyonlarını yürüten ana motordur.
    /// </summary>
    public static class DataAccess
    {
        #region 🏢 1. FİRMA VERİTABANI İŞLEMLERİ

        /// <summary>Yeni bir firmayı veritabanına ekler.</summary>
        public static void InsertFirma(Firma f)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Firmalar (FirmaAdi, Adres, Il, Telefon1, Telefon2) 
                                        VALUES (@fa, @ad, @il, @t1, @t2);";
                    cmd.Parameters.AddWithValue("@fa", f.FirmaAdi ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ad", f.Adres ?? string.Empty);
                    cmd.Parameters.AddWithValue("@il", f.Il ?? string.Empty);
                    cmd.Parameters.AddWithValue("@t1", f.Telefon1 ?? string.Empty);
                    cmd.Parameters.AddWithValue("@t2", f.Telefon2 ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Mevcut bir firmanın bilgilerini ID'sine göre günceller.</summary>
        public static void UpdateFirma(Firma f)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE Firmalar 
                                        SET FirmaAdi=@fa, Adres=@ad, Il=@il, Telefon1=@t1, Telefon2=@t2 
                                        WHERE Id=@id;";
                    cmd.Parameters.AddWithValue("@fa", f.FirmaAdi ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ad", f.Adres ?? string.Empty);
                    cmd.Parameters.AddWithValue("@il", f.Il ?? string.Empty);
                    cmd.Parameters.AddWithValue("@t1", f.Telefon1 ?? string.Empty);
                    cmd.Parameters.AddWithValue("@t2", f.Telefon2 ?? string.Empty);
                    cmd.Parameters.AddWithValue("@id", f.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Veritabanındaki tüm firmaları alfabetik sırayla getirir.</summary>
        public static List<Firma> GetAllFirmalar()
        {
            var list = new List<Firma>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, FirmaAdi, Adres, Il, Telefon1, Telefon2 FROM Firmalar ORDER BY FirmaAdi;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Firma
                            {
                                Id = reader.GetInt32(0),
                                FirmaAdi = reader.GetString(1),
                                Adres = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Il = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Telefon1 = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Telefon2 = reader.IsDBNull(5) ? "" : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>Belirtilen ID'ye sahip firmanın detaylarını getirir.</summary>
        public static Firma GetFirmaById(int id)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, FirmaAdi, Adres, Il, Telefon1, Telefon2 FROM Firmalar WHERE Id=@id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Firma
                            {
                                Id = reader.GetInt32(0),
                                FirmaAdi = reader.GetString(1),
                                Adres = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Il = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Telefon1 = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Telefon2 = reader.IsDBNull(5) ? "" : reader.GetString(5)
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>Seçilen firmayı veritabanından kalıcı olarak siler.</summary>
        public static void DeleteFirma(int id)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Firmalar WHERE Id=@id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Tüm firmaları siler ve ID (Otomatik Artan Sayı) sayacını sıfırlar.</summary>
        public static void DeleteAllFirmalar()
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Firmalar;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name='Firmalar';";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        // =========================================================================================

        #region 📦 2. ÜRETİM TAKİP VE ÜRÜN MOTORLARI

        /// <summary>Barkod okutulduğunda eşleşen ürünü bulup getirir.</summary>
        public static Urun GetUrunByBarkod(string barkod)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, UrunKodu, Aciklama, IngilizceAciklama, Barkod FROM Urunler WHERE Barkod=@barkod;";
                    cmd.Parameters.AddWithValue("@barkod", barkod ?? string.Empty);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Urun
                            {
                                Id = reader.GetInt32(0),
                                UrunKodu = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Aciklama = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                IngilizceAciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Barkod = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>Excel'den aktarırken veya elle girildiğinde yeni ürün ekler.</summary>
        public static void InsertUrun(Urun u)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Urunler (UrunKodu, Aciklama, IngilizceAciklama, Barkod) 
                                        VALUES (@kod, @aciklama, @ing, @barkod);";
                    cmd.Parameters.AddWithValue("@kod", u.UrunKodu ?? string.Empty);
                    cmd.Parameters.AddWithValue("@aciklama", u.Aciklama ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ing", u.IngilizceAciklama ?? string.Empty);
                    cmd.Parameters.AddWithValue("@barkod", u.Barkod ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Mevcut ürünün özelliklerini koduna göre bulup günceller.</summary>
        public static void UpdateUrun(Urun u)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE Urunler 
                                        SET Aciklama=@aciklama, IngilizceAciklama=@ing, Barkod=@barkod 
                                        WHERE UrunKodu=@kod;";
                    cmd.Parameters.AddWithValue("@kod", u.UrunKodu ?? string.Empty);
                    cmd.Parameters.AddWithValue("@aciklama", u.Aciklama ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ing", u.IngilizceAciklama ?? string.Empty);
                    cmd.Parameters.AddWithValue("@barkod", u.Barkod ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Seçilen ürünü siler.</summary>
        public static void DeleteUrun(string urunKodu)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Urunler WHERE UrunKodu=@kod;";
                    cmd.Parameters.AddWithValue("@kod", urunKodu ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Sistemdeki tüm ürünleri listeler.</summary>
        public static List<Urun> GetAllUrunler()
        {
            var list = new List<Urun>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, UrunKodu, Aciklama, IngilizceAciklama, Barkod FROM Urunler;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Urun
                            {
                                Id = reader.GetInt32(0),
                                UrunKodu = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Aciklama = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                IngilizceAciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Barkod = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>Veritabanındaki tüm ürünleri kalıcı olarak siler.</summary>
        public static void DeleteAllUrunler()
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Urunler;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        // =========================================================================================

        #region 👤 3. KULLANICI MODELLERİ VE GÜVENLİK (KRİPTO) MOTORU

        /// <summary>
        /// Sisteme giriş yapacak olan kullanıcıların (Süre ve Yetki Destekli) verilerini taşıyan modeldir.
        /// </summary>
        public class Kullanici
        {
            public int Id { get; set; }
            public string KullaniciAdi { get; set; }
            public string SifreHash { get; set; }
            public string Yetkiler { get; set; }
            public DateTime? BitisTarihi { get; set; } // Null ise süresizdir
            public DateTime SonSifreDegistirme { get; set; }
            public int SifreGecerlilikAyi { get; set; } // 0 ise şifre hiç eskimez
        }

        /// <summary>
        /// Kullanıcı şifrelerini veritabanına kaydederken kırılamaz (SHA256) formata dönüştüren güvenlik kalkanı.
        /// </summary>
        public static class SecurityHelper
        {
            public static string HashPassword(string password)
            {
                using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    System.Text.StringBuilder builder = new System.Text.StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
        }

        #endregion

        // =========================================================================================

        #region 🔐 4. KULLANICI YÖNETİMİ VE GİRİŞ İŞLEMLERİ

        /// <summary>Sisteme süreli/süresiz yeni bir kullanıcı hesabı açar.</summary>
        public static void InsertKullanici(Kullanici k)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Kullanicilar (KullaniciAdi, SifreHash, Yetkiler, BitisTarihi, SonSifreDegistirme, SifreGecerlilikAyi) 
                                        VALUES (@kAdi, @hash, @yetkiler, @bitis, @sonSifre, @gecerlilik);";
                    cmd.Parameters.AddWithValue("@kAdi", k.KullaniciAdi);
                    cmd.Parameters.AddWithValue("@hash", k.SifreHash);
                    cmd.Parameters.AddWithValue("@yetkiler", k.Yetkiler ?? string.Empty);
                    cmd.Parameters.AddWithValue("@bitis", (object)k.BitisTarihi ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@sonSifre", k.SonSifreDegistirme);
                    cmd.Parameters.AddWithValue("@gecerlilik", k.SifreGecerlilikAyi);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Kullanıcı adı ve şifresiyle sisteme giriş izni arar.</summary>
        public static Kullanici GetKullaniciGiris(string kullaniciAdi, string sifre)
        {
            string kriptoluSifre = SecurityHelper.HashPassword(sifre);
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, KullaniciAdi, SifreHash, Yetkiler, BitisTarihi, SonSifreDegistirme, SifreGecerlilikAyi FROM Kullanicilar WHERE KullaniciAdi=@kAdi AND SifreHash=@hash;";
                    cmd.Parameters.AddWithValue("@kAdi", kullaniciAdi);
                    cmd.Parameters.AddWithValue("@hash", kriptoluSifre);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Kullanici
                            {
                                Id = reader.GetInt32(0),
                                KullaniciAdi = reader.GetString(1),
                                SifreHash = reader.GetString(2),
                                Yetkiler = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                BitisTarihi = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                SonSifreDegistirme = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
                                SifreGecerlilikAyi = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>Admin tarafından süresi dolan kullanıcının süresini uzatır.</summary>
        public static void KullaniciSuresiUzat(string kAdi, DateTime yeniBitis)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Kullanicilar SET BitisTarihi = @bitis WHERE KullaniciAdi = @kAdi;";
                    cmd.Parameters.AddWithValue("@bitis", yeniBitis);
                    cmd.Parameters.AddWithValue("@kAdi", kAdi);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Kullanıcının kendi şifresini yenilemesi için kullanılır.</summary>
        public static void KullaniciSifreDegistir(string kAdi, string yeniSifre)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Kullanicilar SET SifreHash = @h, SonSifreDegistirme = @d WHERE KullaniciAdi = @kAdi;";
                    cmd.Parameters.AddWithValue("@h", SecurityHelper.HashPassword(yeniSifre));
                    cmd.Parameters.AddWithValue("@d", DateTime.Now);
                    cmd.Parameters.AddWithValue("@kAdi", kAdi);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Sistemdeki tüm kullanıcıları (şifreleri gizleyerek) listeler.</summary>
        public static List<Kullanici> GetAllKullanicilar()
        {
            var list = new List<Kullanici>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // Güvenlik için SifreHash sütununu ASLA çekmiyoruz!
                    cmd.CommandText = "SELECT Id, KullaniciAdi, Yetkiler, BitisTarihi, SonSifreDegistirme, SifreGecerlilikAyi FROM Kullanicilar;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Kullanici
                            {
                                Id = reader.GetInt32(0),
                                KullaniciAdi = reader.GetString(1),
                                Yetkiler = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                BitisTarihi = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                SonSifreDegistirme = reader.IsDBNull(4) ? DateTime.Now : reader.GetDateTime(4),
                                SifreGecerlilikAyi = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                            });
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>Sistemden bir kullanıcıyı tamamen siler.</summary>
        public static void DeleteKullanici(string kAdi)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Kullanicilar WHERE KullaniciAdi = @kAdi;";
                    cmd.Parameters.AddWithValue("@kAdi", kAdi);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion
    }
}