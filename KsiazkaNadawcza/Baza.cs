using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    class Baza
    {
//        List<string> zawartosc = new List<string>();
        public string CzytajZBazy(string sql)
        {
            string keyname = "HKEY_CURRENT_USER\\MARKET\\ListPrzewozowy";
            RejestrIO rejestr = new RejestrIO();
            string klucz = rejestr.CzytajKlucz(keyname, "SQLconnect", true); 
            var conn = new SqlConnection(klucz);
            string zawartosc="";
            conn.Open();
            using (SqlCommand command = new SqlCommand(sql, conn))
            {
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    zawartosc=(reader.GetValue(0).ToString());
                }
                conn.Close();
                return zawartosc;
            }
            
        }  //zwraca STRING
        
        public void ZapiszDoBazy(string sql) 
        {
            string keyname = "HKEY_CURRENT_USER\\MARKET\\ListPrzewozowy";
            RejestrIO rejestr = new RejestrIO();
            string klucz = rejestr.CzytajKlucz(keyname, "SQLconnect", true);
            using (var conn = new SqlConnection(klucz))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = sql;
                var result = cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public DataSet Polacz(string sql)
        {
            DataSet ds = new DataSet();
            string keyname = "HKEY_CURRENT_USER\\MARKET\\ListPrzewozowy";
            RejestrIO rejestr = new RejestrIO();
            string klucz = rejestr.CzytajKlucz(keyname, "SQLconnect", true); //parametry połączenia do bazy SQL zapisane w rejestrze
            var conn = new SqlConnection(klucz);
            SqlDataAdapter da = new SqlDataAdapter(sql, conn);
            da.Fill(ds, "Kontrahenci");
            conn.Close();
            conn.Dispose();
            da.Dispose();
            return ds;
        } //Zwraca DataSet

       public void ReinicjalizacjaBazy()
        {
            string sql = "drop table List";
            ZapiszDoBazy(sql);
            sql = "drop table Dok";
            ZapiszDoBazy(sql);
            sql = "CREATE TABLE [dbo].[Dok]([ID] int IDENTITY(1,1) primary key" +
                ",[Data] [varchar] (10) NOT NULL"+
                ",[UserID]int NOT NULL)";
            ZapiszDoBazy(sql);
            sql = "CREATE TABLE [dbo].[List](" +
                "[ID] int IDENTITY(1,1) primary key," +
                "[DokID]int NOT NULL," +
                "[KontrId] [numeric] (9,0) NOT NULL," +
                "[PaliwoID] [int] NOT NULL," +
                "[Ilosc] [int] NOT NULL," +
                "[Cena][varchar] (6) NULL," +
                "[FormaPlat] [varchar] (8) NULL," +
                "[Termin] [int] NULL," +
                "[Sent] [varchar] (20) NULL," +
                "[DostUlica] [varchar] (40) NULL," +
                "[DostNr] [varchar] (8) NULL," +
                "[DostMiasto] [varchar] (40) NULL," +
                "[DostKod] [varchar] (10) NULL," +
                "[DostPoczta] [varchar] (40) NULL," +
                "[DostKraj] [varchar] (15) NULL," +
                "[DostPlanRozp] [varchar] (10) NULL," +
                "[DostRozp] [varchar] (10) NULL," +
                "[DostPlanZak] [varchar] (10) NULL," +
                "[Uwagi] [varchar] (250) NULL," +
                "[NrWZ] [int] NOT NULL," +
                "[Aktywny][bit]NOT NULL)";
            ZapiszDoBazy(sql);
        }
    }
}
