using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace IbKodboxCrawler
{
    class Program
    {
        static HttpClient http = new HttpClient();
        static string baseUrl, shareId;
        static int count = 0;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Base url: (e.g. http://file.example.com/index.php)");
            baseUrl = Console.ReadLine();
            Console.WriteLine("Share ID: (e.g. 1AbcD23E)");
            shareId = Console.ReadLine();
            Console.WriteLine("Path: (e.g. / , /My Files/ )");
            string path = Console.ReadLine();
            Console.WriteLine();

            Directory.CreateDirectory("lists");
            await QueryFolder($"{{shareItemLink:{shareId}}}{path}", ".");
            Console.WriteLine("\nRemember to chcp 65001 if you execute them as a bat file.");
        }

        static async Task QueryFolder(string path, string pathname)
        {
            var content = new FormUrlEncodedContent(new[]{
                new KeyValuePair<string, string>("path", path),
                new KeyValuePair<string, string>("page", "1"),
                new KeyValuePair<string, string>("pageNum", "10000"),  //#TODO
                new KeyValuePair<string, string>("API_ROUTE", "explorer/share/pathList"),
            });
            //Console.WriteLine(content.ReadAsStringAsync().Result);
            var response = await http.PostAsync($"{baseUrl}?explorer/share/pathList&shareID={shareId}", content);

            JObject o = JObject.Parse(await response.Content.ReadAsStringAsync());
            count++;
            string urlspath = $"lists\\{count}.txt";
            using (StreamWriter file = new StreamWriter(urlspath))
            {
                foreach (var token in o.SelectTokens("data.fileList..path"))
                {
                    //Console.WriteLine(token);
                    file.WriteLine($"{baseUrl}?explorer/share/fileDownload&shareID={shareId}&path={HttpUtility.UrlEncode(token.ToString())}");
                }
            }
            Console.WriteLine(@$"aria2c --console-log-level=warn -i {count}.txt -d ""{pathname}""");  //"" is needed to protect special characters

            foreach (var token in o.SelectToken("data.folderList").Children())
            {
                string folderPath = token["path"].ToString();
                string folderName = token["name"].ToString();
                await QueryFolder(folderPath, $"{pathname}/{folderName}");
            }
        }
    }
}