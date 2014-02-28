using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using Newtonsoft.Json;
using RestSharp;

namespace Zappos
{
    class Program
    {

        private static string API_KEY = "52ddafbe3ee659bad97fcce7c53592916a6bfd73";

        private static List<UserRequest> requests = new List<UserRequest>();

        static void Main(string[] args)
        {
            /* 
            Write a small application using the Zappos API (developer.zappos.com) that lets a 
            user input their email and a desired product (productId or productName) and sends
            them an email when the price hits at least 20% off the original price.
            */

            StartWorker();
            while (true)
            {
                NewRequest();
            }
        }

        private static void NewRequest()
        {
            Console.WriteLine("Please select a product");
            string product = Console.ReadLine();

            Console.WriteLine("Please write your email address");
            string email = Console.ReadLine();

            UserRequest newRequest = new UserRequest();
            newRequest.email = email;
            newRequest.product = product;
            requests.Add(newRequest);

            Console.WriteLine("\r\nProduct " + product + " added to our list");
            Console.WriteLine("\r\n\r\n---------------\r\n\r\n");
        }

        private static List<Product> SearchProduct(string product, string key)
        {
            string API_URL = "http://api.zappos.com/Search";

            RestClient client = new RestClient(API_URL);

            RestRequest request = new RestRequest("", Method.GET);
            request.AddParameter("term", product);
            request.AddParameter("limit", "100");
            request.AddParameter("key", key);

            var restResponse = client.Execute(request);

            var products = (Products)JsonConvert.DeserializeObject(
                restResponse.Content, typeof(Products));

            List<Product> results = new List<Product>();
            foreach (Product p in products.results)
            {
                //Here I filter for productName or productId because the search "term" can be any metadata
                if ((p.productName.ToLower().Contains(product.ToLower())) || (p.productId.Contains(product)))
                {
                    results.Add(p);
                }
            }
            return results;
        }

        private static void SendEmail(string toEmail, string product, double discount)
        {
            MailMessage message = new MailMessage();
            string fromEmail = "ronanforzappos@gmail.com";
            string fromPW = "zappos123";
            message.From = new MailAddress(fromEmail);
            message.To.Add(toEmail);
            message.Subject = "Discount for " + product;
            message.Body = "The product " + product + " has a " + discount + "% of discount";
            message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.EnableSsl = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(fromEmail, fromPW);

            smtpClient.Send(message);
        }


        private static void CheckDiscount(List<Product> products, UserRequest ur)
        {
            foreach (Product p in products)
            {
                int discount = int.Parse(p.percentOff.TrimEnd(new char[] { '%', ' ' }));

                /*
                 Conditions:
                 (Discount > 20 and email not sent yet)
                 or
                 (Discount > 20 and email sent but new discount > old discount)
                */
                if ((discount > 20) && ((!ur.sent || (ur.sent && ur.lastDiscount < discount))))
                {
                    SendEmail(ur.email, p.productName, discount);
                    MarkSent(ur, discount);
                }
            }
        }

        private static void MarkSent(UserRequest ur, int discount)
        {
            UserRequest newR = new UserRequest();
            newR.email = ur.email;
            newR.product = ur.product;
            newR.sent = true;
            newR.lastDiscount = discount;
            ur.sent = true;
            ur.lastDiscount = discount;
            requests.Remove(ur);
            requests.Add(newR);
        }

        public class Worker
        {
            public void DoWork()
            {
                while (!_shouldStop)
                {
                    foreach (UserRequest ur in requests.ToList())
                    {
                        List<Product> products = SearchProduct(ur.product, API_KEY);
                        CheckDiscount(products, ur);
                    }
                    
                }
            }
            public void RequestStop()
            {
                _shouldStop = true;
            }

            // Volatile is used as hint to the compiler that this data 
            // member will be accessed by multiple threads. 
            private volatile bool _shouldStop;
        }

        private static void StartWorker()
        {
            Worker workerObject = new Worker();
            Thread workerThread = new Thread(workerObject.DoWork);

            // Start the worker thread.
            workerThread.Start();
            while (!workerThread.IsAlive);
            Thread.Sleep(1);
        }
    }
}

