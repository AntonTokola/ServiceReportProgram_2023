using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace ServiceReportProgram
{
    public class AvaTrace
    {
        public int instrumentId { get; set; }
        public int identifier { get; set; }
        public double batteryMax { get; set; }
        public double temperature { get; set; }
        public string objectUUID { get; set; }
        public string model { get; set; }
        public string variant { get; set; }
        public string handlerName { get; set; }
        public string handlerUnitName { get; set; }
        public string batteryTimestamp { get; set; }
        public string imsi { get; set; }
        public string iccid { get; set; }
        public string signalQuality { get; set; }
        public string receivedSignalStrength { get; set; }
        public string calibrationExpiryDate { get; set; }
        public DateTime connectedAt { get; set; } 
        public bool enabled { get; set; }
        public bool hasAvanetErrors { get; set; }
        public bool stateDataNotFound { get; set; }
        public bool externalPower { get; set; }
        public bool ftpExports { get; set; }
        public string errorReport { get; set; } 

        public List<AvaErrors> avaErrors = new List<AvaErrors>();

        public List<AvaTrace> extractAvaJSONtoList(int count, JArray json)
        {
            List<AvaTrace> AvaTraceList = new List<AvaTrace>();

            for (int i = 0; i < count; i++)
            {
                //Tietojen purku jsonista AvaTrace ja AvaErrors-luokkien muuttujiin
                JObject jObject = (JObject)json[i];
                AvaTrace avaUnit = new AvaTrace();
                avaUnit.identifier = Convert.ToInt32(jObject["identifier"]);
                avaUnit.model = jObject["model"].ToString();                               
                avaUnit.hasAvanetErrors = Convert.ToBoolean(jObject["state"]["hasErrors"]);                
                avaUnit.instrumentId = Convert.ToInt32(jObject["instrumentId"]);
                avaUnit.objectUUID = jObject["objectUUID"].ToString();
                avaUnit.enabled = Convert.ToBoolean(jObject["enabled"]);
                avaUnit.externalPower = Convert.ToBoolean(jObject["externalPower"]);
                avaUnit.ftpExports = Convert.ToBoolean(jObject["ftpExports"]);
                avaUnit.handlerName = jObject["handlerName"].ToString();
                avaUnit.handlerUnitName = jObject["handlerUnitName"].ToString();

                if (avaUnit.identifier == 10676)
                {

                }

                if (jObject["variant"] != null)
                {
                    avaUnit.variant = jObject["variant"].ToString();
                }

                //Jos jsonista löytyy "state"-avain
                if (jObject.ContainsKey("state"))
                {
                    JObject stateFolder = (JObject)jObject["state"];

                    //Jos "state"-avaimen takana on tietoa, tallenna tiedot AvaTrace ja AvaErrors-luokkien muuttujiin
                    if (stateFolder.Count != 0)
                    {
                        try
                        {
                            if (jObject["state"]["connectedAt"] != null)
                            {
                                avaUnit.connectedAt = jObject["state"]["connectedAt"].Value<DateTime>();
                            }
                            if (jObject["state"]["battery"]["max"]["value"] != null)
                            {
                                avaUnit.batteryMax = jObject["state"]["battery"]["max"]["value"].Value<double>();
                            }

                            if (jObject["state"]["deviceSpecificInfo"]["receivedSignalStrength"] != null)
                            {
                                avaUnit.receivedSignalStrength = jObject["state"]["deviceSpecificInfo"]["receivedSignalStrength"].ToString();
                            }

                            if (jObject["state"]["deviceSpecificInfo"]["iccid"] != null)
                            {
                                avaUnit.iccid = jObject["state"]["deviceSpecificInfo"]["iccid"].ToString();
                            }

                            if (jObject["state"]["deviceSpecificInfo"]["imsi"] != null)
                            {
                                avaUnit.imsi = jObject["state"]["deviceSpecificInfo"]["imsi"].ToString();
                            }

                            if (jObject["state"]["deviceSpecificInfo"]["signalQuality"] != null)
                            {
                                avaUnit.signalQuality = jObject["state"]["deviceSpecificInfo"]["signalQuality"].ToString();
                            }

                            if (jObject["state"]["battery"]["temperature"]["value"] != null)
                            {
                                avaUnit.temperature = jObject["state"]["battery"]["temperature"]["value"].Value<double>();
                            }

                            if (jObject["state"]["battery"]["timestamp"] != null)
                            {
                                avaUnit.batteryTimestamp = jObject["state"]["battery"]["timestamp"].ToString();
                            }

                            if (jObject["calibrationExpiryDate"] != null)
                            {
                                avaUnit.calibrationExpiryDate = jObject["calibrationExpiryDate"].ToString();
                            }

                            if (avaUnit.hasAvanetErrors == true)
                            {
                                JArray errorsArray = jObject["state"]["errors"] as JArray;
                                if (errorsArray != null)
                                {
                                    foreach (var error in errorsArray)
                                    {
                                        AvaErrors avaError = new AvaErrors
                                        {
                                            // Korvaa alla olevat kentät vastaavilla AvaErrors-luokan kentillä.
                                            Code = error["code"].Value<int>(),
                                            EventIndex = error["eventIndex"].Value<int?>(),
                                            Name = error["name"].Value<string>(),
                                            Timestamp = error["timestamp"].Value<DateTime>(),
                                            Severity = error["severity"].Value<string>()
                                        };
                                        avaUnit.avaErrors.Add(avaError);
                                    }
                                }
                            }

                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Error with unit: " + avaUnit.identifier + ". State-instructions doesn't exist. Unit is not active in Avanet 2.0");
                            avaUnit.stateDataNotFound = true;
                        }
                        
                    }                
                }
                AvaTraceList.Add(avaUnit);
            }          
                        
            return AvaTraceList;
        }

        public List<AvaTrace> GetAvaErrors(List<AvaTrace> allAvaList)
        {


            List<AvaTrace> avaErrorsList = new List<AvaTrace> ();
            return avaErrorsList;
        }



    }

    public class AvaErrors
    {
        public int Code { get; set; }
        public int? EventIndex { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public string Severity { get; set; }
    }
}
