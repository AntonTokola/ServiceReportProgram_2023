using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using Microsoft.Win32.SafeHandles;
using ServiceReportProgram;
using static ServiceReportProgram.Settings;

namespace ServiceReportProgram
{
    class TikruInfo
    {
        public DateTime LaskutuksenAloitus { get; set; }
        public string MittarinSijainti { get; set; }
        public string LoggerinSarjanumero { get; set; }
        public string MittauksestaVastaava { get; set; }
        public string HuoltoraportinVastaanottaja { get; set; }
        public string NoutoPaiva { get; set; }
        public string Asentaja { get; set; }
        public string ProjektinVetaja { get; set; }
        public string ProjektinNimi { get; set; }
        public string ProjektinNumero { get; set; }
        public string Asiakas { get; set; }
        public string AsennuksenLisatiedot { get; set; }

        public int CountTikruRecords { get; set; }
        public AvaTrace AvaErrorReport { get; set; }
        public SigicomFTP sigicomErrorReport { get; set; }
        public List<TikruInfo> errorReportList { get; set; }
        public SafeFileHandle ToSaveFileTo { get; private set; }

        public int TikruRecordsCount(TikruInfo info)
        {
            TikruInfo Count = new TikruInfo();
            Count = info;
            return Convert.ToInt32(info.CountTikruRecords);
        }


