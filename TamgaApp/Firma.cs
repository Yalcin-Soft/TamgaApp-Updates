namespace TamgaApp
{
    #region 🏢 FİRMA VERİ MODELİ
    /// <summary>
    /// Sistemde kayıtlı olan müşteri/firma bilgilerini taşımak için kullanılan temel veri modelidir.
    /// </summary>
    public class Firma
    {
        /// <summary>Veritabanındaki benzersiz kimlik numarası (Otomatik artar)</summary>
        public int Id { get; set; }

        /// <summary>Firmanın resmi veya ticari adı</summary>
        public string FirmaAdi { get; set; }

        /// <summary>Firmanın açık adresi</summary>
        public string Adres { get; set; }

        /// <summary>Firmanın bulunduğu il veya ilçe</summary>
        public string Il { get; set; }

        /// <summary>Birincil iletişim numarası veya ilgili kişi</summary>
        public string Telefon1 { get; set; }

        /// <summary>İkincil iletişim numarası veya alternatif kişi</summary>
        public string Telefon2 { get; set; }
    }
    #endregion

    // ==============================================================================================

    #region 📦 ÜRÜN VERİ MODELİ
    /// <summary>
    /// Üretim, etiket ve stok işlemleri için kullanılan ürün bilgilerini taşıyan veri modelidir.
    /// </summary>
    public class Urun
    {
        /// <summary>Veritabanındaki benzersiz kimlik numarası (Otomatik artar)</summary>
        public int Id { get; set; }

        /// <summary>Ürüne ait benzersiz stok/sistem kodu</summary>
        public string UrunKodu { get; set; }

        /// <summary>Ürünün Türkçe veya genel açıklaması</summary>
        public string Aciklama { get; set; }

        /// <summary>İhracat veya yabancı etiketler için ürünün İngilizce açıklaması</summary>
        public string IngilizceAciklama { get; set; }

        /// <summary>Barkod okuyucular için tanımlanmış sayısal/alfanumerik barkod verisi</summary>
        public string Barkod { get; set; }
    }
    #endregion

    // ==============================================================================================

    #region 💡 GELİŞTİRİCİ NOTLARI & REFERANSLAR
    /* 
     * NOT: Aşağıdaki CRUD (Oluşturma, Okuma, Güncelleme, Silme) operasyonlarının 
     * 'DataAccess' sınıfı içerisine eklendiği varsayılmıştır:
     * 
     * 1. DataAccess.GetUrunByBarkod(string barkod);       -> Barkod ile ürün arar
     * 2. DataAccess.InsertUrun(Urun u);                   -> Veritabanına yeni ürün ekler
     * 3. DataAccess.DeleteUrun(string barkodVeyaKodu);    -> Sistemden ürün siler
     * 4. DataAccess.UpdateUrun(Urun u);                   -> Mevcut ürünün bilgilerini günceller
     */
    #endregion
}