using System;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Reflection;

namespace TownLibrarian
{
    public static class ModuleDatabase
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "ATT Meta Bot";

        //this is literally google's example code
        public static List<ModuleData> init()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("./Credentials/Credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "Token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine(DateTime.Now + ": Credential file saved to: " + credPath);

            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String spreadsheetId = "1mhEJL32ovYawQOHNo3XnmaBlmbt0HtF-Z2BVpGimPEU";
            //start at second row to ignore header text
            String range = "Help Module Data!A2:AG";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            List<ModuleData> ResultData = new List<ModuleData>();

            // https://docs.google.com/spreadsheets/d/1mhEJL32ovYawQOHNo3XnmaBlmbt0HtF-Z2BVpGimPEU/edit
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    ModuleData data = new ModuleData();
                    //easy way to populate each field of a class by crawling along it
                    int i = 0;
                    foreach (FieldInfo val in typeof(ModuleData).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        //damn you inconsistent zero based thingies
                        if (row.Count - 1 < i)
                        {
                            break;
                        }

                        val.SetValue(data, ParseSpecialChars(row[i]));
                        // I know I know (get it lol) i could use a for loop but i like doing this
                        i++;
                    }

                    ResultData.Add(data);
                }

            }
            else
            {
                //argh errories
            }
            return ResultData;
        }

        private static object ParseSpecialChars(object thing)
        {
            if (thing is string)
            {
                string text = (string)thing;
                if (text.Contains("\\n"))
                {
                    text = text.Replace("\\n", "\n");
                }
                return text;
            }
            else
            {
                return thing;
            }

        }



        public class ModuleData
        {
            public string Trigger;
            public string Title;
            public string Footer;
            public string Field_1_Name;
            public string Field_1_Data;
            public string Field_2_Name;
            public string Field_2_Data;
            public string Field_3_Name;
            public string Field_3_Data;
            public string Field_4_Name;
            public string Field_4_Data;
            public string Field_5_Name;
            public string Field_5_Data;
            public string Field_6_Name;
            public string Field_6_Data;
            public string Field_7_Name;
            public string Field_7_Data;
            public string Field_8_Name;
            public string Field_8_Data;
            public string Field_9_Name;
            public string Field_9_Data;
            public string Field_10_Name;
            public string Field_10_Data;
            public string Field_11_Name;
            public string Field_11_Data;
            public string Field_12_Name;
            public string Field_12_Data;
            public string Field_13_Name;
            public string Field_13_Data;
            public string Field_14_Name;
            public string Field_14_Data;
            public string Field_15_Name;
            public string Field_15_Data;
            

            public override string ToString()
            {
                return Trigger;
            }

        }
    }
}
