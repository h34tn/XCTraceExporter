using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;

namespace TraceExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);

            // select the start-time
            var timeNodes = doc.SelectNodes("/trace-query-result/node/row/start-time/@id/..");
            var mapIdTime = new Dictionary<string, string>();
            foreach (XmlNode node in timeNodes)
            {
                //Console.WriteLine(node.OuterXml);
                mapIdTime.Add(node.Attributes["id"].Value, node.Attributes["fmt"].Value);
            }

            //var memNodes = doc.SelectNodes("/trace-query-result/node/row/process[@ref=\"5391\"]/../size-in-bytes[1]/@fmt");

            StreamWriter file = new StreamWriter("result.csv");
            // select the specified process nodes
            var processNodes = doc.SelectNodes($"/trace-query-result/node/row/process[@ref={args[1]}]/..");
            foreach(XmlNode node in processNodes)
            {
                string time;
                string refTime = node["start-time"].GetAttribute("ref");
                if (string.IsNullOrEmpty(refTime))
                    time = node["start-time"].GetAttribute("fmt");
                else
                    mapIdTime.TryGetValue(refTime, out time);
                if(!string.IsNullOrEmpty(time))
                {
                    string fmtMem = node["size-in-bytes"].GetAttribute("fmt");
                    if (!string.IsNullOrEmpty(fmtMem))
                        file.WriteLine($"{time},{float.Parse(fmtMem.Replace(" MiB", ""))}");
                }
                else
                {
                    Console.WriteLine($"Not found refTime {refTime}");
                }
            }

            file.Close();
        }
    }
}
