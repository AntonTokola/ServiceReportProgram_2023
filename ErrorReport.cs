using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ServiceReportProgram
{
    class ErrorReport
    {
        public List<TikruInfo> UnitErrorReport(List<SigicomFTP> sigicomErrors, List<AvaTrace> avaErrors, List<TikruInfo> Tikru)
        {
            List<TikruInfo> errorReportList = new List<TikruInfo>();
            TikruInfo errorReport = new TikruInfo();


            //Viallisten Sigicom-mittarien ja asennusten tietojen yhdistäminen (Tikruinfo / asennus-objektiin lisätään laitteen mallista riippuen vikaraportti-objekti)
            foreach (var sigicom_item in sigicomErrors)
            {

                foreach (var Tikru_item in Tikru)
                {
                    //string trimmedLoggerinSarjanumero = Tikru_item.LoggerinSarjanumero.Replace(@"-", "");
                    if (sigicom_item.sigicomId == Tikru_item.LoggerinSarjanumero)
                    {
                        errorReport = Tikru_item;
                        errorReport.sigicomErrorReport = sigicom_item;
                        errorReportList.Add(errorReport);
                        break;
                    }
                }


            }
            //Viallisten AVA-mittarien ja asennusten tietojen yhdistäminen (Tikruinfo / asennus-objektiin lisätään laitteen mallista riippuen vikaraportti-objekti)
            foreach (var ava_item in avaErrors)
            {
                foreach (var tikru_item in Tikru)
                {
                    string trimmed_AvaID = Convert.ToString(ava_item.identifier);
                    if (("AvaT-" + trimmed_AvaID) == tikru_item.LoggerinSarjanumero || ("bs-" + trimmed_AvaID) == tikru_item.LoggerinSarjanumero)
                    {
                        errorReport = tikru_item;
                        errorReport.AvaErrorReport = ava_item;
                        errorReportList.Add(errorReport);
                        break;
                    }
                }

            }


            return errorReportList;
        }
        // **************************** Listaa virheraportit listaan MITTARIASENTAJIEN perusteella ****************************
        public void forConsultants(List<TikruInfo> errorReportList)
        {
            List<string> nameList = new List<string>();
            List<TikruInfo> errorReports = new List<TikruInfo>();

            //Asentajien nimien erittely string-listaan
            foreach (var item in errorReportList)
            {
                if (item.Asentaja != "")
                {
                    if (!nameList.Contains(item.Asentaja))
                    {
                        nameList.Add(item.Asentaja);
                    }

                }
            }

            //Virheraporttien lisääminen uuteen listaan, jonka indeksien nimiksi annetaan asentajien/konsulttien nimet
            foreach (var item in nameList)
            {
                TikruInfo TikruInfo = new TikruInfo();
                List<TikruInfo> errorReportListByNames = new List<TikruInfo>();

                foreach (var item2 in errorReportList)
                {
                    if (item == item2.Asentaja)
                    {
                        errorReportListByNames.Add(item2);
                    }
                }
                TikruInfo.errorReportList = errorReportListByNames;
                TikruInfo.Asentaja = item;
                errorReports.Add(TikruInfo);
            }


            //Virheraporttien laadinta
            string report = "";
            foreach (var item in errorReports)
            {

                report = (report + ("Hei " + item.Asentaja + ", asentamasi mittarit saattavat vaatia huoltoa." + System.Environment.NewLine + System.Environment.NewLine));

                foreach (var item2 in item.errorReportList)
                {
                    if (item2.sigicomErrorReport != null)
                    {

                        report = (report + item2.sigicomErrorReport.errorReport);
                        report = (report + "- Projekti: " + item2.ProjektinNimi + System.Environment.NewLine);
                        report = (report + "- Mittarin sijainti: " + item2.MittarinSijainti + System.Environment.NewLine);
                        report = (report + "- Laskutuksen aloituspäivämäärä: " + item2.LaskutuksenAloitus + System.Environment.NewLine);
                        report = (report + "----------------------------------------------------------------------------");
                        report = (report + System.Environment.NewLine + System.Environment.NewLine);
                    }
                    if (item2.AvaErrorReport != null)
                    {
                        report = (report + item2.AvaErrorReport.errorReport);
                        report = (report + "- Projekti: " + item2.ProjektinNimi + System.Environment.NewLine);
                        report = (report + "- Mittarin sijainti: " + item2.MittarinSijainti + System.Environment.NewLine);
                        report = (report + "- Laskutuksen aloituspäivämäärä: " + item2.LaskutuksenAloitus + System.Environment.NewLine);
                        report = (report + "----------------------------------------------------------------------------");
                        report = (report + System.Environment.NewLine + System.Environment.NewLine);
                    }

                }

                report = (report + "****************************************************************************" + System.Environment.NewLine + System.Environment.NewLine + System.Environment.NewLine);


            }
            //Log-tiedoston luonti. Logi sisältää kaikki huoltoraportit.
        string dir = @"C:\Users\fortokan\OneDrive - Forcit Oy\Työpöytä\UNIT SERVICE-REPORT\ErrorReportLog\";
        string logName = ("ErrorReportLog_forConsultants" + DateTime.Now);
        Console.WriteLine();

        //Tallennus "dir" polkuun
        System.IO.File.WriteAllText(dir + logName + ".txt", report);
        Console.WriteLine(report);
        Console.WriteLine();
        }


        // **************************** Listaa virheraportit listaan PROJEKTIEN VETÄJIEN perusteella ****************************
        public void forProjectOwners(List<TikruInfo> errorReportList)
        {
            List<string> nameList = new List<string>();
            List<TikruInfo> errorReports = new List<TikruInfo>();

            //Projektinvetäjien nimien erittely string-listaan
            foreach (var item in errorReportList)
            {
                if (item.HuoltoraportinVastaanottaja != "")
                {
                    if (!nameList.Contains(item.HuoltoraportinVastaanottaja))
                    {
                        nameList.Add(item.HuoltoraportinVastaanottaja);
                    }

                }
            }

            //Virheraporttien selaaminen ja lisääminen uuteen listaan, jonka indeksien nimiksi annetaan projektin vetäjien nimet
            foreach (var item in nameList)
            {
                TikruInfo TikruInfo = new TikruInfo();
                List<TikruInfo> errorReportListByNames = new List<TikruInfo>();

                foreach (var item2 in errorReportList)
                {
                    if (item == item2.HuoltoraportinVastaanottaja)
                    {
                        errorReportListByNames.Add(item2);
                    }
                }
                TikruInfo.errorReportList = errorReportListByNames;
                TikruInfo.HuoltoraportinVastaanottaja = item;
                errorReports.Add(TikruInfo);
            }


            //Virheraporttien laadinta
            string report = "";
            foreach (var item in errorReports)
            {

                report = (report + ("Hei " + item.HuoltoraportinVastaanottaja + ", projekteillasi olevat mittarit saattavat vaatia huoltoa." + System.Environment.NewLine + System.Environment.NewLine));

                foreach (var item2 in item.errorReportList)
                {
                    //SIGICOM RAPORTIN LUONTI
                    if (item2.sigicomErrorReport != null)
                    {
                        report = (report + item2.sigicomErrorReport.errorReport);

                        if (item2.MittauksestaVastaava != null && item2.MittauksestaVastaava != "" && item2.MittauksestaVastaava != item2.ProjektinVetaja)
                        {
                            report = (report + "Sinut on merkitty Tikruun mittauksesta vastaavaksi tälle projektille." + System.Environment.NewLine);
                        }
                        report = (report + "- Projekti: " + item2.ProjektinNimi + System.Environment.NewLine);
                        report = (report + "- Mittarin sijainti: " + item2.MittarinSijainti + System.Environment.NewLine);
                        report = (report + "- Laskutuksen aloituspäivämäärä: " + item2.LaskutuksenAloitus + System.Environment.NewLine);

                        if (item2.MittauksestaVastaava != null && item2.MittauksestaVastaava != "" && item2.MittauksestaVastaava != item2.ProjektinVetaja)
                        {
                            report = (report + "- Projektista vastaava: " + item2.ProjektinVetaja + System.Environment.NewLine);
                        }

                        report = (report + "- Mittarin asentaja: " + item2.Asentaja + System.Environment.NewLine);

                        if (item2.AsennuksenLisatiedot != "-")
                        {
                            report = (report + "- Mittariasennuksen lisätiedot: " + item2.AsennuksenLisatiedot + System.Environment.NewLine);
                        }

                        report = (report + "----------------------------------------------------------------------------");
                        report = (report + System.Environment.NewLine + System.Environment.NewLine);
                    }

                    // AVA RAPORTIN LUONTI
                    if (item2.AvaErrorReport != null)
                    {
                        report = (report + item2.AvaErrorReport.errorReport); //report = (report + item2.AvaErrorReport.errorReport);

                        if (item2.MittauksestaVastaava != null && item2.MittauksestaVastaava != "" && item2.MittauksestaVastaava != item2.ProjektinVetaja)
                        {
                            report = (report + "Sinut on merkitty Tikruun mittauksesta vastaavaksi tälle projektille." + System.Environment.NewLine);
                        }

                        report = (report + "- Projekti: " + item2.ProjektinNimi + System.Environment.NewLine);
                        report = (report + "- Mittarin sijainti: " + item2.MittarinSijainti + System.Environment.NewLine);
                        report = (report + "- Laskutuksen aloituspäivämäärä: " + item2.LaskutuksenAloitus + System.Environment.NewLine);

                        if (item2.MittauksestaVastaava != null && item2.MittauksestaVastaava != "" && item2.MittauksestaVastaava != item2.ProjektinVetaja)
                        {
                            report = (report + "- Projektista vastaava: " + item2.ProjektinVetaja + System.Environment.NewLine);
                        }

                        report = (report + "- Mittarin asentaja: " + item2.Asentaja + System.Environment.NewLine);

                        if (item2.AsennuksenLisatiedot != "-")
                        {
                            report = (report + "- Mittariasennuksen lisätiedot: " + item2.AsennuksenLisatiedot + System.Environment.NewLine);
                        }

                        report = (report + "----------------------------------------------------------------------------");
                        report = (report + System.Environment.NewLine + System.Environment.NewLine);
                    }

                }

                report = (report + "****************************************************************************" + System.Environment.NewLine + System.Environment.NewLine + System.Environment.NewLine);


            }
            //Log-tiedoston luonti. Logi sisältää kaikki huoltoraportit.
        string dir = @"C:\Users\fortokan\OneDrive - Forcit Oy\Työpöytä\UNIT SERVICE-REPORT\ErrorReportLog\";
            //string dir = @"\ServiceReportsForProjectOwners";
            //string dir = @"C:\Users\fortokan\Desktop\UNIT SERVICE-REPORT\ErrorReportLog\";

            string logName = ("ErrorReportLog_ForProjectOwners_" + DateTime.Now);
            Console.WriteLine();
            //Tallennus "dir" polkuun
            System.IO.File.WriteAllText(dir + logName + ".txt", report);

            //Tallennus projektin juurihakemistoon
            //System.IO.File.WriteAllText(@"ReportsForProjectOwners\" + logName + ".txt", report);

            //Avaa ProjectOwner log-tiedosto Notepadilla
            try
            {
                System.Diagnostics.Process.Start(dir + logName + ".txt");
            }
            catch (Exception)
            {

                Console.WriteLine("ProjectOwner log-tiedoston avaaminen ei onnistunut.");
            }

            Console.WriteLine(report);
            Console.WriteLine();
        }

    }
}
