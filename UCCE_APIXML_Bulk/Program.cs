//Добавление балком в UCCE кампании изменений параметров
using System;
using System.IO;
using System.Net;
using System.Text;
using GlobalTrash;
using System.Xml;
using System.Text.RegularExpressions;


namespace GlobalTrash
{
    class Logger
    {
        public static void Log(string message)
        {
            string a = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszzz");
            message = "\n" + a + " " + message;
            File.AppendAllText("APIXML_Bulk.log", message);
            Console.WriteLine(message);
        }
    }
    class XmlParse
    {
        public static Tuple<string> xp(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            XmlElement? xRoot = xmlDoc.DocumentElement;
            string recvalue = "undef";
            string changestamp = "undef";
            if (xRoot != null)
            {
                // обход всех узлов в корневом элементе
                foreach (XmlElement xnode in xRoot)
                {
                    // получаем атрибут changeStamp

                    if (xnode.Name == "changeStamp")
                    {
                        changestamp = xnode.InnerText;
                    }

                }
            }
            return Tuple.Create(changestamp);
        }
    }

    class Auth
    {
        public static string AuthWeb(string customerKey, string customerSecret)
        {
            string plainCredential = customerKey + ":" + customerSecret;
            var plainTextBytes = Encoding.UTF8.GetBytes(plainCredential);
            string encodedCredential = Convert.ToBase64String(plainTextBytes);
            string authorizationHeader = "Authorization: Basic " + encodedCredential;
            return authorizationHeader;
        }
    }
}

namespace WebReq
{
    public class WebRequestPostExample
    {

        async static Task Main()

        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
            string[] arguments = Environment.GetCommandLineArgs();
            string campaign_link = arguments[1]; //ссылка API кампаний
            string xmlfile = arguments[2]; // xml шаблон на замену, changestamp заменяется регекспом
            string path_cid = arguments[3]; // список ID кампаний по одному в строчку
            string loginname = arguments[4]; // Логин
            string password = arguments[5]; // Пароль
            string authorizationHeader = Auth.AuthWeb(loginname, password);
            string[] mass = new string[1000];
            int i = 0;
            using (StreamReader readerID = new StreamReader(@path_cid))
            {
                Console.WriteLine(path_cid);
                string? lineID;
                while ((lineID = await readerID.ReadLineAsync()) != null)
                {
                    mass[i] = lineID;
                    i++;
                    Logger.Log("CampaignID: " + lineID);
                    string a = "<-------------- Starting new request ------------------>";
                    Logger.Log(a);
                    string link = campaign_link + lineID;
                    WebRequest request = WebRequest.Create(link);
                    request.Method = "GET";
                    a = "Link is: " + link;
                    Logger.Log(a);
                    a = "Update command is: " + xmlfile;
                    // Add authorization header
                    Logger.Log(a);
                    request.Headers.Add(authorizationHeader);
                    request.ContentType = "";
                    a = "Trying to send GET request...";
                    Logger.Log(a);
                    try
                    {
                        WebResponse response = request.GetResponse();
                        Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                        Logger.Log("---- response ----");
                        using (Stream dataStream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(dataStream);
                            string responseFromServer = reader.ReadToEnd();
                            Logger.Log(responseFromServer);
                            Logger.Log("---- end of response ----");
                            string changestamp = XmlParse.xp(responseFromServer).Item1;
                            Logger.Log("Update: " + xmlfile);
                            Logger.Log("changestamp: " + changestamp);
                            //------------------------------------------------------------------
                            string xml2put = "init";
                            using (StreamReader reader1 = new StreamReader(xmlfile))
                            {
                                Console.WriteLine(xmlfile);
                                string? line;
                                string replace_str = "123456789";

                                while ((line = await reader1.ReadLineAsync()) != null)
                                {
                                    xml2put = line;
                                }
                                xml2put = Regex.Replace(xml2put, replace_str, changestamp);
                            }
                            //----------------------------------------------------
                            a = "Trying to send PUT request: " + xml2put;

                            Logger.Log(a);
                            System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
                            WebRequest request1 = WebRequest.Create(link);
                            request1.Headers.Add(authorizationHeader);
                            request1.Method = "PUT";
                            request1.ContentType = "application/xml";
                            using (var requestStream = request1.GetRequestStream())
                            {
                                byte[] byteArray = Encoding.ASCII.GetBytes(xml2put);
                                requestStream.Write(byteArray, 0, byteArray.Length);
                            }
                            var response1 = (HttpWebResponse)request1.GetResponse();

                            if (response1.StatusCode == HttpStatusCode.OK)
                                a = "Update completed";
                            else
                                a = "Error in update";
                            Logger.Log(a);
                        }

                        response.Close();
                    } // end of try
                    catch (Exception e)
                    {
                        a = "Error: " + e.Message;
                        Logger.Log(a);
                    }



                }
            }
        }
    }
}