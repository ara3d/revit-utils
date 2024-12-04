using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RevitElementBipChecker.Model
{
    public static class JsonUtils
    {
        public static void WriteJson(this DataTable dataTable, out string path, string filename = "Report.json")
        {
            var PathDocument = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string namesave;
            if (filename.ToLower().Contains(".json"))
            {
                namesave = filename;
            }
            else
            {
                namesave = filename + ".json";
            }
            path = Path.Combine(PathDocument, namesave);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var serializeObject = JsonConvert.SerializeObject(dataTable,Formatting.Indented);
            File.WriteAllText(path, serializeObject);
        }


    }
}
