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
using System.Net;
using System.Collections.Specialized;
using System.Collections;

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
        static void save_data(string building,string node, string datetime,string value)
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
                    var response = client.UploadValues("http://192.168.1.88/log", values);
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
            int port = 502;
            bool war = false;
            IPAddress address = new IPAddress(new byte[] { 10, 1, 3, 40 });            
            // create the master
            TcpClient masterTcpClient = new TcpClient(address.ToString(), port);
            ModbusIpMaster master = ModbusIpMaster.CreateIp(masterTcpClient);
            master.Transport.ReadTimeout = master.Transport.WriteTimeout = 1000;
            Hashtable node_data = new Hashtable();
            
            while (true)
            {
                string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string building = "1";
                ushort[] inputs = master.ReadHoldingRegisters(2, 3203, 4);                               
                node_data.Add(2, register_to_double(inputs));                

                for (byte i=16;i<24;i++)
                {
                    inputs = master.ReadHoldingRegisters(i, 1007, 4);                    
                    node_data.Add(i, register_to_double(inputs));
                    //Thread.Sleep(100);                  
                }

                foreach (DictionaryEntry node in node_data)
                {
                    Console.Write("Datetime: " + dt);
                    Console.Write("Building: " + building);
                    Console.Write("Node: " + node.Key.ToString());
                    Console.WriteLine("KWh: " + node.Value.ToString());

                    if ((DateTime.Now.Minute) % 15 == 0 && war)
                    {
                        save_data(building, node.Key.ToString(), dt, node.Value.ToString());
                    }
                }
                node_data.Clear();
                Thread.Sleep(60 * 1000);
            }                                   
        }
    }
}
