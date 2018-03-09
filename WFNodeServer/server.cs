﻿
//
// WFNodeServer - ISY Node Server for Weather Flow weather station data
//
// Copyright (C) 2018 Robert Paauwe
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace WFNodeServer {
    internal class WFNServer {
        private Thread server_thread;
        private string root_directory;
        private HttpListener listener;
        private int port;
        private int profile;

        internal int Port {
            get {
                return port;
            }
            private set { }
        }

        internal WFNServer(string path, int port, int profile) {
            this.profile = profile;
            this.Initialize(path, port);
        }

        // Auto assign port. Can probably remove this
        internal WFNServer(string path, int profile) {
            this.profile = profile;
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(path, port);
        }

        internal void Stop() {
            server_thread.Abort();
            listener.Stop();
        }

        private void Listen() {
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port.ToString() + "/");
            listener.Start();
            while (true) {
                try {
                    HttpListenerContext context = listener.GetContext();
                    Process(context);
                } catch (Exception ex) {
                    Console.WriteLine("Failed to process connection: " + ex.Message);
                }
                Thread.Sleep(5000);
            }
        }

        private void Process(HttpListenerContext context) {
            string filename = context.Request.Url.AbsolutePath;
            Console.WriteLine(" request = " + filename);

            // WeatherFlow/install  - install
            // Weatherflow/nodes/<address>/query - Query the node and report status
            // WeatherFlow/nodes/<address>/status - report current status
            // WeatherFlow/add/nodes - Add all nodes
            //  These could have a ?requestid=<requestid> at the end
            //  that means we need to send a message after completing the task
            // WeatherFlow/nodes/<address>/report/add
            // WeatherFlow/nodes/<address>/report/remove
            // WeatherFlow/nodes/<address>/report/rename
            // WeatherFlow/nodes/<address>/report/enable
            // WeatherFlow/nodes/<address>/report/disable

            // TODO: Parse out the request id if present
            if (filename.Contains("install")) {
                Console.WriteLine("Recieved request to install the profile files.");
            } else if (filename.Contains("query")) {
                string[] parts;
                parts = filename.Split('/');

                Console.WriteLine("Query node " + parts[3]);
            } else if (filename.Contains("status")) {
                string[] parts;
                parts = filename.Split('/');

                Console.WriteLine("Get status of node " + parts[3]);
                NodeStatus();
            } else if (filename.Contains("add")) {
                Console.WriteLine("Add our node.  How is this different from report/Add?");
                AddNodes();

            // the report API is not yet implemented on the ISY so we'll
            // never get anything of these until it is.
            } else if (filename.Contains("report/add")) {
                Console.WriteLine("Report that a node was added?");
            } else if (filename.Contains("report/rename")) {
            } else if (filename.Contains("report/remove")) {
            } else if (filename.Contains("report/enable")) {
            } else if (filename.Contains("report/disable")) {
            } else {
                Console.WriteLine("Unknown Request: " + filename);
            }

            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = 0;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
        }

        private void Initialize(string path, int port) {
            this.root_directory = path;
            this.port = port;

            server_thread = new Thread(this.Listen);
            server_thread.IsBackground = true;
            server_thread.Name = "WFNodeServer REST API";
            server_thread.Priority = ThreadPriority.Normal;
            server_thread.Start();
        }

        private void AddNodes() {
            string str;

            str = "ns/" + profile.ToString() + "/nodes/n" + profile.ToString("000") + "_weather_flow/add/WeatherFlow/?name=WeatherFlow";
            WeatherFlowNS.NS.Rest.REST(str);
        }

        private void NodeStatus() {
            WeatherFlowNS.NS.RaiseAirEvent(this, new WFNodeServer.AirEventArgs(WeatherFlowNS.NS.udp_client.AirObj));
            WeatherFlowNS.NS.RaiseSkyEvent(this, new WFNodeServer.SkyEventArgs(WeatherFlowNS.NS.udp_client.SkyObj));
        }
    }
 
}
