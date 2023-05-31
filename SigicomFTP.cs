using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Linq;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using ServiceReportProgram;

namespace ServiceReportProgram
{
    [Serializable()]
    class SigicomFTP
    {
        public string sigicomId { get; set; }
        public string D10_C12_Temperature { get; set; }
        public string D10_BatteryPercent { get; set; }
        public string IM_Voltage { get; set; }
        public double availableSpace { get; set; }
        public DateTime statusCreated { get; set; }
        public string nodeState { get; set; }
        public bool connectionMissed { get; set; }
        public bool memoryLow { get; set; }
        public bool nodeLost { get; set; }
        public string errorReport { get; set; }
        public string statusFile { get; set; }




        //**Aliohjelma "SaveStatusFiles" metodille**

        public List<string> ListStatusFiles(string ftp_user, string ftp_pass)
        {
            SigicomFTP SigicomCredentials = new SigicomFTP();

            try
            {
                //FTP-palvelimen hakemiston sisältö haetaan ja tallennetaan "StatusFileNames"-listaan.
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://uplFR@ftp1.finnrock.fi/remote/");
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                request.Credentials = new NetworkCredential(ftp_user, ftp_pass);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string names = reader.ReadToEnd();

                reader.Close();
                response.Close();
                List<string> StatusFileNames = new List<string>();
                StatusFileNames = names.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                return StatusFileNames;
            }
            catch (Exception)
            {
                throw;
            }
        }

        //**Aliohjelma "SaveStatusFiles" metodille** (Splittaa status-oliosta tiedot)
        public string Splitteri(string strSource, string strStart, string strEnd)
        {
            try
            {
                if (strSource.Contains(strStart) && strSource.Contains(strEnd))
                {
                    int Start, End;
                    Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                    End = strSource.IndexOf(strEnd, Start);
                    return strSource.Substring(Start, End - Start);
                }
            }
            catch (Exception)
            {

                Console.WriteLine("ERROR on 'Splitteri'-method.");
            }


            return "";
        }

        //**Aliohjelma "SaveStatusFiles" metodille**
        public string GetStatusFileFromFTP(string statusFileName, string ftp_user, string ftp_pass)

        {
            SigicomFTP sigicomCredentials = new SigicomFTP();
            WebClient request = new WebClient();

            string url = "ftp://uplFR@ftp1.finnrock.fi/remote/" + statusFileName;
            string statusFile = "";

            request.Credentials = new NetworkCredential(ftp_user, ftp_pass);

            try
            {
                byte[] newFileData = request.DownloadData(new Uri(url));
                Console.WriteLine("STATUS-file " + statusFileName + " ladattu.");
                string fileString = System.Text.Encoding.UTF8.GetString(newFileData);

                statusFile = fileString;
            }
            catch (WebException e)
            {
                Console.WriteLine("Tiedostoa: " + statusFileName + " ei ladattu.");
            }
            return statusFile;

        }

