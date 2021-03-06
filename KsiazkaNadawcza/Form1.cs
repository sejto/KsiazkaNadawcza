﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using WindowsFormsApp1;

namespace KsiazkaNadawcza
{
    public partial class Ksiazka_Nadawcza : Form
    {
        public static List<Kontrahent> KontrahentLista = new List<Kontrahent>();
        
        public Ksiazka_Nadawcza()
        {
            InitializeComponent();
            CreateDGV();
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            DrukujKoperty();
        } //Drukuj koperty
        private void PokazBtn_Click(object sender, EventArgs e)
        {
            string data = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");
            /* string sql = "select Nazwa, Data, NrDok,  Ulica, CASE WHEN (NrLokalu IS NULL or NrLokalu='')THEN NrDomu ELSE NrDomu+'/'+NrLokalu END as NrDomu, Kod, Miasto from OTD.dbo.Dok D " +
            "inner join OTD.dbo.dokkontr DK on dk.dokid = D.dokid inner join OTD.dbo.Kontrahent K on DK.kontrid = K.kontrid " +
            "where typdok = 33 and d.aktywny = 1 and data = '"+data+"'"; */
            string sql = "SELECT t.nazwa,t.data, STUFF((SELECT ',' + s.nrdok FROM OTD.dbo.Faktury s " +
            "WHERE s.nazwa = t.nazwa and data = '" + data + "' FOR XML PATH('')),1,1,'') AS CSV, Ulica, NrDomu, Kod, Miasto " +
            "FROM OTD.dbo.Faktury AS t where data = '" + data + "' GROUP BY t.nazwa, t.data, t.ulica, t.nrdomu, t.kod,t.miasto";
            PokazDokumenty(sql);
            PoliczZaznaczone();
        } //Pokaz dokumenty
        private void Button3_Click(object sender, EventArgs e)
        {
            string nazwa = textBox6.Text;
            string ulica = textBox5.Text;
            string nrdomu = textBox4.Text;
            string kod = textBox3.Text;
            string miasto = textBox2.Text;
            string faktura = textBox1.Text;
            dataGridView2.Rows.Add(true,nazwa, ulica, nrdomu, kod, miasto," ",faktura);
            dataGridView2.FirstDisplayedScrollingRowIndex = dataGridView2.RowCount - 1;
            PoliczZaznaczone();
        } //button Dodaj
        private void SzukajBtn_Click(object sender, EventArgs e)
        {
            SzukajKTH();
        }
        private void NrFakturyBtn_Click(object sender, EventArgs e)
        {
            string nrfaktury = nrFakturyTxt.Text;
            ZnajdzFakture(nrfaktury);
            PoliczZaznaczone();
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            DrukujKsiazke();
        } //DrukujKsiazke

