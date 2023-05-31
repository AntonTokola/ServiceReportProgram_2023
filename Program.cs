using System;
using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceReportProgram;
using PuppeteerSharp.Input;

class Program
{
    static async Task Main(string[] args)
    {
        //***** "AVA" MITTALAITTEIDEN TIETOJEN HAKU AVANET-JÄRJESTELMÄSTÄ *****

        Console.WriteLine("Syötä Avanet käyttäjätunnus, ja paina enter:");        
        string avaUserName = Console.ReadLine();

        Console.WriteLine("Syötä Avanet salasana, ja paina enter:");        
        var avaPassword = string.Empty;
        ConsoleKey key;
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && avaPassword.Length > 0)
            {
                Console.Write("\b \b");
                avaPassword = avaPassword[0..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("*");
                avaPassword += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);
        Console.WriteLine();
        Console.WriteLine("Aloitetaan kirjautumista..");
        

        var fetcher = new BrowserFetcher();
        var revision = BrowserFetcher.DefaultRevision;
        string executablePath = fetcher.GetExecutablePath(revision);

        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
        {
            await fetcher.DownloadAsync(revision);
            executablePath = fetcher.GetExecutablePath(revision);
        }

        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = executablePath
        });

        try
        {
            // Luo uusi Page-olio ja määritä tapahtumankäsittelijä, joka nappaa tokenin verkkopyynnöstä
            Page page = await browser.NewPageAsync();
            string token = null;
            string jwtAssertion = null;

            page.Request += (sender, e) =>
            {
                if (e.Request.Headers.TryGetValue("Authorization", out string authorizationHeader) &&
                    authorizationHeader.StartsWith("Bearer "))
                {
                    token = authorizationHeader.Substring("Bearer ".Length);
                }
                if (e.Request.Headers.TryGetValue("x-jwt-assertion", out string jwtAssertionHeader))
                {
                    jwtAssertion = jwtAssertionHeader;
                }
            };

            //var page = await browser.NewPageAsync();
            Console.WriteLine("Täytetään Avanet 2.0 kirjautumislomakkeita..");            
            await page.WaitForTimeoutAsync(2000);
            await page.GoToAsync("https://avanet.avamonitoring.net", WaitUntilNavigation.Networkidle0);
            Console.WriteLine("URL osoite lisätty..");
            await page.WaitForTimeoutAsync(2000);
            await page.WaitForSelectorAsync("#identifier.form-control");
            Console.WriteLine("Lisätään tunnuksia..");
            await page.TypeAsync("#identifier.form-control", avaUserName);
            await page.WaitForSelectorAsync("#password.form-control");
            await page.TypeAsync("#password.form-control", avaPassword);
            Console.WriteLine("Kirjautumislomakkeet täytetty..");
            await page.WaitForSelectorAsync("input#accept.btn.btn-primary.btn-block");
            await page.FocusAsync("input#accept.btn.btn-primary.btn-block");
            Console.WriteLine("Kirjaudutaan Avanetiin..");
            var clickTask = page.ClickAsync("input#accept.btn.btn-primary.btn-block");
            var timeoutTask = Task.Delay(10000); // 10 sekuntia
            await Task.WhenAny(clickTask, timeoutTask);
            await page.WaitForTimeoutAsync(4000);            
            var homePage = await page.EvaluateFunctionAsync<string>("() => document.documentElement.outerHTML");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "fi-FI,fi;q=0.9,en-US;q=0.8,en;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://avanet.avamonitoring.net");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://avanet.avamonitoring.net/");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
            if (jwtAssertion != null)
            {
                httpClient.DefaultRequestHeaders.Add("x-jwt-assertion", jwtAssertion);
            }

            // Tee API-kutsu käyttämällä httpClient-oliota
            List<dynamic> jsonResultList = new List<dynamic>();

            HttpResponseMessage response = await httpClient.GetAsync("https://gateway.k8s-prod.avamonitoring.net/devicemgmt/v1/instruments?limit=100&offset=100&sort=state.connected_at&descending=true");
            if (Convert.ToString(response.StatusCode) == "OK")
            {
                Console.WriteLine("Kirjautuminen onnistui.");
            }
            else
            {
                Console.WriteLine("Kirjautuminen Avanetiin ei onnistunut. Yritä kirjautumista uudelleen. (Error: " + response.StatusCode + ")");
                Console.WriteLine("Paina mitä tahansa näppäintä lopettaaksesi.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            //Tietojen haku Avanet-järjestelmästä. JSON-tietojen tallennus listaan.
            int pages = 100;
            while (pages < 2000)
            {
                response = await httpClient.GetAsync("https://gateway.k8s-prod.avamonitoring.net/devicemgmt/v1/instruments?limit=100&offset="+pages+"&sort=state.connected_at&descending=true");
                Console.WriteLine("Ladataan JSON-listaa: " + pages);
                dynamic responseBody = await response.Content.ReadAsStringAsync();
                jsonResultList.Add(responseBody);
                pages = pages + 100;
                await Task.Delay(500);
            }

            List<AvaTrace> allAvaUnits = new List<AvaTrace>();

            foreach (dynamic dynItem in jsonResultList)
            {
                //Parserointi, jotta indeksien määrä voidaan laskea ja syöttää "extractJSONtoList"-metodille
                JObject itemsObject = JObject.Parse(dynItem.ToString());
                JArray itemsArray = itemsObject["items"] as JArray;
                int resultCount = itemsArray.Count;

                //Pura JSON sisältö "extractJSONtoList"-metodilla AvaTrace-objektiin. Lopuksi objektien lisäys "allAvaUnits"-listaan
                AvaTrace avaTrace = new AvaTrace();
                List<AvaTrace> avaTraceList = new List<AvaTrace>();
                avaTraceList = avaTrace.extractAvaJSONtoList(resultCount, itemsArray);

                foreach (var item2 in avaTraceList)
                {
                    allAvaUnits.Add(item2);
                }
            }           

            List<AvaTrace> avaErrorsList = new List<AvaTrace>();
            HelpFunctions helpFunctions = new HelpFunctions();
            TikruInfo tikruInfo = new TikruInfo();
            SigicomFTP sigicomFTP = new SigicomFTP();
            LoggerErrors loggerErrors = new LoggerErrors();
            ErrorReport errorReport = new ErrorReport();

            //Avanet JSON-listasta eritellään ja tallennetaan ainoastaan vialliset mittarit
            avaErrorsList = loggerErrors.GetAvaErrors(allAvaUnits);



            //***** PROJEKTI- JA LASKUTUSTIETOJEN HAKU CRM-JÄRJESTELMÄSTÄ *****

            //"TikruInfoTable"-oliotaulukko Tikruinfoja varten. (Taulukon indeksit määräytyvät TikruInfo-luokan "CountTikruRecords"-metodin kautta.)
            TikruInfo[] tikruInfoTable = new TikruInfo[0];
            // "ListWithoutDuplicates"-oliolista Tikruinfoja varten. (Taulukon tiedot palautuvat "FindDuplicates"-metodilta = tupla laskutettavat mittarit jäävät pois.)
            List<TikruInfo> tikruInfoList = new List<TikruInfo>();

            bool tikruAccess = false;

            while (tikruAccess != true)
            {
                try
                {
                    tikruInfoTable = tikruInfo.GetTikruInfo();
                    tikruAccess = true;
                }
                catch (Exception)
                {
                    tikruAccess = false;
                }
            }
            int tikruInfoCountRecords = tikruInfoTable.Count();
            //Etsii mittarien "duplikaatit" Tikrun/ERP:n tietueista
            tikruInfoList = tikruInfo.FindDuplicates(tikruInfoTable, tikruInfoCountRecords);
            Console.WriteLine();



            //***** "SIGICOM" MITTALAITTEIDEN TIETOJEN HAKU FTP-PALVELIMELTA *****

            //Kaikkien palvelimella olevien Sigicom-mittarien lista
            List<SigicomFTP> sigicomList = new List<SigicomFTP>();
            sigicomList = sigicomFTP.SaveStatusFiles();

            //Viallisten Sigicom-mittarien lista ("GetSigicomErrors"-metodi palauttaa listan, johon on tallennettu vialliset Sigicom mittarit.)
            List<SigicomFTP> sigicomErrors = new List<SigicomFTP>();
            sigicomErrors = loggerErrors.GetSigicomErrors(sigicomList);


            //***** VIKARAPORTTIEN KOKOAMINEN *****

            //Asennusten ja mittareiden tiedot kattavan vikaraportin luonti ErrorReport-luokassa            
            tikruInfoList = errorReport.UnitErrorReport(sigicomErrors, avaErrorsList, tikruInfoList);
            //Tuloksien tallennus log-tiedostoihin (viestit asentajille ja projektien vetäjille)
            errorReport.forConsultants(tikruInfoList);
            errorReport.forProjectOwners(tikruInfoList);
            Console.WriteLine("Mittarien vikaLista on luotu. Lopeta painamalla enter.");
            Console.ReadLine();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            await browser.CloseAsync();
        }
    }
}
