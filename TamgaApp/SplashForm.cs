using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TamgaApp
{
    public partial class SplashForm : Form
    {
        #region 🔲 Yuvarlak Köşe Ayarları

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        #endregion

        // Sayaçları manuel oluşturuyoruz
        private Timer animasyonSayaci = new Timer();
        private Timer beklemeSayaci = new Timer();

        public SplashForm()
        {
            InitializeComponent();

            // 1️⃣ GÖRSEL AYARLAR
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Opacity = 0; // Sönük başla

            // 2️⃣ KABLOLARI MANUEL BAĞLAMA (Kopyala-yapıştır yaparken kopmasınlar)
            this.Load += SplashForm_Load;
            animasyonSayaci.Tick += AnimasyonSayaci_Tick;
            beklemeSayaci.Tick += BeklemeSayaci_Tick;

            // 3️⃣ HIZ VE SÜRE AYARLARI
            animasyonSayaci.Interval = 30;   // Animasyon akıcılığı
            beklemeSayaci.Interval = 3000;   // 3 saniye ekranda kalma süresi
        }

        private void SplashForm_Load(object sender, EventArgs e)
        {
            // Yuvarlak köşeleri form ekrana gelirken kendi boyutuna göre kesiyoruz
            this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 40, 40));

            // Animasyon motorunu ateşle
            animasyonSayaci.Start();
        }

        private void AnimasyonSayaci_Tick(object sender, EventArgs e)
        {
            this.Opacity += 0.05; // Şeffaflığı yavaş yavaş artır

            // Tamamen aydınlandığında (%100 görünürlük)
            if (this.Opacity >= 1.0)
            {
                animasyonSayaci.Stop(); // Parlamayı durdur
                beklemeSayaci.Start();  // 3 saniyelik geri sayımı başlat
            }
        }

        private void BeklemeSayaci_Tick(object sender, EventArgs e)
        {
            beklemeSayaci.Stop(); // Süre bitti, sayacı durdur

            // 💣 ZORLA KAPATMA: ShowDialog ile açılan formları this.Close() bazen kapatmaz.
            // DialogResult.OK vererek sisteme formun işinin bittiğini zorla kabul ettiriyoruz!
            this.DialogResult = DialogResult.OK;
        }
    }
}