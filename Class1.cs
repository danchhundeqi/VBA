using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using static OCR.ExportProcess;
using static OCRTest.class2;

namespace OCR
{
    public class Class1
    {
        public async Task OcrHttpRequest( string splitPageFilePath, string excelSavePath)
        {
            string appKey = "5dad19e5";
            string appSecret = "15f2a4360e13b9d3a558f27a75de2b49";
            //string url = "http://172.18.10.11:8080/v1/item/get_multiple_items_info";11123
            string url = "http://9.125.131.179:8080/v1/item/get_multiple_items_info";

            DirectoryInfo folder = new DirectoryInfo(splitPageFilePath);
            foreach (FileInfo fileInfo in folder.GetFiles())
            {
                string filePath = splitPageFilePath;
                string fileName = fileInfo.Name;
                long timeStamp = DateTime.Now.Millisecond / 1000;
                string token = Md5Hex(appKey + "+" + timeStamp + "+" + appSecret);
                HttpResponseMessage message;
                DateTime startTime;
                DateTime endTime;
                try
                {
                    //send HTTP Request
                    using (var formContent = new MultipartFormDataContent())
                    {
                        formContent.Headers.ContentType.MediaType = "multipart/form-data";
                        Stream fileStream = File.OpenRead(Path.Combine(filePath, fileName));
                        formContent.Add(new StreamContent(fileStream), "image_file", fileName);
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("app_key", appKey);
                            client.DefaultRequestHeaders.Add("timestamp", timeStamp.ToString());
                            client.DefaultRequestHeaders.Add("token", token);
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                            startTime = DateTime.Now;
                            
                            message = await client.PostAsync(url, formContent);
                            endTime = DateTime.Now;
                        }
                    }
                    //save OCR feedbak data
                    Details ord = new Details();
                    if (message.IsSuccessStatusCode)
                    {
                        string result = await message.Content.ReadAsStringAsync();
                        var jObject = JObject.Parse(result);
                        //get details
                        var details = jObject["response"]["data"]["identify_results"][0]["details"].ToString();
                        
                        var type = jObject["response"]["data"]["identify_results"][0]["type"].ToString();
                        
                        //Console.WriteLine(details);
                        if (type == "10100" | type == "10101" | type == "10102" | type == "10103")
                        {
                            ord = JsonConvert.DeserializeObject<Details>(details);
                            Console.WriteLine($"type match: {type} Processing...     path:{fileInfo}");

                            var taxRateTemp = "";
                            var nameTemp = "";
                            var priceTemp = "";
                            var quantityTemp = "";
                            var taxTemp = "";
                            var totalTemp = "";
                            //Export Excel
                            try
                            {
                                var taxRateTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["tax_rate"].ToString();
                                var nameTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["name"].ToString();
                                var priceTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["price"].ToString();
                                var quantityTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["quantity"].ToString();
                                var taxTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["tax"].ToString();
                                var totalTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["total"].ToString();

                                taxRateTemp = taxRateTemp1;
                                nameTemp = nameTemp1;
                                priceTemp = priceTemp1;
                                quantityTemp = quantityTemp1;
                                taxTemp = taxTemp1;
                                totalTemp = totalTemp1;
                            }
                            catch(Exception)
                            {
                                try
                                {
                                    var taxRateTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][1]["tax_rate"].ToString();
                                    var nameTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][1]["name"].ToString();
                                    var priceTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][1]["price"].ToString();
                                    var quantityTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][1]["quantity"].ToString();
                                    var taxTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][1]["tax"].ToString();
                                    var totalTemp1 = jObject["response"]["data"]["identify_results"][0]["details"]["items"][1]["total"].ToString();

                                    taxRateTemp = taxRateTemp1;
                                    nameTemp = nameTemp1;
                                    priceTemp = priceTemp1;
                                    quantityTemp = quantityTemp1;
                                    taxTemp = taxTemp1;
                                    totalTemp = totalTemp1;
                                }
                                catch(Exception)
                                {
                                    var taxRateTemp1 = "";
                                    var nameTemp1 = "";
                                    var priceTemp1 = "";
                                    var quantityTemp1 = "";
                                    var taxTemp1 = "";
                                    var totalTemp1 = "";
                                    taxRateTemp = taxRateTemp1;
                                    nameTemp = nameTemp1;
                                    priceTemp = priceTemp1;
                                    quantityTemp = quantityTemp1;
                                    taxTemp = taxTemp1;
                                    totalTemp = totalTemp1;
                                }
                            }

                            //var total = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["total"].ToString();
                            //var price = jObject["response"]["data"]["identify_results"][0]["details"]["items"][0]["price"].ToString();
                            var taxRate = taxRateTemp;
                            var price = priceTemp;
                            var total = totalTemp;

                            var name = nameTemp;
                            var quantity = quantityTemp;
                            var tax = taxTemp;

                            

                            string excelFullFileName = Path.Combine(excelSavePath, Path.GetFileNameWithoutExtension(fileName) + ".xlsx");

                            ExportProcess export = new ExportProcess();
                            export.ExportExcel(export.ListToDataTable(ord), excelFullFileName,type,taxRate, total,price, name, quantity, tax,"", true, "");
                            Console.WriteLine("OCR data processing complete");
                        }
                        else
                        {
                            Console.WriteLine($"type not match: {type}     path:{fileInfo}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"OCR file recieve fail.     path:{fileInfo}");
                    }
                }
                catch (Exception )
                {
                    Console.WriteLine("OCR fail");
                }
            }
        }
        private string Md5Hex(string data)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] dataHash = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in dataHash)
            {
                sb.Append(b.ToString("x2").ToLower());
            }
            return sb.ToString();

        }
    }

}
