using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Collections;

namespace SimConnectTestWPF
{
    public class WebServer
    {
        private HttpListener listener = null;

        public WebServer()
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add("http://*:40000/");
        }

        public void start()
        {
            this.listener.Start();
            IAsyncResult res = listener.BeginGetContext(new AsyncCallback(ListenerCallBack), listener);
        }

        public void stop()
        {
            this.listener.Stop();
        }

        public static void ListenerCallBack(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            CultureInfo c = new CultureInfo("en-us");

            string responseString = "";
            if (request.RawUrl == "/route/")
            {
                responseString = String.Format("{0}", FsxData.coords);
            }
            else
            {
                responseString = "{\"array\":[";
                try
                {
                    foreach (DictionaryEntry de in FsxData.planeTable)
                    {
                        PlaneData p = (PlaneData)de.Value;
                        String aircraft_title = p.title.Replace("\"", "\\\"");
                        responseString += String.Format("{{\"title\":\"{0}\", \"latitude\":{1}, \"longitude\":{2}, \"altitude\":{3}, \"heading\":{4}, \"isUser\":{5}}},", aircraft_title, p.latitude.ToString(c), p.longitude.ToString(c), p.altitude.ToString(c), p.heading.ToString(c), p.isUser);
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.Print("{0}", e);
                }
                responseString = responseString.TrimEnd(',');
                responseString += "]}";
            }
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;

            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
            IAsyncResult res = listener.BeginGetContext(new AsyncCallback(ListenerCallBack), listener);
        }
    }
}