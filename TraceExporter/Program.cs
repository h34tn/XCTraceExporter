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
            string option = args[0];

            if(option != "-cpu" && option != "-mem")
            {
                Console.WriteLine($"Not supported option : {option}");
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(args[1]);

            if(option == "-cpu")
            {
                // select the percent
                var percentNodes = doc.SelectNodes("/trace-query-result/node/row/percent/@id/..");
                var mapIdPercent = new Dictionary<string, string>();
                foreach(XmlNode node in percentNodes)
                {
                    mapIdPercent.Add(node.Attributes["id"].Value, node.Attributes["fmt"].Value);
                }

                string outFileName = string.IsNullOrEmpty(args[2]) ? "result.csv" : args[2];
                using (StreamWriter file = new StreamWriter(outFileName))
                {
                    var sysmonNodes = doc.SelectNodes("/trace-query-result/node/row");
                    foreach (XmlNode node in sysmonNodes)
                    {
                        string fmtPercent;
                        if (node["percent"].HasAttribute("fmt"))
                            fmtPercent = node["percent"].Attributes["fmt"].Value;
                        else
                        {
                            string refPercent = node["percent"].Attributes["ref"].Value;
                            if (!mapIdPercent.TryGetValue(refPercent, out fmtPercent))
                            {
                                Console.WriteLine($"Can't find percent ref : {refPercent}");
                                continue;
                            }
                        }
                        file.WriteLine($"{node["start-time"].Attributes["fmt"].Value}, {fmtPercent}");
                    }
                }
            }
            else if(option == "-mem")
            {
                // select the start-time
                var timeNodes = doc.SelectNodes("/trace-query-result/node/row/start-time/@id/..");
                var mapIdTime = new Dictionary<string, string>();
                foreach (XmlNode node in timeNodes)
                {
                    //Console.WriteLine(node.OuterXml);
                    mapIdTime.Add(node.Attributes["id"].Value, node.Attributes["fmt"].Value);
                }

                // select the size-in-bytes
                var sizeNodes = doc.SelectNodes("/trace-query-result/node/row/size-in-bytes/@id/..");
                var mapidSize = new Dictionary<string, string>();
                foreach (XmlNode node in sizeNodes)
                {
                    mapidSize.Add(node.Attributes["id"].Value, node.Attributes["fmt"].Value);
                }

                //var memNodes = doc.SelectNodes("/trace-query-result/node/row/process[@ref=\"5391\"]/../size-in-bytes[1]/@fmt");
                var targetProcessNode = doc.SelectSingleNode($"/trace-query-result/node/row/process/pid[@fmt={args[2]}]/..");
                if (targetProcessNode == null)
                {
                    Console.WriteLine($"Can't find the specified process {args[2]}");
                    return;
                }

                string processID = targetProcessNode.Attributes["id"].Value;

                string outFileName = string.IsNullOrEmpty(args[3]) ? "result.csv" : args[3];
                using (StreamWriter file = new StreamWriter(outFileName))
                {
                    // select the specified process nodes
                    var processNodes = doc.SelectNodes($"/trace-query-result/node/row/process[@ref={processID}]/..");
                    foreach (XmlNode node in processNodes)
                    {
                        string time;
                        string refTime = node["start-time"].GetAttribute("ref");
                        if (string.IsNullOrEmpty(refTime))
                            time = node["start-time"].GetAttribute("fmt");
                        else
                            mapIdTime.TryGetValue(refTime, out time);
                        if (!string.IsNullOrEmpty(time))
                        {
                            string fmtMem = node["size-in-bytes"].GetAttribute("fmt");
                            if (string.IsNullOrEmpty(fmtMem))
                            {
                                string sizeRef = node["size-in-bytes"].GetAttribute("ref");
                                mapidSize.TryGetValue(sizeRef, out fmtMem);
                            }
                            string fmtCpu = node["system-cpu-percent"].GetAttribute("fmt");
                            float memVal = 0.0f;
                            if (fmtMem.Contains("MiB"))
                                memVal = float.Parse(fmtMem.Replace(" MiB", ""));
                            else if(fmtMem.Contains("GiB"))
                                memVal = float.Parse(fmtMem.Replace(" GiB", "")) * 1024;
                            file.WriteLine($"{time}, {memVal}, {fmtCpu}");
                        }
                        else
                        {
                            Console.WriteLine($"Not found refTime {refTime}");
                        }
                    }
                }
            }
        }
    }
}
