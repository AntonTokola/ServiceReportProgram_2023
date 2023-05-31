using System.Collections.Generic;
using System.IO;

namespace ServiceReportProgram
{

    class HelpFunctions
    {
        //Hakemisto tallennettaville objekteille
        string dir = @"C:\Users\fortokan\OneDrive - Forcit Oy\Työpöytä\UNIT SERVICE-REPORT\TemporaryFiles";
        //string dir = @"C:\Users\fortokan\source\repos\ConsoleApp1\ConsoleApp1\bin\Debug\netcoreapp3.1\TemporaryFiles";
        //C:\Users\fortokan\OneDrive - Forcit Oy\Työpöytä\UNIT SERVICE-REPORT
        //Funktio etsii ohjelman hakemistosijainnin (hakemistojen tietoja tarvitaan log-kansioiden luontia varten)
        public string getPath()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directory = System.IO.Path.GetDirectoryName(path);
            return directory;
        }



        //Save local Sigicom-list .bin file
        public void CreateSigicomObjectFile(List<SigicomFTP> sigicomList, string fileName)
        {

            string serializationFile = Path.Combine(dir, (fileName + ".bin"));
            //serialize
            using (Stream stream = File.Open(serializationFile, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                bformatter.Serialize(stream, sigicomList);
            }

        }

        //Open local Sigicom-list .bin file
        public List<SigicomFTP> OpenSigicomObjectFile(string fileName)
        {
            string serializationFile = Path.Combine(dir, fileName + ".bin");
            List<SigicomFTP> returnSavedList = new List<SigicomFTP>();


            //deserialize
            using (Stream stream = File.Open(serializationFile, FileMode.Open))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                List<SigicomFTP> openSavedList = (List<SigicomFTP>)bformatter.Deserialize(stream);
                returnSavedList = openSavedList;
            }



            return returnSavedList;
        }

        public void CreateStringFile(string stringFile, string fileName)
        {
            string serializationFile = Path.Combine(dir, (fileName + ".bin"));

            //serialize
            using (Stream stream = File.Open(serializationFile, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                bformatter.Serialize(stream, stringFile);
            }

        }

        //Open local Sigicom-list .bin file
        public string OpenStringFile(string fileName)
        {
            string serializationFile = Path.Combine(dir, fileName + ".bin");
            string returnSavedString = "";

            //deserialize
            using (Stream stream = File.Open(serializationFile, FileMode.Open))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                string openSavedString = (string)bformatter.Deserialize(stream);
                returnSavedString = openSavedString;
            }
            return returnSavedString;
        }
    }
}