        private void DataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            PoliczZaznaczone();
        } 
        void PoliczZaznaczone()
        {
            if ((dataGridView2.CurrentCell as DataGridViewCheckBoxCell) != null)
            {
                int num = 0;
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    bool isChecked = (bool)row.Cells[0].EditedFormattedValue;
                    if (isChecked)
                    {
                        num++;
                    }
                }
                PozycjeLbl.Text = "Zaznaczonych pozycji: " + num;
            }
        }
        private void Zaznacz_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (Convert.ToBoolean(row.Cells[0].Value) != true)
                    row.Cells[0].Value = true;
                else
                    row.Cells[0].Value = false;
            }
        }
        
        void CreateDGV()
        {
            dataGridView2.RowHeadersVisible = false;
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.Columns.Add("Column", "Nazwa");
            dataGridView2.Columns[0].Width = 300;
            dataGridView2.Columns.Add("Column", "Ulica");
            dataGridView2.Columns[1].Width = 120;
            dataGridView2.Columns.Add("Column", "NrDomu/lokalu");
            dataGridView2.Columns[2].Width = 60;
            dataGridView2.Columns.Add("Column", "KodPocztowy");
            dataGridView2.Columns[3].Width = 60;
            dataGridView2.Columns.Add("Column", "Miasto");
            dataGridView2.Columns[4].Width = 80;
            dataGridView2.Columns.Add("Column", "Inny adres");
            dataGridView2.Columns[5].Width = 120;
            dataGridView2.Columns.Add("Column", "Nr faktury");
            dataGridView2.Columns[6].Width = 300;
            DataGridViewCheckBoxColumn col = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Wybierz"
            };
            dataGridView2.Columns.Insert(0, col);
            DataGridViewColumn columnID = dataGridView2.Columns[0];
            col.Width = 50;
        }
        void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["Wybor"].Index && e.RowIndex >= 0)
            {
                if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "Wybierz")
                {
                    int rownumber = Convert.ToInt16(((DataGridView)sender).SelectedCells[0].RowIndex);
                    string nazwa = dataGridView1[0, rownumber].Value.ToString();
                    string ulica = dataGridView1[1, rownumber].Value.ToString();
                    string nrdomu = dataGridView1[2, rownumber].Value.ToString();
                    string kod = dataGridView1[3, rownumber].Value.ToString();
                    string miasto = dataGridView1[4, rownumber].Value.ToString();
                    dataGridView2.Rows.Add(true,nazwa, ulica, nrdomu, kod, miasto," ");
                    dataGridView2.FirstDisplayedScrollingRowIndex = dataGridView2.RowCount - 1;
                }
            }
            PoliczZaznaczone();
        }
        void SzukajKTH()
        {
            dataGridView1.Columns.Clear();
            string nazwa = txt_KTH.Text;
            string sql;
            if (NIPValidate(nazwa) != true)
            {
                sql = "select Nazwa, Ulica,CASE WHEN (NrLokalu IS NULL or NrLokalu='')THEN NrDomu ELSE NrDomu+'/'+NrLokalu END as NrDomu, kod, miasto from OTD.dbo.kontrahent where nazwa like '%" + nazwa + "%'";
            }
            else
            {
                sql = "select Nazwa, Ulica,CASE WHEN (NrLokalu IS NULL or NrLokalu='')THEN NrDomu ELSE NrDomu+'/'+NrLokalu END as NrDomu, kod, miasto from OTD.dbo.kontrahent where nip ='" + nazwa + "'";
            }
            DataSet ds = new DataSet();
            string keyname = "HKEY_CURRENT_USER\\MARKET\\ListPrzewozowy";
            RejestrIO rejestr = new RejestrIO();
            string klucz = rejestr.CzytajKlucz(keyname, "SQLconnect", true); 
            var conn = new SqlConnection(klucz);
            SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
            ds.Tables.Add("list");
            adapter.Fill(ds, "list");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "list";
            DataGridViewButtonColumn col = new DataGridViewButtonColumn
            {
                UseColumnTextForButtonValue = true,
                Text = "Wybierz",
                Name = "Wybor"
            };
            dataGridView1.Columns.Add(col);
            DataGridViewColumn columnnazwa = dataGridView1.Columns[0];
            columnnazwa.Width = 300;
        }
        void PokazDokumenty(string sql)
        {
            dataGridView2.Columns.Clear();
            CreateDGV();
            string keyname = "HKEY_CURRENT_USER\\MARKET\\ListPrzewozowy";
            RejestrIO rejestr = new RejestrIO();
            string klucz = rejestr.CzytajKlucz(keyname, "SQLconnect", true);
            var conn = new SqlConnection(klucz);
            string data = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");

            SqlDataAdapter adp = new SqlDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            adp.Fill(ds);
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            { 
                string nazwa = ds.Tables[0].Rows[i][0].ToString();
                string ulica = ds.Tables[0].Rows[i][3].ToString();
                string nrdomu = ds.Tables[0].Rows[i][4].ToString();
                string kod = ds.Tables[0].Rows[i][5].ToString();
                string miasto = ds.Tables[0].Rows[i][6].ToString();
                string faktura = ds.Tables[0].Rows[i][2].ToString();
                dataGridView2.Rows.Add(true,nazwa, ulica, nrdomu, kod, miasto," ",faktura);
            }
            DataGridViewColumn columnNazwa = dataGridView2.Columns[1];
            DataGridViewColumn columnID = dataGridView2.Columns[0];
            columnNazwa.Width = 250;
            if (dataGridView2.RowCount != 0)
             dataGridView2.FirstDisplayedScrollingRowIndex = dataGridView2.RowCount - 1; 
        } 
        
        static public bool NIPValidate(string NIPValidate)
        {
            const byte lenght = 10;

            ulong nip = ulong.MinValue;
            byte[] digits;
            byte[] weights = new byte[] { 6, 5, 7, 2, 3, 4, 5, 6, 7 };

            if (NIPValidate.Length.Equals(lenght).Equals(false)) return false;

            if (ulong.TryParse(NIPValidate, out nip).Equals(false)) return false;
            else
            {
                string sNIP = NIPValidate.ToString();
                digits = new byte[lenght];

                for (int i = 0; i < lenght; i++)
                {
                    if (byte.TryParse(sNIP[i].ToString(), out digits[i]).Equals(false)) return false;
                }

                int checksum = 0;

                for (int i = 0; i < lenght - 1; i++)
                {
                    checksum += digits[i] * weights[i];
                }

                return (checksum % 11 % 10).Equals(digits[digits.Length - 1]);
            }

        }
        
        private void CheckKeys(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                SzukajBtn_Click(sender, (EventArgs)e);
            }
        }
        private static readonly XPen _pen = new XPen(XColors.Black, 0.5);
        void DrukujKsiazke()
        {
            string data = dateTimePicker2.Value.Date.ToString("yyyy-MM-dd");
            PdfDocument document = new PdfDocument();
            var page = new PdfPage();
            Print ksiazka = new Print();
            page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            page.Rotate = 0;

            int posYoffset = 80;
            double EndPage = page.Height -150;
            int lp = 1;
            ksiazka.RysujKsiazke(page,data);
            for (int i = 0; i < dataGridView2.Rows.Count ; i++)
            {


                if (Convert.ToBoolean(dataGridView2.Rows[i].Cells[0].Value))  //jezeli zaznaczony
                {
                    string nazwa = dataGridView2[1, i].Value.ToString();
                    string ulica = dataGridView2[2, i].Value.ToString();
                    string nrdomu = dataGridView2[3, i].Value.ToString();
                    string kod = dataGridView2[4, i].Value.ToString();
                    string miasto = dataGridView2[5, i].Value.ToString();
                    if (dataGridView2[6, i].Value.ToString().Length > 2)
                    {
                        string InnyAdres = dataGridView2[6, i].Value.ToString();
                        ulica = InnyAdres;
                        miasto = "";
                        kod = "";
                    }
                    string faktura = "";
                    if (!string.IsNullOrEmpty(dataGridView2.Rows[i].Cells[7].Value as string))
                    {
                        faktura = dataGridView2[7, i].Value.ToString();
                    }
                    if (posYoffset > EndPage)
                    {
                        page = document.AddPage();
                        posYoffset = -20;
                        page.Size = PageSize.A4;
                        page.Orientation = PageOrientation.Landscape;
                        page.Rotate = 0;
                    }
                        ksiazka.RysujKsiazkePozycje(page, lp, nazwa, ulica + " " + nrdomu + ", " + kod + " " + miasto, faktura, posYoffset);
                        posYoffset = posYoffset + 20;
                        lp = ++lp;
                    
                }

                
            }
            ksiazka.Stopka(page);
            int f = 1;
            string filename = AppDomain.CurrentDomain.BaseDirectory + @"\pdf\Ksiazka_" + data + ".pdf";
            while (File.Exists(filename)) { filename = AppDomain.CurrentDomain.BaseDirectory + @"\pdf\ksiazka_" + data + "_" + f + ".pdf"; f++; }
            
            document.Save(filename);
            Process.Start(filename);
        }
        void DrukujKoperty()
        {
            XFont font = new XFont("Times", 11, XFontStyle.Bold);
            XFont fontNormal = new XFont("Arial", 8, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
            int posX = 30;
            int posXC = 250;
            int offsetY = 10;
            int posXoplata = 450;
            string data = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");
            string[] lineour = new string[6];
            string[] linecust = new string[6];
            string file = "parametry.xml";
            string filename = "";
            Print pdf = new Print();
            PdfDocument document = new PdfDocument();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);
            XmlNodeList nodeList = xmlDoc.SelectNodes("/Parametry/Firma/Wartosc");
            int l = 0;
            foreach (XmlNode _node in nodeList)
            {
                lineour[l] = _node.InnerText.ToString(); //Kolejne linie nazwy naszej firmy z xml
                l = l + 1;
            }
            var page = new PdfPage
            {
                Size = PageSize.A5,
                Orientation = PageOrientation.Landscape,
                Rotate = 0
            };
            int nrkoperty = 1;
            for (int i = 0; i < dataGridView2.Rows.Count; i++)
            {
                if (Convert.ToBoolean(dataGridView2.Rows[i].Cells[0].Value))
                {
                    int posY = 30;
                    int posYC = 250;
                    int posYoffset = 30;
                    int posYoplata = 30;
                    
                    string nazwa = dataGridView2[1, i].Value.ToString();
                    string ulica = dataGridView2[2, i].Value.ToString();
                    string InnyAdres = "";
                    ulica = ulica + " " + dataGridView2[3, i].Value.ToString();
                    string miasto = dataGridView2[4, i].Value.ToString();
                    miasto = miasto + " " + dataGridView2[5, i].Value.ToString();
                    if (dataGridView2[6, i].Value.ToString().Length > 2)
                    {
                        InnyAdres = dataGridView2[6, i].Value.ToString();
                        ulica = InnyAdres;
                        miasto = "";
                    }
                    page = document.AddPage();
                    page.Size = PageSize.A5;
                    page.Orientation = PageOrientation.Landscape;
                    page.Rotate = 0;
                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    int c = lineour.Count();
                    //---------wydruk nasze dane-------------------
                    for (int count = 0; count < 5; count++)
                    {
                        gfx.DrawString(lineour[count], font, XBrushes.Black, new XRect(posX, posY, 190, 35), XStringFormat.TopLeft);
                        posY = posY + offsetY;
                    }
                    //--------numeracja kopert---------------------
                    gfx.DrawString(nrkoperty.ToString(), fontNormal, XBrushes.Black, new XRect(posX, posY, 190, 35), XStringFormat.TopLeft);
                    nrkoperty = ++nrkoperty;
                    //---------wydruk danych kontrahenta-----------
                    //-------------------------------------------
                    StringBuilder completedWord = new StringBuilder();
                    int znaki = nazwa.Count();
                    if (znaki > 40)
                    {
                        List<string> lines = nazwa.SplitOn(40);//Jeżeli za długa nazwa kontrahenta, to po 40 znaku, przy uwzględnieniu wyrazów, podzielic na 2 linie
                        XRect adresat1linia = new XRect(posXC+70, posYC - 10, 120, 20);
                        XRect adresat2linia = new XRect(posXC+70, posYC, 120, 20);
                        gfx.DrawString(lines[0], font, XBrushes.Black, adresat1linia, XStringFormats.TopCenter);
                        gfx.DrawString(lines[1], font, XBrushes.Black, adresat2linia, XStringFormats.TopCenter);
                    }
                    else
                    {
                        gfx.DrawString(nazwa, font, XBrushes.Black, new XRect(posXC, posYC, 190, 35), XStringFormat.TopCenter);
                    }
                    //-------------------------------------------
                    posYC = posYC + offsetY;
                    gfx.DrawString(ulica, font, XBrushes.Black, new XRect(posXC, posYC, 190, 35), XStringFormat.TopCenter);
                    posYC = posYC + offsetY;
                    gfx.DrawString(miasto, font, XBrushes.Black, new XRect(posXC, posYC, 190, 35), XStringFormat.TopCenter);
                    posYC = posYC + offsetY;
                    posYC = posYC + offsetY;
                    //----------nadruk oplata pobrana------------
                    gfx.DrawString("OPŁATA POBRANA", font, XBrushes.Black, new XRect(posXoplata, posYoplata, 120, 35), XStringFormat.TopCenter);
                    posYoplata = posYoplata + offsetY;
                    gfx.DrawString("TAXE PERÇUE - POLOGNE", font, XBrushes.Black, new XRect(posXoplata, posYoplata, 120, 35), XStringFormat.TopCenter);
                    posYoplata = posYoplata + offsetY;
                    gfx.DrawString("Umowa z Pocztą Polską S.A ID 337248/B", fontNormal, XBrushes.Black, new XRect(posXoplata, posYoplata, 120, 35), XStringFormat.TopCenter);
                    posYoplata = posYoplata + offsetY;
                }
            }
            int f = 1;
            filename = AppDomain.CurrentDomain.BaseDirectory + @"\pdf\Koperta_" + data + ".pdf";
            while (File.Exists(filename)) { filename = AppDomain.CurrentDomain.BaseDirectory + @"\pdf\Koperta_" + data + "_" + f + ".pdf"; f++; }
            document.Save(filename);
            Process.Start(filename);
        }
        void ZnajdzFakture(string nrfaktury)
        {
            string keyname = "HKEY_CURRENT_USER\\MARKET\\ListPrzewozowy";
            RejestrIO rejestr = new RejestrIO();
            string klucz = rejestr.CzytajKlucz(keyname, "SQLconnect", true);
            var conn = new SqlConnection(klucz);
            string rok = dateTimePicker1.Value.Date.ToString("yy");
         
            string sql = "SELECT * FROM OTD.dbo.Faktury where nrdok = 'FV/" + rok + "/"+nrfaktury+"'";
            SqlDataAdapter adp = new SqlDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            adp.Fill(ds);
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                string nazwa = ds.Tables[0].Rows[i][2].ToString();
                string ulica = ds.Tables[0].Rows[i][3].ToString();
                string nrdomu = ds.Tables[0].Rows[i][4].ToString();
                string kod = ds.Tables[0].Rows[i][5].ToString();
                string miasto = ds.Tables[0].Rows[i][6].ToString();
                string faktura = ds.Tables[0].Rows[i][1].ToString();
                //--dodac warunek sprawdzajacy czy w DGV istnieje firma o takiej nazwie, jezeli tak to odczytac zawartosc pola nrfaktury i dodac w jednej pozycji
                //---------wykorzystac fragment ponizszy w oddzielnej funkcji - 
                for (int rows = 0; rows < dataGridView2.Rows.Count; rows++)
                {
                    string nazwaDodawana = dataGridView2[1, rows].Value.ToString();
                    if (nazwa == nazwaDodawana)
                    {
                        string polefaktura = dataGridView2[7, rows].Value.ToString();
                        faktura = faktura + ", " + polefaktura;
                        dataGridView2.Rows.RemoveAt(rows);
                    }
                }
                //---------------
                dataGridView2.Rows.Add(true, nazwa, ulica, nrdomu, kod, miasto, " ", faktura);
            }
            dataGridView2.FirstDisplayedScrollingRowIndex = dataGridView2.RowCount - 1;
        }

    }
    public class Kontrahent
    {
        public string KontrNazwa { get; private set; }
        public string KontrUlica { get; private set; }
        public string KontrNrDomu { get; private set; }
        public string KontrKod { get; private set; }
        public string KontrMiasto { get; private set; }
        public string KontrTel { get; private set; }
        public string Uwagi { get; private set; }
        

        public Kontrahent(string nKontrNazwa, string nKontrUlica, string nKontrNrDomu, string nKontrKod, string nKontrMiasto, string nUwagi)
        {
            KontrNazwa = nKontrNazwa;
            KontrUlica = nKontrUlica;
            KontrNrDomu = nKontrNrDomu;
            KontrKod = nKontrKod;
            KontrMiasto = nKontrMiasto;
            Uwagi = nUwagi;
        }
    }
    class Print
    {
        #region
        private static readonly XPen pen = new XPen(XColors.Black, 0.5);
        private static readonly XBrush brush = XBrushes.Black;
        private static readonly XFont fontNormal = new XFont("Arial", 7, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        private static readonly XFont fontBold = new XFont("Arial", 10, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
        private static readonly XFont fontHeader = new XFont("Arial", 14, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
        private static readonly XFont fontKontr = new XFont("Arial", 6, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        private static readonly XFont fontNumer = new XFont("Arial", 6, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        private static readonly XFont fontDates = new XFont("Arial", 10, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
        private static readonly XFont fontSubtitle = new XFont("Arial", 6, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));

        int posX = 30;
        int posY = 80;
        int posXData = 350;
        int posYData = 50;

        #endregion
        public void RysujKsiazke(PdfPage page, string Data)
        {

            using (XGraphics graphics = XGraphics.FromPdfPage(page))
            {

                XRect ramka = new XRect(posX, posY, 780, 50);
                XRect KsiazkaData = new XRect(posXData, posYData, 130, 30);
               // XRect ramkaLP = new XRect(posX, posY, 20, 50);
                XRect LP = new XRect(posX, posY, 20, 50);
                XRect ramkaAdresat = new XRect(posX + 20, posY, 220, 50);
                XRect ramkaAdresat1 = new XRect(posX + 20, posY, 220, 25);
                XRect ramkaAdresat2 = new XRect(posX + 20, posY+15, 220, 25);
                XRect ramkaAdres = new XRect(posX + 240, posY, 150, 50);
                XRect ramkaAdres1 = new XRect(posX + 240, posY, 150, 25);
                XRect ramkaAdres2 = new XRect(posX + 240, posY+15, 150, 25);
                XRect ramkaDeklar = new XRect(posX + 390, posY, 50, 30);
                XRect ramkaDeklar1 = new XRect(posX + 390, posY-5, 50, 20);
                XRect ramkaDeklar2 = new XRect(posX + 390, posY+15, 50, 20);
                XRect ramkaDeklarZl = new XRect(posX + 390, posY+30, 25, 20);
                XRect ramkaDeklarGr = new XRect(posX + 415, posY+30, 25, 20);
                XRect ramkaMasa = new XRect(posX + 440, posY, 50, 30);
                XRect ramkaMasaKg = new XRect(posX + 440, posY + 30, 25, 20);
                XRect ramkaMasaG = new XRect(posX + 465, posY + 30, 25, 20);
                XRect ramkaNumer = new XRect(posX + 490, posY, 60, 50);
                XRect ramkaNumer1 = new XRect(posX + 490, posY+10, 60, 50);
                XRect ramkaUwagi = new XRect(posX + 550, posY, 30, 50);
                XRect ramkaOplata = new XRect(posX + 580, posY, 50, 30);
                XRect ramkaOplataZl = new XRect(posX + 580, posY + 30, 25, 20);
                XRect ramkaOplataGr = new XRect(posX + 605, posY + 30, 25, 20);
                XRect ramkaPobranie = new XRect(posX + 630, posY, 50, 30);
                XRect ramkaPobranie1 = new XRect(posX + 630, posY-5, 50, 30);
                XRect ramkaPobranie2 = new XRect(posX + 630, posY + 5, 50, 30);
                XRect ramkaPobranieZl = new XRect(posX + 630, posY + 30, 25, 20);
                XRect ramkaPobranieGr = new XRect(posX + 655, posY + 30, 25, 20);
                XRect ramkaFaktura = new XRect(posX + 680, posY, 100, 50);
                XRect ramkaKolumna1 = new XRect(posX, posY+50, 20, 20);
                XRect ramkaKolumna2 = new XRect(posX + 20, posY+50, 220, 20);
                XRect ramkaKolumna3 = new XRect(posX + 240, posY + 50, 150, 20);
                XRect ramkaKolumna4 = new XRect(posX + 390, posY + 50, 50, 20);
                XRect ramkaKolumna5 = new XRect(posX + 440, posY + 50, 50, 20);
                XRect ramkaKolumna6 = new XRect(posX + 490, posY + 50, 60, 20);
                XRect ramkaKolumna7 = new XRect(posX + 550, posY + 50, 30, 20);
                XRect ramkaKolumna8 = new XRect(posX + 580, posY + 50, 50, 20);
                XRect ramkaKolumna9 = new XRect(posX + 630, posY + 50, 50, 20);
                XRect ramkaKolumna10 = new XRect(posX + 680, posY + 50, 100, 20);
                XRect ramkaPrzeniesienie1 = new XRect(posX, posY + 70, 390, 20);
                XRect ramkaPrzeniesienie2 = new XRect(posX + 440, posY + 70, 140, 20);
                XRect ramkaPrzeniesienie1txt = new XRect(posX, posY + 70, 680, 20);
                XRect ramkaPrzeniesienie2txt = new XRect(posX + 440, posY + 70, 200, 20);



                graphics.DrawRectangle(pen, ramka);
                graphics.DrawRectangle(pen, ramka);
                graphics.DrawRectangle(pen, ramkaAdresat);
                graphics.DrawRectangle(pen, ramkaAdres);
                graphics.DrawRectangle(pen, ramkaDeklar);
                graphics.DrawRectangle(pen, ramkaDeklarZl);
                graphics.DrawRectangle(pen, ramkaDeklarGr);
                graphics.DrawRectangle(pen, ramkaMasa);
                graphics.DrawRectangle(pen, ramkaMasaKg);
                graphics.DrawRectangle(pen, ramkaMasaG);
                graphics.DrawRectangle(pen, ramkaNumer);
                graphics.DrawRectangle(pen, ramkaUwagi);
                graphics.DrawRectangle(pen, ramkaOplata);
                graphics.DrawRectangle(pen, ramkaOplataZl);
                graphics.DrawRectangle(pen, ramkaOplataGr);
                graphics.DrawRectangle(pen, ramkaPobranie);
                graphics.DrawRectangle(pen, ramkaPobranieZl);
                graphics.DrawRectangle(pen, ramkaPobranieGr);
                graphics.DrawRectangle(pen, ramkaFaktura);
                graphics.DrawRectangle(pen, ramkaKolumna1);
                graphics.DrawRectangle(pen, ramkaKolumna2);
                graphics.DrawRectangle(pen, ramkaKolumna3);
                graphics.DrawRectangle(pen, ramkaKolumna4);
                graphics.DrawRectangle(pen, ramkaKolumna5);
                graphics.DrawRectangle(pen, ramkaKolumna6);
                graphics.DrawRectangle(pen, ramkaKolumna7);
                graphics.DrawRectangle(pen, ramkaKolumna8);
                graphics.DrawRectangle(pen, ramkaKolumna9);
                graphics.DrawRectangle(pen, ramkaKolumna10);
                graphics.DrawRectangle(pen, ramkaPrzeniesienie1);
                graphics.DrawRectangle(pen, ramkaPrzeniesienie2);



                graphics.DrawString(Data, fontHeader, brush, KsiazkaData, XStringFormats.Center);
                graphics.DrawString("LP", fontNormal, brush, LP, XStringFormats.Center);
                graphics.DrawString("ADRESAT", fontNormal, brush, ramkaAdresat1, XStringFormats.Center);
                graphics.DrawString("(imię i nazwisko lub nazwa)", fontNormal, brush, ramkaAdresat2, XStringFormats.Center);
                graphics.DrawString("Adres adresata", fontNormal, brush, ramkaAdres, XStringFormats.Center);
                graphics.DrawString("Kwota", fontNormal, brush, ramkaDeklar1, XStringFormats.Center);
                graphics.DrawString("zadekl.", fontNormal, brush, ramkaDeklar, XStringFormats.Center);
                graphics.DrawString("wartości", fontNormal, brush, ramkaDeklar2, XStringFormats.Center);
                graphics.DrawString("zł", fontNormal, brush, ramkaDeklarZl, XStringFormats.Center);
                graphics.DrawString("gr", fontNormal, brush, ramkaDeklarGr, XStringFormats.Center);
                graphics.DrawString("Masa", fontNormal, brush, ramkaMasa, XStringFormats.Center);
                graphics.DrawString("kg", fontNormal, brush, ramkaMasaKg, XStringFormats.Center);
                graphics.DrawString("g", fontNormal, brush, ramkaMasaG, XStringFormats.Center);
                graphics.DrawString("Numer", fontNormal, brush, ramkaNumer, XStringFormats.Center);
                graphics.DrawString("nadawczy", fontNormal, brush, ramkaNumer1, XStringFormats.Center);
                graphics.DrawString("Uwagi", fontNormal, brush, ramkaUwagi, XStringFormats.Center);
                graphics.DrawString("Opłata", fontNormal, brush, ramkaOplata, XStringFormats.Center);
                graphics.DrawString("Kwota", fontNormal, brush, ramkaPobranie1, XStringFormats.Center);
                graphics.DrawString("pobrania", fontNormal, brush, ramkaPobranie2, XStringFormats.Center);
                graphics.DrawString("Nr faktury", fontNormal, brush, ramkaFaktura, XStringFormats.Center);


                graphics.DrawString("1", fontNormal, brush, ramkaKolumna1, XStringFormats.Center);
                graphics.DrawString("2", fontNormal, brush, ramkaKolumna2, XStringFormats.Center);
                graphics.DrawString("3", fontNormal, brush, ramkaKolumna3, XStringFormats.Center);
                graphics.DrawString("4", fontNormal, brush, ramkaKolumna4, XStringFormats.Center);
                graphics.DrawString("5", fontNormal, brush, ramkaKolumna5, XStringFormats.Center);
                graphics.DrawString("6", fontNormal, brush, ramkaKolumna6, XStringFormats.Center);
                graphics.DrawString("7", fontNormal, brush, ramkaKolumna7, XStringFormats.Center);
                graphics.DrawString("8", fontNormal, brush, ramkaKolumna8, XStringFormats.Center);
                graphics.DrawString("9", fontNormal, brush, ramkaKolumna9, XStringFormats.Center);
                graphics.DrawString("10", fontNormal, brush, ramkaKolumna10, XStringFormats.Center);

                graphics.DrawString("Z przeniesienia", fontNormal, brush, ramkaPrzeniesienie1txt, XStringFormats.Center);
                graphics.DrawString("Z przeniesienia", fontNormal, brush, ramkaPrzeniesienie2txt, XStringFormats.Center);

            }
        }
        public void RysujKsiazkePozycje(PdfPage page, int lp, string nazwa, string adres, string nrfaktury, int posYoffset)
        {
            
            using (XGraphics graphics = XGraphics.FromPdfPage(page))
            {
                XRect LPKontr = new XRect(posX, posYoffset + 90, 20, 20);
                XRect AdresatKontr = new XRect(posX + 20, posYoffset + 90, 220, 20);
                XRect AdresKontr = new XRect(posX + 240, posYoffset + 90, 150, 20);
                XRect LPKontrtxt = new XRect(posX+8, posYoffset + 95, 20, 20);
                XRect AdresatKontrtxt = new XRect(posX + 23, posYoffset + 95, 220, 20);
                XRect AdresKontrtxt = new XRect(posX + 243, posYoffset + 95, 150, 20);
                XRect DeklarKontr1 = new XRect(posX + 390, posYoffset + 90, 25, 20);
                XRect DeklarKontr2 = new XRect(posX + 415, posYoffset + 90, 25, 20);
                XRect MasaKontr1 = new XRect(posX + 440, posYoffset + 90, 25, 20);
                XRect MasaKontr2 = new XRect(posX + 465, posYoffset + 90, 25, 20);
                XRect NumerKontr = new XRect(posX + 490, posYoffset + 90, 60, 20);
                XRect UwagiKontr = new XRect(posX + 550, posYoffset + 90, 30, 20);
                XRect OplataKontr1 = new XRect(posX + 580, posYoffset + 90, 25, 20);
                XRect OplataKontr2 = new XRect(posX + 605, posYoffset + 90, 25, 20);
                XRect PobranieKontr1 = new XRect(posX + 630, posYoffset + 90, 25, 20);
                XRect PobranieKontr2 = new XRect(posX + 655, posYoffset + 90, 25, 20);
                XRect FakturaKontr = new XRect(posX + 680, posYoffset + 90, 100, 20);
                XRect FakturaKontrtxt = new XRect(posX + 683, posYoffset + 95, 100, 20);

                graphics.DrawRectangle(pen, LPKontr);
                graphics.DrawRectangle(pen, AdresatKontr);
                graphics.DrawRectangle(pen, AdresKontr);
                graphics.DrawRectangle(pen, DeklarKontr1);
                graphics.DrawRectangle(pen, DeklarKontr2);
                graphics.DrawRectangle(pen, MasaKontr1);
                graphics.DrawRectangle(pen, MasaKontr2);
                graphics.DrawRectangle(pen, NumerKontr);
                graphics.DrawRectangle(pen, UwagiKontr);
                graphics.DrawRectangle(pen, OplataKontr1);
                graphics.DrawRectangle(pen, OplataKontr2);
                graphics.DrawRectangle(pen, PobranieKontr1);
                graphics.DrawRectangle(pen, PobranieKontr2);
                graphics.DrawRectangle(pen, FakturaKontr);



                graphics.DrawString(lp.ToString(), fontKontr, brush, LPKontrtxt, XStringFormats.TopLeft);
                StringBuilder completedWord = new StringBuilder();
                int znaki = nazwa.Count();
                if (znaki > 53)
                {

                    completedWord.Append(nazwa.Substring(0, 53));//Jeżeli za długa nazwa kontrahenta, to po 53 znaku podzielic na 2 linie
                    completedWord.AppendLine();
                    string pierwszalinia = completedWord.ToString();
                    completedWord.Clear();
                    completedWord.Append(nazwa.Substring(53, znaki - 53));
                    string drugalinia = completedWord.ToString();
                    XRect adresat1linia = new XRect(posX + 23, posYoffset + 93, 150, 20);
                    XRect adresat2linia = new XRect(posX + 23, posYoffset + 100, 150, 20);
                    graphics.DrawString(pierwszalinia, fontKontr, brush, adresat1linia, XStringFormats.TopLeft);
                    graphics.DrawString(drugalinia, fontKontr, brush, adresat2linia, XStringFormats.TopLeft);
                }
                else
                {
                    graphics.DrawString(nazwa, fontKontr, brush, AdresatKontrtxt, XStringFormats.TopLeft);
                }
                graphics.DrawString(adres, fontKontr, brush, AdresKontrtxt, XStringFormats.TopLeft);

                //--------------
                int znakiFV = nrfaktury.Count();
                StringBuilder completedFV = new StringBuilder();
                if (znakiFV > 30)
                {
                    completedFV.Append(nrfaktury.Substring(0, 30));
                    completedFV.AppendLine();
                    string nrfakturypierwszalinia = completedFV.ToString();
                    completedFV.Clear();
                    completedFV.Append(nrfaktury.Substring(30, znakiFV - 30));
                    string nrfakturydrugalinia = completedFV.ToString();
                    XRect nrfaktury1linia = new XRect(posX + 683, posYoffset + 93, 100, 20);
                    XRect nrfaktury2linia = new XRect(posX + 683, posYoffset + 100, 100, 20);
                    graphics.DrawString(nrfakturypierwszalinia, fontKontr, brush, nrfaktury1linia, XStringFormats.TopLeft);
                    graphics.DrawString(nrfakturydrugalinia, fontKontr, brush, nrfaktury2linia, XStringFormats.TopLeft);
                }
                else
                {
                    graphics.DrawString(nrfaktury, fontNumer, brush, FakturaKontrtxt, XStringFormats.TopLeft);
                }
                //-----------------
                
            }
        }
        public void Stopka(PdfPage page)
        {
            string wersja = "20180322";

            using (XGraphics graphics = XGraphics.FromPdfPage(page))
            {
                graphics.DrawLine(pen, new XPoint(28, page.Height - 26), new XPoint(page.Width - 30.75, page.Height - 26));
                XRect rectFooter = new XRect(28, page.Height - 25, page.Width - 59, 7);
                XStringFormat formatNear = new XStringFormat
                {
                    Alignment = XStringAlignment.Near
                };
                XStringFormat formatFar = new XStringFormat
                {
                    Alignment = XStringAlignment.Far
                };
                graphics.DrawString("Książka nadawcza v1."+wersja+" ©sejto.pl", fontSubtitle, brush, rectFooter, formatNear);
            }
        }
    }
    public static class StringExtensions
    {

        /// <summary>Use this function like string.Split but instead of a character to split on, 
        /// use a maximum line width size. This is similar to a Word Wrap where no words will be split.</summary>
        /// Note if the a word is longer than the maxcharactes it will be trimmed from the start.
        /// <param name="initial">The string to parse.</param>
        /// <param name="MaxCharacters">The maximum size.</param>
        /// <remarks>This function will remove some white space at the end of a line, but allow for a blank line.</remarks>
        /// 
        /// <returns>An array of strings.</returns>
        public static List<string> SplitOn(this string initial, int MaxCharacters)
        {
            List<string> lines = new List<string>();

            if (string.IsNullOrEmpty(initial) == false)
            {
                string targetGroup = "Line";
                string pattern = string.Format(@"(?<{0}>.{{1,{1}}})(?:\W|$)", targetGroup, MaxCharacters);

                lines = Regex.Matches(initial, pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant)
                             .OfType<Match>()
                             .Select(mt => mt.Groups[targetGroup].Value)
                             .ToList();
            }
            return lines;
        }
    }
}