        ////**Aliohjelma status tietojen tallentamiseen** (käytetään Program luokassa)
        public List<SigicomFTP> SaveStatusFiles()
        {
            ////OBJEKTILISTA JOHON TIEDOT TALLENNETAAN
            List<SigicomFTP> sigicomObjectList = new List<SigicomFTP>();


            Console.WriteLine("Syötä 'ftp1.finnrock.fi' palvelimen käyttäjätunnus ja paina enter:");
            string ftp_user = Console.ReadLine();
            Console.WriteLine("Syötä 'ftp1.finnrock.fi' palvelimen salasana ja paina enter:");
            string ftp_pass = Console.ReadLine();

            try
            {
                //Sigicom STATUS-filelistan haku palvelimelta
                SigicomFTP getStatusFileNames = new SigicomFTP();
                List<string> statusFileList = getStatusFileNames.ListStatusFiles(ftp_user, ftp_pass);

                //Muuttujat tulostettavaa prosenttilaskuria varten
                int counter = 1;
                bool onePercentTrue = false;
                double onePercentInDaDouble = statusFileList.Count();
                double percentInDaDouble = (onePercentInDaDouble / 100);
                double onePercent = (onePercentInDaDouble / 100);
                double percents = 0;

                foreach (var statusFileName in statusFileList)
                {

                    try
                    {
                        //Status-tiedoston lataamista ei suoriteta jos status file on .tmp tiedosto (nou skula)                        
                        if (statusFileName.Contains("status") && !statusFileName.Contains("tmp"))
                        {
                            // Hakee Sigicom STATUS-filen palvelimelta "GetStatusFileNames" listan mukaisesti
                            SigicomFTP getStatusFile = new SigicomFTP();
                            string sigicomStatusFile = getStatusFile.GetStatusFileFromFTP(statusFileName, ftp_user, ftp_pass);

                            //Väliaikainen objektilista johon tiedot tallennetaan
                            SigicomFTP sigicom = new SigicomFTP();


                            //TIETOJEN TALLENNUS OBJEKTILISTAAN ERI EHDOIN
                            //Mittarien tyyppien tallenns D10/C12/IM
                            if (sigicomStatusFile.Contains("[\"type\"] = \"D10\""))
                            {
                                sigicom.sigicomId = Splitteri(statusFileName, "", ".status");
                                sigicom.sigicomId = sigicom.sigicomId.Replace(@"IM", "D10-");
                            }
                            else if (sigicomStatusFile.Contains("[\"type\"] = \"COMPACT\""))
                            {
                                sigicom.sigicomId = Splitteri(statusFileName, "", ".status");
                                sigicom.sigicomId = sigicom.sigicomId.Replace(@"IM", "C12-");
                            }
                            else if (sigicomStatusFile.Contains("[\"type\"] = \"IM\""))
                            {
                                sigicom.sigicomId = Splitteri(statusFileName, "", ".status");
                                sigicom.sigicomId = sigicom.sigicomId.Replace(@"IM", "IM-");
                            }
                            else
                            {

                            }

                            //** SigicomOBJECT.statusCreated-arvon Status-tiedostosta **

                            //Status-filen splittaus listaan ','-erottimella, jonka jälkeen 'created' päivämäärät (2kpl) tallennetaan DateTime-listaan.
                            //DateTime-listasta valitaan tuoreempi päivämäärä, joka asetetaan 'SigicomOBJECT.statusCreated'-arvoksi.
                            List<string> splittedStringList = new List<string>();
                            splittedStringList = sigicomStatusFile.Split(',').ToList();
                            DateTime compareDate = DateTime.ParseExact("01-01-1900", "dd-MM-yyyy", CultureInfo.InvariantCulture);

                            List<DateTime> datetimelista = new List<DateTime>();
                            if (sigicomStatusFile.Contains("created"))
                            {
                                foreach (var item in splittedStringList)
                                {
                                    if (item.Contains("created"))
                                    {
                                        DateTime a = Convert.ToDateTime(Splitteri(item, "[\"created\"] = \"", "\""));
                                        datetimelista.Add(a);
                                    }
                                }
                            }
                            foreach (var item in datetimelista)
                            {
                                if (compareDate < item)
                                {
                                    compareDate = item;
                                }

                            }

                            sigicom.statusCreated = compareDate;
                            sigicom.IM_Voltage = Splitteri(sigicomStatusFile, "[\"voltage\"] = ", ",");
                            sigicom.availableSpace = Convert.ToDouble(Splitteri(sigicomStatusFile, "[\"availspace\"] = ", ","));
                            sigicom.nodeState = Splitteri(sigicomStatusFile, "[\"node_state\"] = {", "},");
                            sigicom.statusFile = sigicomStatusFile;

                            DateTime now = DateTime.Now;
                            Settings settings = new Settings();

                            //Määrittele asetukset yhteydenoton lajittelulle -> 1 vrk?
                            DateTime statusCreated = sigicom.statusCreated.AddDays(settings.setConnectionMissed);
                            //Jos "compare" on <0 = vertailun ensimmäinen muuttuja on aikaisempi päivämäärä
                            int compare = DateTime.Compare(statusCreated, now);

                            if (sigicom.nodeState.Contains("lost"))
                            {
                                sigicom.nodeLost = true;
                            }
                            //Jos laite ei ole ottanut yhteyttä yllä määritetyn vuorokausimäärän (2vrk) mukaisesti -> connectionMissed = true; / CONNECTION MISSED
                            if (compare < 0)
                            {
                                sigicom.connectionMissed = true;
                            }
                            //Laitteen vajavaisen muistin määrittely "20000000" = 20mb / MEMORYLOW
                            if (sigicom.availableSpace < 20000000)
                            {
                                sigicom.memoryLow = true;
                            }

                            if (sigicomStatusFile.Contains("voltage"))
                            {
                                sigicom.D10_BatteryPercent = Splitteri(sigicomStatusFile, $"[\"battery_percent\"] = ", ",");
                            }
                            if (sigicomStatusFile.Contains("temperature"))
                            {
                                sigicom.D10_C12_Temperature = Splitteri(sigicomStatusFile, $"[\"temperature\"] = ", ",");
                            }

                            //LISÄÄ FORLOOPIN VÄLIAIKAINEN OBJEKTI TIETOINEEN LOPULLISEEN OBJEKTILISTAAN             


                            sigicomObjectList.Add(sigicom);

                            Console.WriteLine("Sigicom " + statusFileName + " tiedosto tallennettu");
                            counter++;
                            Console.WriteLine((counter) + "/" + statusFileList.Count());


                            //KONSOLIN PROSENTTILASKURI
                            //######################################################################
                            //#
                            if (counter > onePercent)                                            //#
                            {                                                                    //#
                                onePercentTrue = true;                                           //#
                            }                                                                    //#
                            if (onePercentTrue == false)                                         //#
                            {                                                                    //#
                                if (counter >= onePercent)                                       //#
                                {                                                                //#
                                    percents = percents + 1;                                     //#
                                }                                                                //#
                            }                                                                    //#
                            if (counter >= percentInDaDouble)                                    //#
                            {                                                                    //#
                                percents = percents + 1;                                         //#
                                percentInDaDouble = percentInDaDouble + onePercent;              //#
                            }                                                                    //#
                            Console.WriteLine(percents + "% ladattu.");                          //#
                            //######################################################################

                            Console.WriteLine();
                        }

                    }
                    catch (Exception)
                    {
                        //Tiedostolaskuri
                        Console.WriteLine("*** Tiedostoa: " + statusFileName + " ei tallennettu. ***");
                        Console.WriteLine((counter++) + "/" + statusFileList.Count());

                        //KONSOLIN PROSENTTILASKURI
                        //######################################################################
                        //#
                        if (counter > onePercent)                                            //#
                        {                                                                    //#
                            onePercentTrue = true;                                           //#
                        }                                                                    //#
                        if (onePercentTrue == false)                                         //#
                        {                                                                    //#
                            if (counter >= onePercent)                                       //#
                            {                                                                //#
                                percents = percents + 1;                                     //#
                            }                                                                //#
                        }                                                                    //#
                        if (counter >= percentInDaDouble)                                    //#
                        {                                                                    //#
                            percents = percents + 1;                                         //#
                            percentInDaDouble = percentInDaDouble + onePercent;              //#
                        }                                                                    //#
                        Console.WriteLine(percents + "% ladattu.");                          //#
                        //######################################################################

                        Console.WriteLine();
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR in 'SaveStatusFiles'-method. Check that your internet connection is working securely.");

            }
            return sigicomObjectList;
        }

    }
}