        public StreamReader TikruLogIn()
        {
            Console.WriteLine("Syötä Tikrun (CRM) käyttäjätunnus ja paina enter:");
            string crmUserName = Console.ReadLine();
            Console.WriteLine("Syötä salasana ja paina enter:");

            //Salasanan piilottaminen
            var crmPassword = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && crmPassword.Length > 0)
                {
                    Console.Write("\b \b");
                    crmPassword = crmPassword[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    crmPassword += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);



            Console.WriteLine();

            //Kirjautuminen Tikruun/CRM-järjestelmään + CSV-tiedoston lataus
            var client = new CookieAwareWebClient();
            client.BaseAddress = @"http://xrm.forcit.org/FI01081666/";
            var loginData = new NameValueCollection();
            loginData.Add("username", crmUserName);
            loginData.Add("password", crmPassword);
            Console.WriteLine("Kirjaudutaan Tikruun...");
            client.UploadValues("index.php?module=Users&action=Login", "POST", loginData);
            string htmlSource = client.DownloadString("index.php");
            Console.WriteLine();
            if (htmlSource.Contains("Mittariasennukset"))
            {
                Console.WriteLine("Kirjautuminen onnistui. Ladataan tärinämittariasennusten tietoja... (tässä saattaa kestää hetki)");
            }
            else
            {
                Console.WriteLine("Kirjautuminen ei onnistunut");
                Environment.Exit(0);
            }

            var tikruByte = client.DownloadData("index.php?module=Reports&view=ExportReport&mode=GetCSV&record=222");
            Console.WriteLine("Tikrun tiedot ladattu onnistuneesti.");
            string tikruString = Encoding.UTF8.GetString(tikruByte, 0, tikruByte.Length);
            var tikruStreamReader = new StreamReader(new MemoryStream(tikruByte));

            return tikruStreamReader;
        }
        public TikruInfo[] GetTikruInfo()
        {
            TikruInfo tikru = new TikruInfo();
            // Hakee Tikrun palvelimelta laskutus ja mittaritiedot sisältävän .CSV raportin. (automaticStreamReader)
            var automaticStreamReader = tikru.TikruLogIn();

            // Hakee Tikrun tiedot paikalliselta .CSV tiedostolta (käytössä vaín debugausta varten) (manualStreamReader)

            using (var csvReader = new CsvReader(automaticStreamReader, CultureInfo.InvariantCulture))


            {
                var records = csvReader.GetRecords<dynamic>().ToList();

                //CSV-listan indeksien määrä
                int LoggerCounter = records.Count();

                TikruInfo[] tikruInfoTable = new TikruInfo[LoggerCounter];
                int indexcount = 0;

                foreach (var item1 in records)
                {
                    TikruInfo tikruInfo = new TikruInfo();

                    //Asennusten tiedot tallennetaan TIKRUOBJECT-objektiin
                    foreach (var item in item1)
                    {
                        if (item.Key == "Mittariasennukset Projekti")
                        {
                            tikruInfo.ProjektinNimi = item.Value;
                        }
                        if (item.Key == "Mittariasennukset Mittalaitteen sijainti")
                        {
                            tikruInfo.MittarinSijainti = item.Value;
                        }
                        if (item.Key == "Mittariasennukset Laskutuksen aloituspvm")
                        {
                            if (item.Value != "-")
                            {
                                tikruInfo.LaskutuksenAloitus = DateTime.ParseExact(item.Value, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            }
                        }
                        if (item.Key == "Mittariasennukset Ohjattu")
                        {
                            tikruInfo.Asentaja = item.Value;
                        }
                        if (item.Key == "Projektit Ohjattu käyttäjälle")
                        {
                            tikruInfo.ProjektinVetaja = item.Value;
                        }
                        if (item.Key == "Projektit Mittauksesta vastaava")
                        {
                            tikruInfo.MittauksestaVastaava = item.Value;
                        }
                        if (tikruInfo.ProjektinVetaja != null)
                        {
                            if (tikruInfo.ProjektinVetaja != "")
                            {
                                tikruInfo.HuoltoraportinVastaanottaja = tikruInfo.ProjektinVetaja;
                            }
                        }
                        if (tikruInfo.MittauksestaVastaava != null)
                        {
                            if (tikruInfo.MittauksestaVastaava != "")
                            {
                                if (tikruInfo.MittauksestaVastaava == tikruInfo.ProjektinVetaja)
                                {

                                }
                                if (tikruInfo.MittauksestaVastaava != tikruInfo.ProjektinVetaja)
                                {
                                    tikruInfo.HuoltoraportinVastaanottaja = tikruInfo.MittauksestaVastaava;
                                }

                            }
                        }


                        if (item.Key == "Projektit Projektin numero")
                        {
                            tikruInfo.ProjektinNumero = item.Value;
                        }

                        if (item.Key == "Mittariasennukset Mittalaite")
                        {
                            tikruInfo.LoggerinSarjanumero = item.Value;
                            if (tikruInfo.LoggerinSarjanumero.Contains("im"))
                            {
                                tikruInfo.LoggerinSarjanumero = tikruInfo.LoggerinSarjanumero.Replace("im", "IM");
                            }

                            if (tikruInfo.LoggerinSarjanumero.Contains("ava"))
                            {
                                tikruInfo.LoggerinSarjanumero = tikruInfo.LoggerinSarjanumero.Replace("ava", "Ava");
                            }
                            if (tikruInfo.LoggerinSarjanumero.Contains("avat"))
                            {
                                tikruInfo.LoggerinSarjanumero = tikruInfo.LoggerinSarjanumero.Replace("avat", "AvaT");
                            }
                            if (tikruInfo.LoggerinSarjanumero.Contains("Avat"))
                            {
                                tikruInfo.LoggerinSarjanumero = tikruInfo.LoggerinSarjanumero.Replace("Avat", "AvaT");
                            }
                            if (tikruInfo.LoggerinSarjanumero.Contains("LAINA"))
                            {
                                tikruInfo.LoggerinSarjanumero = tikruInfo.LoggerinSarjanumero.Replace(" LAINA", "");
                            }
                            if (tikruInfo.LoggerinSarjanumero.Contains("abe"))
                            {
                                tikruInfo.LoggerinSarjanumero = tikruInfo.LoggerinSarjanumero.Replace("abe", "ABE");
                            }

                        }

                        if (item.Key == "Projektit Asiakas")
                        {
                            tikruInfo.Asiakas = item.Value;
                        }
                        if (item.Key == "Mittariasennukset Lisätietoja")
                        {
                            tikruInfo.AsennuksenLisatiedot = item.Value;
                        }

                    }
                    tikruInfoTable[indexcount] = tikruInfo;
                    indexcount++;

                }
                return tikruInfoTable;
            }
        }



        //-LASKUTUKSEN ALLA OLEVIEN ASENNUSTEN TALLENNUS YHTEEN LISTAAN
        //-ASENNUKSET TALLENNETAAN VIIMEISIMMÄN LASKUTUSPÄIVÄMÄÄRÄN PERUSTEELLA
        //-LISTASSA ON VAIN YKSI ASENNUSMERKINTÄ YHTÄ MITTARIA KOHTI
        public List<TikruInfo> FindDuplicates(TikruInfo[] TikruInfoTable, int TikruRecordsCount)
        {
            //TUPLASTI LASKULLA OLEVIEN MITTAREIDEN TIETOJEN LAJITTELU KAHDELLE ERI LISTALLE
            //Apumuuttujat looppeja varten
            int index = Convert.ToInt32(TikruRecordsCount);
            int arrayIndex = (index - 1);
            string[] compareSerialNumbers = new string[index];

            //Loggerien sarjanumerot tallennetaan uuteen taulukkoon (sarjanumerotaulukkoon)
            foreach (var item in TikruInfoTable)
            {
                compareSerialNumbers[arrayIndex] = item.LoggerinSarjanumero;
                arrayIndex = arrayIndex - 1;
            }
            //"Apulista" tuplana laskettavista loggereista
            List<string> listOfDuplicatesHelpList = new List<string>();
            //LISTA TUPLANA OLEVISTA LOGGEREISTA
            List<string> listOfDuplicates = new List<string>();

            arrayIndex = (index - 1);
            foreach (var item in TikruInfoTable)
            {

                if (item.LoggerinSarjanumero == compareSerialNumbers[arrayIndex])
                {
                    arrayIndex = arrayIndex - 1;

                    //Loggerien ID:n tallennus listaan jossa on pelkät laskutettavat duplikaatit
                    if (listOfDuplicatesHelpList.Contains(item.LoggerinSarjanumero))
                    {
                        if (listOfDuplicates.Contains(item.LoggerinSarjanumero))
                        {

                        }
                        else
                        {
                            listOfDuplicates.Add(item.LoggerinSarjanumero);
                            continue;
                        }
                    }
                    else
                    {
                        listOfDuplicatesHelpList.Add(item.LoggerinSarjanumero);
                    }

                }
            }

            List<string> allSerialNumbers = new List<string>();
            foreach (var item in TikruInfoTable)
            {
                allSerialNumbers.Add(item.LoggerinSarjanumero);
            }

            List<string> allSerialNumbersOnce = new List<string>();
            allSerialNumbersOnce = allSerialNumbers.Distinct().ToList();
            List<string> nonDuplicatesList = new List<string>();

            foreach (var item in allSerialNumbersOnce)
            {
                bool passWriting = false;

                foreach (var item2 in listOfDuplicates)
                {

                    if (item == item2)
                    {
                        passWriting = true;
                        break;
                    }

                }

                if (passWriting == false)
                {
                    nonDuplicatesList.Add(item);
                }

            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////                        


            // *** listOfDuplicates tallennetaan objekteina listaan ***
            List<TikruInfo> doubleFinal = new List<TikruInfo>();
            TikruInfo temporaryObject = new TikruInfo();

            foreach (var item in listOfDuplicates)
            {
                temporaryObject = null;

                foreach (var item2 in TikruInfoTable)
                {
                    if (item == item2.LoggerinSarjanumero)
                    {
                        if (temporaryObject != null)
                        {
                            if (temporaryObject.LaskutuksenAloitus < item2.LaskutuksenAloitus || temporaryObject.LaskutuksenAloitus == item2.LaskutuksenAloitus)
                            {
                                temporaryObject = item2;
                            }

                        }
                        if (temporaryObject == null)
                        {
                            temporaryObject = item2;
                        }

                    }
                }
                doubleFinal.Add(temporaryObject);

            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////


            // *** nonDuplicatesList tallennetaan objekteina listaan ***
            List<TikruInfo> oneFinal = new List<TikruInfo>();

            foreach (var item in nonDuplicatesList)
            {
                temporaryObject = null;

                foreach (var item2 in TikruInfoTable)
                {
                    if (item == item2.LoggerinSarjanumero)
                    {
                        if (temporaryObject != null)
                        {
                            if (temporaryObject.LaskutuksenAloitus < item2.LaskutuksenAloitus || temporaryObject.LaskutuksenAloitus == item2.LaskutuksenAloitus)
                            {
                                temporaryObject = item2;
                            }

                        }
                        if (temporaryObject == null)
                        {
                            temporaryObject = item2;
                        }

                    }
                }
                oneFinal.Add(temporaryObject);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////

            List<TikruInfo> semiFinal = new List<TikruInfo>();

            foreach (var item in doubleFinal)
            {
                semiFinal.Add(item);
            }
            foreach (var item in oneFinal)
            {
                semiFinal.Add(item);
            }


            //////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // *** Tässä semiFinal-listasta poistetaan asennukset joissa ei ole määritelty laskutuksen aloituspäivämäärää. Loput lisätään final-listaan ***
            List<TikruInfo> final = new List<TikruInfo>();

            foreach (var item in semiFinal)
            {

                DateTime a = DateTime.ParseExact("01-01-1900", "dd-MM-yyyy", CultureInfo.InvariantCulture);
                if (item.LaskutuksenAloitus > a)
                {
                    final.Add(item);
                }

            }

            return final;

        }












    }
}

