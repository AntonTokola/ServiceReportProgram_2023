using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ServiceReportProgram
{
    class LoggerErrors
    {
        //Viallisten Sigicom-mittarien määrittely vikakoodien perusteella. Vialliset mittarit palautetaan listana.
        public List<SigicomFTP> GetSigicomErrors(List<SigicomFTP> SigicomLIST)
        {
            //try
            //{
            //Lista johon kerätään huoltoa vaativat Sigicom mittarit
            List<SigicomFTP> sigicomErrors = new List<SigicomFTP>();
            Settings settings = new Settings();
            string sigicomErrorReport = "";
            string printError = "";



            foreach (var item in SigicomLIST)
            {

                sigicomErrorReport = "";
                double batteryPercent = -1;
                double temperature = 0;
                double voltage = -1;
                bool loggerError = false;

                //BatteryPercent
                if (item.D10_BatteryPercent != "")
                {

                    batteryPercent = double.Parse(item.D10_BatteryPercent, System.Globalization.CultureInfo.InvariantCulture);
                }
                //Temperature
                if (item.D10_C12_Temperature != null)
                {

                    string S1 = item.D10_C12_Temperature;
                    try
                    {
                        if (S1.Length > 4)
                        {
                            S1 = item.D10_C12_Temperature.Substring(0, 4);
                        }
                    }
                    catch (Exception)
                    {

                        Console.WriteLine("IM Temperature error!");
                    }


                    temperature = double.Parse(S1, System.Globalization.CultureInfo.InvariantCulture);
                }
                //Voltage
                if (item.IM_Voltage != null)
                {
                    string V1 = item.IM_Voltage;
                    string V2 = V1;

                    try
                    {
                        if (V1.Length > 5)
                        {
                            V2 = V1.Substring(0, 5);
                        }
                    }
                    catch (Exception)
                    {

                        Console.WriteLine("IM Voltage error!");
                    }

                    voltage = double.Parse(V2, System.Globalization.CultureInfo.InvariantCulture);
                }


                printError = ("Laite: " + item.sigicomId + " on antanut virheilmoituksen järjestelmään." + System.Environment.NewLine + "Diagnoosi: " + System.Environment.NewLine);

                //Huoltoa vaativien mittarien määrittely ja tallentaminen

                //Connection missed = jos mittari ei ole ottanut yhteyttä viimeiseen vuorokauteen.
                if (item.connectionMissed == true)
                {
                    sigicomErrorReport = (sigicomErrorReport + "[CONNECTION MISSED] - " + settings.setConnectionMissedInfo + " Viimeisin yhteydenottoaika: " + Convert.ToString(item.statusCreated) + System.Environment.NewLine);
                    loggerError = true;
                }

                //Laitteen virta määrän määrittely = onko mittarin malli IM, C12 vai D10
                //C12 = IMxxxxx
                if (item.sigicomId.Contains("C12"))
                {
                    if (item.IM_Voltage != null && voltage < 3.5)
                    {
                        sigicomErrorReport = (sigicomErrorReport + "[BATTERY LOW] - Laitteen virta on vähissä. Jäljellä oleva virta: " + Convert.ToString(voltage) + "V" + System.Environment.NewLine);
                        loggerError = true;
                    }
                }

                //D10 = IMxxxxxx
                if (item.sigicomId.Contains("D10"))
                {
                    if (item.IM_Voltage != null && voltage < 11.7)
                    {
                        sigicomErrorReport = (sigicomErrorReport + "[BATTERY LOW] - Laitteen virta on vähissä. Jäljellä oleva virta: " + Convert.ToString(voltage) + "V" + System.Environment.NewLine);
                        loggerError = true;
                    }
                }

                //IM = IMxxxx
                if (item.sigicomId.Contains("IM") || item.sigicomId.Contains("ABE"))
                {
                    if (item.IM_Voltage != null && voltage < 11.7)
                    {
                        sigicomErrorReport = (sigicomErrorReport + "[BATTERY LOW] - Laitteen virta on vähissä. Jäljellä oleva virta: " + Convert.ToString(voltage) + "V" + System.Environment.NewLine + "(Mikäli laitteen virta on vain paristojen varassa, paristojen vaihtoa suositellaan lähiaikoina.)" + System.Environment.NewLine);
                        loggerError = true;
                    }
                }

                //Akun varaus prosentteina (tämä ominaisuus vain Sigicom-D10 mittareissa.
                //Ilmoitetaan vikaraportissa vain silloin kun prosenttimäärä on alle 20%.)

                if (item.D10_BatteryPercent != "0.0" && batteryPercent < 15 && batteryPercent > -1)
                {
                    if (item.D10_BatteryPercent != null && batteryPercent < 15 && batteryPercent > -1)
                    {
                        if (item.D10_BatteryPercent != "" && batteryPercent < 15 && batteryPercent > -1)
                        {
                            sigicomErrorReport = (sigicomErrorReport + "Akkujen varausprosentti: " + Convert.ToString(batteryPercent) + "%" + System.Environment.NewLine + "(Mikäli laitteen virtaa on jäljellä alle 15%, Sigicom D10-akkujen vaihtoa suositellaan lähiaikoina.)" + System.Environment.NewLine);
                            loggerError = true;
                        }
                    }

                }
                if (item.D10_BatteryPercent == "0.0")
                {
                    sigicomErrorReport = (sigicomErrorReport + "Akkujen varausprosentti: " + Convert.ToString(batteryPercent) + "%" + System.Environment.NewLine + "(Mittari on sammunut.)" + System.Environment.NewLine);
                    loggerError = true;
                }

                //Nodelost = laitteen ja anturin välillä on yhteyshäiriö
                if (item.nodeLost == true)
                {
                    sigicomErrorReport = (sigicomErrorReport + "[NODE LOST] - Mittarin ja anturin välillä on yhteyshäiriö." + System.Environment.NewLine + "(Tarkista että datakaapeli on paikallaan ja kunnossa. Tarkista myös että mittarin ja anturin liittimet ovat ehjät.)" + System.Environment.NewLine);
                    loggerError = true;
                }

                //Memory low = jos muisti on alle 20mb
                if (item.memoryLow == true)
                {
                    string availableSpace = Convert.ToString(item.availableSpace);
                    //Low memory / availableSpace. Tietueen lopusta poistetaan 6 merkkiä (lopullinen MB tulos kahden desimaalin tarkkuudella).
                    availableSpace = availableSpace.Substring(0, availableSpace.Length - 6);

                    sigicomErrorReport = (sigicomErrorReport + "[MEMORY LOW] - Laitteen muisti on vähissä. Muistia on jäljellä: " + availableSpace + "MB." + System.Environment.NewLine + "(Mikäli muistia on alle 20MB, muistin tyhjentämistä/muistikortin vaihtoa suositellaan lähiaikoina.)" + System.Environment.NewLine);
                    loggerError = true;
                }
                //Temperature = liian alhaisen tai korkean lämpötilan määrittely (-35 tai yli 40 astetta)
                if (item.D10_C12_Temperature != null && temperature < -30 || temperature > 40)
                {
                    if (temperature < -30)
                    {
                        sigicomErrorReport = (sigicomErrorReport + "Laitteen lämpötila saattaa olla liian alhainen: -" + Convert.ToString(temperature) + "°C" + System.Environment.NewLine);
                    }
                    if (temperature > 40)
                    {
                        sigicomErrorReport = (sigicomErrorReport + "Laitteen lämpötila saattaa olla liian korkea: +" + Convert.ToString(temperature) + "°C" + System.Environment.NewLine);
                    }
                    loggerError = true;
                }

                //Huoltoraportti lisätään olioon, ja olio huollettavien asennusten/mittarien taulukkoon
                if (loggerError)
                {
                    printError = (printError + sigicomErrorReport + System.Environment.NewLine);
                    item.errorReport = printError;
                    //Huoltoraportti lisätään listaan
                    sigicomErrors.Add(item);
                }
            }
            return sigicomErrors;
        }

        //Viallisten AvaTrace-mittarien määrittely vikakoodien perusteella. Vialliset mittarit palautetaan listana.
        public List<AvaTrace> GetAvaErrors(List<AvaTrace> AvaLIST)
        {
            List<AvaTrace> avaErrors = new List<AvaTrace>();
            Settings settings = new Settings();
            double batteryLimit = 6.5;
            

            foreach (var item in AvaLIST)
            {
                if (item.handlerUnitName == "Forcit Consulting Oy FI")
                {
                    if (item.hasAvanetErrors == true || item.batteryMax < batteryLimit)
                    {
                        if (item.stateDataNotFound != true)
                        {

                            bool containsError = false;
                            int countAvaErrors = item.avaErrors.Count();
                            string selfTestFailure = "";
                            string avaErrorReport = "";
                            string avaErrorReportInfo = ("Laite: " + item.model + " (" + item.identifier + ") on antanut virheilmoituksen järjestelmään." + System.Environment.NewLine + "Diagnoosi:" + System.Environment.NewLine);

                            for (int i = 0; i < countAvaErrors; i++)
                            {

                                //Jos laitteessa on Avanetin välinen yhteysongelma, ja yhteysongelma on muodostunut kaksi vuorokautta aikaisemmin.
                                //if (item.avaErrors[i].Name == "Missing device connection" && item.avaErrors[i].Timestamp < (DateTime.Today.AddDays(-2)))
                                //{
                                //    AvaErrorReport += ("(Missing device connection) - Mittari ei ole ottanut yhteyttä kahteen vuorokauteen. Viimeisin yhteydenottoaika: " + item.avaErrors[i].Timestamp + System.Environment.NewLine);
                                //    containsError = true;
                                //}
                                if (item.connectedAt < (DateTime.Today.AddDays(-2)) && !avaErrorReport.Contains("CONNECTION MISSED"))
                                {
                                    avaErrorReport += ("[CONNECTION MISSED] - Mittari ei ole ottanut yhteyttä Avanetiin kahteen vuorokauteen."+ System.Environment.NewLine + "Viimeisin yhteydenottoaika: " + item.connectedAt + System.Environment.NewLine);
                                    containsError = true;
                                }

                                //Jos laitteessa on FTP-yhteysongelma, ja yhteysongelma on muodostunut vuorokautta aikaisemmin
                                if (item.avaErrors[i].Name == "Ftp upload failed" && item.avaErrors[i].Timestamp < (DateTime.Today.AddDays(-1)) && !avaErrorReport.Contains("FTP UPLOAD FAILED"))
                                {
                                    avaErrorReport += ("[FTP UPLOAD FAILED] - Mittari ei ole ottanut yhteyttä FTP-palvelimeen vuorokauteen. Viimeisin yhteydenottoaika: " + item.avaErrors[i].Timestamp + System.Environment.NewLine);
                                    containsError = true;
                                }

                                //Self test failuren määrittely (laitteen ja anturin välinen vika)
                                if (item.avaErrors[i].Name.Contains("Self test channel"))
                                {
                                    if (item.avaErrors[i].Name == ("Self test channel 1"))
                                    {
                                        selfTestFailure += "- CH: 1 (vaaka-suunta ei toimi)" + System.Environment.NewLine;
                                    }
                                    if (item.avaErrors[i].Name == ("Self test channel 2"))
                                    {
                                        selfTestFailure += "- CH: 2 (pysty-suunta ei toimi)" + System.Environment.NewLine;
                                    }
                                    if (item.avaErrors[i].Name == ("Self test channel 3"))
                                    {
                                        selfTestFailure += "- CH: 3 (pituus-suunta ei toimi)" + System.Environment.NewLine;
                                    }
                                    containsError = true; 

                                }
                                if (i == (countAvaErrors - 1) && selfTestFailure != "")
                                {
                                    avaErrorReport += ("[SELF TEST FAILURE] - viimeisin ajankohta kun vika on havaittu: " + item.avaErrors[i].Timestamp + "." + System.Environment.NewLine + "Laitteen ja anturin välillä on häiriö seuraavissa kanavissa: " + System.Environment.NewLine + selfTestFailure);
                                    containsError = true;
                                }

                            }

                            if (item.batteryMax < batteryLimit && item.batteryMax >= 1)
                            {
                                    avaErrorReport += ("[BATTERY LOW] - Mittarin paristot ovat vähissä. Paristojen varaus: " + item.batteryMax + "V" + System.Environment.NewLine);
                               
                                containsError = true;
                            }

                            //Yllä luodun raporttikokonaisuuden lisääminen listaan.
                            if (containsError)
                            {
                                item.errorReport = avaErrorReportInfo + avaErrorReport + System.Environment.NewLine;
                                avaErrors.Add(item);
                            }                       
                        }
                    }
                }              

            }
            return avaErrors;
        }
    }
}
