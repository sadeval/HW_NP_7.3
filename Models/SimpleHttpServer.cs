using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace UserManagementApp
{
    public class SimpleHttpServer
    {
        private HttpListener listener;
        private Thread listenerThread;
        private List<Person> people = new List<Person>();

        public bool IsRunning { get; private set; }

        public void Start(string prefix)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("HttpListener is not supported on this operating system.");
            }

            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            IsRunning = true;

            listenerThread = new Thread(new ThreadStart(HandleRequests));
            listenerThread.Start();
        }

        public void Stop()
        {
            IsRunning = false;
            listener.Stop();
            listenerThread.Abort();
        }

        private void HandleRequests()
        {
            while (IsRunning)
            {
                try
                {
                    var context = listener.GetContext();
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "application/json";

            string responseString = "";
            int statusCode = 200;

            try
            {
                if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/persons")
                {
                    responseString = JsonConvert.SerializeObject(people);
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/persons")
                {
                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string json = reader.ReadToEnd();
                        var person = JsonConvert.DeserializeObject<Person>(json);
                        person.Id = Guid.NewGuid().ToString();
                        people.Add(person);
                        responseString = JsonConvert.SerializeObject(person);
                        statusCode = 201; 
                    }
                }
                else if (request.HttpMethod == "PUT" && request.Url.AbsolutePath.StartsWith("/persons/"))
                {
                    string id = request.Url.AbsolutePath.Substring("/persons/".Length);
                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string json = reader.ReadToEnd();
                        var updatedPerson = JsonConvert.DeserializeObject<Person>(json);
                        var person = people.Find(p => p.Id == id);
                        if (person != null)
                        {
                            person.Name = updatedPerson.Name;
                            person.BirthDate = updatedPerson.BirthDate;
                            person.Salary = updatedPerson.Salary;
                            responseString = JsonConvert.SerializeObject(person);
                        }
                        else
                        {
                            statusCode = 404; 
                            responseString = $"{{\"error\":\"Person with Id {id} not found.\"}}";
                        }
                    }
                }
                else if (request.HttpMethod == "DELETE" && request.Url.AbsolutePath.StartsWith("/persons/"))
                {
                    string id = request.Url.AbsolutePath.Substring("/persons/".Length);
                    var person = people.Find(p => p.Id == id);
                    if (person != null)
                    {
                        people.Remove(person);
                        responseString = $"{{\"message\":\"Person with Id {id} deleted.\"}}";
                    }
                    else
                    {
                        statusCode = 404; 
                        responseString = $"{{\"error\":\"Person with Id {id} not found.\"}}";
                    }
                }
                else
                {
                    statusCode = 404; 
                    responseString = "{\"error\":\"Endpoint not found.\"}";
                }
            }
            catch (Exception ex)
            {
                statusCode = 500; 
                responseString = $"{{\"error\":\"{ex.Message}\"}}";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.StatusCode = statusCode;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}
