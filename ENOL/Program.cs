using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modbus;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using System.Threading;
using System.Collections.Specialized;
using System.Collections;
using MadMilkman.Ini;

namespace ENOL
{
    class Program
    {
        static double register_to_double(ushort[] inputs)
        {
            double sum_power = 0;
            for (int i = 0; i < 4; i++)
            {
                sum_power += inputs[i] * Math.Pow(65536, 3 - i);
            }
            sum_power /= 1000;
            return sum_power;
        }
        static void save_data(string building,string node, string datetime,string value,string logurl)
        {
            using (var client = new WebClient())
            {
                var values = new NameValueCollection();
                values["building"] = building.ToString();
                values["node"] = node.ToString();
                values["datetime"] = datetime;
                values["value"] = value.ToString();
                
                try
                {
                    var response = client.UploadValues(logurl, values);
                    var responseString = Encoding.Default.GetString(response);
                    Console.WriteLine(datetime + "__________" + value + "______紀錄成功");
                }
                catch (WebException e)
                {
                    Console.Write(e.Message);
                }                
            }
        }
        static void Main(string[] args)
        {
            IniOptions option = new IniOptions();
            option.CommentStarter = IniCommentStarter.Hash;

            // Load INI file from path, Stream or TextReader.
            IniFile ini = new IniFile(option);
            ini.Load("config.ini");
            //IniSection sec = ini.Sections["PM5350"];            
            while (true)
            {
                foreach (IniSection sec in ini.Sections)
                {
                    String Building = sec.Keys["BUILDING"].Value;
                    String[] Nodes = sec.Keys["NODE"].Value.Split(',');
                    String IP = sec.Keys["IP"].Value;
                    String Port = sec.Keys["PORT"].Value;
                    String Register = sec.Keys["REGISTER"].Value;
                    String Length = sec.Keys["LENGTH"].Value;
                    bool Log = Boolean.Parse(sec.Keys["LOG"].Value);
                    String Logurl = sec.Keys["LOGURL"].Value;
                    int LogInterval = int.Parse(sec.Keys["LOGINTERVAL"].Value);

                    IPAddress address = IPAddress.Parse(IP);
                    // create the master
                    TcpClient masterTcpClient = new TcpClient(address.ToString(), int.Parse(Port));
                    ModbusIpMaster master = ModbusIpMaster.CreateIp(masterTcpClient);
                    master.Transport.ReadTimeout = master.Transport.WriteTimeout = 1000;

                    string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    foreach (String node in Nodes)
                    {
                        ushort[] inputs = master.ReadHoldingRegisters(byte.Parse(node), ushort.Parse(Register), ushort.Parse(Length));

                        Console.Write("Datetime: " + dt);
                        Console.Write("Building: " + Building);
                        Console.Write("Node: " + node);
                        Console.Write("Register: " + Register);
                        Console.WriteLine("Value: " + register_to_double(inputs).ToString());

                        if (Log && ((DateTime.Now.Minute) % LogInterval == 0))
                        {
                            save_data(Building, node, dt, register_to_double(inputs).ToString(),Logurl);
                        }
                    }
                    master.Dispose();
                }
                Thread.Sleep(60 * 1000);
            }                            
        }
    }
}
