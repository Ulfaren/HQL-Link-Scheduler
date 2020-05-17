using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace SmartNode
{
    public class Topology:ISerializable
    {
        public double LightSspeed { get; set; }
        public double Frequency { get; set; }
        public double WaveLength { get; set; }
        public double Noise { get; set; }
        public double NoiseVar { get; set; }
        public List<Node> Nodes { get; set; }
        


        public Topology()
        {
            LightSspeed = Math.Pow(10, 8) * 3;
            Frequency = Math.Pow(10, 9) * 2.4;
            Noise = 0.000000911;
            WaveLength = LightSspeed / Frequency;
            NoiseVar = 0;
            Nodes = new List<Node>();
        }

        public Topology(double no)
        {
            LightSspeed = Math.Pow(10, 8) * 3;
            Frequency = Math.Pow(10, 9) * 2.4;
            Noise = no;
            WaveLength = LightSspeed / Frequency;
            NoiseVar = 0;
            Nodes = new List<Node>();
            

        }

        public void Generate(int width, int length)
        {
            
            Random rand = new Random();

            foreach (Node node in Nodes)
            {
                node.X = rand.Next(0, length);
                node.Y = rand.Next(0, width);
                node.Number = Nodes.IndexOf(node);
            }

            double MaxTrarng = new double();
            foreach (Node m in Nodes)
            {
                foreach (Node n in Nodes)
                {
                    if (m.Number != n.Number)
                    {
                        double d = CalculateDistance(m, n);
                        double maximum_transmission_range = Maximum_Transmission_Range(m, n);
                        MaxTrarng = maximum_transmission_range;
                        if (d <= (maximum_transmission_range*0.5))
                        {
                            Link link = new Link(m, n);
                            m.Neightbors.Add(n.Number);
                        }
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine("Topogy Generated, maximum transmission range is " + MaxTrarng.ToString() + "m");
            foreach(Node n in Nodes)
            {
                Console.WriteLine("Node N" + n.Number.ToString() + " (" + n.X.ToString() + "," + n.Y.ToString() + ")");
            }


            /*
            foreach(Node m in Nodes)
            {
                foreach(Link link in m.Links)
                {
                    Console.WriteLine("Node: " + m.Number.ToString() + " has Link: (N" + link.Transmitter.Number.ToString() + ",N" + link.Receiver.Number.ToString() + ")");
                }
                foreach(Node n in m.Neighbors)
                {
                    Console.WriteLine("Node: " + m.Number.ToString() + " has Neightbor: Node " + n.Number.ToString());
                }
            }
            */
        }

        /*
        public void Regenerate()
        {
            foreach (Node m in Nodes)
            {
                foreach (Node n in Nodes)
                {
                    double d = CalculateDistance(m, n);
                    double maximum_transmission_range = Maximum_Transmission_Range(m, n);
                    if (d <= maximum_transmission_range && !m.Equals(n))
                    {
                        Link link = new Link(m, n);
                        m.Neightbors.Add(n);
                    }
                }
            }
        }
        */

        public double CalculateDistance(Node m, Node n)
        {
            int X1 = m.X;
            int X2 = n.X;
            int Y1 = m.Y;
            int Y2 = n.Y;

            double distance = (double)Math.Sqrt(((X1 - X2) * (X1 - X2)) + ((Y1 - Y2) * (Y1 - Y2)));

            return (distance);
        }

        public double Maximum_Transmission_Range(Node m, Node n)
        {
            double maximum_transmission_range = new double();
            double l_0 = m.Transmit_Antenna_Gain * n.Receive_Antenna_Gain * Math.Pow((LightSspeed / (4 * Frequency * Math.PI * 1)), 2);
            double pt = 0.1;
            double pr = 0.1 * l_0;
            double L_0 = 10 * Math.Log10(pt / pr);
            double c_0 = Math.Pow(1, 2) * Math.Pow(10, (-L_0 / 10));
            maximum_transmission_range = Math.Sqrt((m.Transmission_Power*c_0 )/ (Noise*Math.Pow(10,0.4)));
            return (maximum_transmission_range);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Light Speed", LightSspeed);
            info.AddValue("Frequency", Frequency);
            info.AddValue("Wave Length", WaveLength);
            info.AddValue("Noise", Noise);
            info.AddValue("Noise Var", NoiseVar);
            info.AddValue("Nodes", Nodes);
        }

        public Topology(SerializationInfo info, StreamingContext context)
        {
            LightSspeed = (double)info.GetValue("Light Speed", typeof(double));
            Frequency = (double)info.GetValue("Frequency", typeof(double));
            WaveLength = (double)info.GetValue("Wave Length", typeof(double));
            Noise = (double)info.GetValue("Noise", typeof(double));
            NoiseVar = (double)info.GetValue("Noise Var", typeof(double));
            Nodes = (List<Node>)info.GetValue("Nodes", typeof(List<Node>));
           
        }

        public double InterferenceSingle(Node m, Node n, Node sender, Random rand)
        {
            double interference = new double();
            
            if (m.Number == n.Number)
            {
                interference = 0;
            }
            else
            {
                double l_0 = m.Transmit_Antenna_Gain * n.Receive_Antenna_Gain * Math.Pow((LightSspeed / (4 * Frequency * Math.PI * 1)), 2);
                double pt = 0.1;
                double pr = 0.1 * l_0;
                double L_0 = 10 * Math.Log10(pt / pr);
                double c_0 = Math.Pow(1, 2) * Math.Pow(10, (-L_0 / 10));
                double F_g = Math.Pow(10, -(SimpleGaussian(rand, 0, NoiseVar) / 10));
                double PL = (c_0 * F_g) / (Math.Pow((CalculateDistance(m, n)), 2));
                interference = m.Transmission_Power * PL;
            }
            return (interference);
        }

        public double Interference(Node n, Node sender, Random rand)
        {
            double interference = new double();
            
            foreach (Node m in Nodes)
            {
                if (m.IsTransmitting == false)
                {
                    interference = interference + 0;
                }
                else
                {
                    if (n.Number == m.Number)
                    {
                        interference = interference + 0;
                    }
                    else if (m.Number == sender.Number)
                    {
                        interference = interference + 0;
                    }
                    else
                    {
                        double l_0 = m.Transmit_Antenna_Gain * n.Receive_Antenna_Gain * Math.Pow((LightSspeed / (4 * Frequency * Math.PI * 1)), 2);
                        double pt = 0.1;
                        double pr = 0.1 * l_0;
                        double L_0 = 10 * Math.Log10(pt / pr);
                        double c_0 = Math.Pow(1, 2) * Math.Pow(10, (-L_0 / 10));
                        double F_g = Math.Pow(10, -(SimpleGaussian(rand, 0, NoiseVar) / 10));
                        double PL = (c_0 * F_g) / (Math.Pow((CalculateDistance(m, n)), 2));
                        interference = interference + m.Transmission_Power * PL;

                    }
                }
                
                    
          
              

            }

            return (interference);
        }

        public double CurrentTransmissionPower(Node n, Node m, Random rand)
        {
            double l_0 = m.Transmit_Antenna_Gain * n.Receive_Antenna_Gain * Math.Pow((LightSspeed / (4 * Frequency * Math.PI * 1)), 2);
            double pt = 0.1;
            double pr = 0.1 * l_0;
            double L_0 = 10 * Math.Log10(pt/pr);
            double c_0 = Math.Pow(1, 2) * Math.Pow(10, (-L_0 / 10));
            double F_g = Math.Pow(10, -(SimpleGaussian(rand, 0, NoiseVar) / 10));
            double PL = (c_0 * F_g) / (Math.Pow((CalculateDistance(m, n)), 2));
            return(m.Transmission_Power * PL);
        }

        public double SINR(double signalpower, double interference)
        {
            double s = signalpower;
            double i = interference;
            double t = s / i;
            double sinr = (10 * Math.Log10(t));
            return (sinr);
        }

        public int DataRate(double sinr)
        {
            int datarate = new int();
            if (sinr < 4)
            {
                datarate = 0;
            }
            if (sinr >= 4 && sinr < 6)
            {
                datarate = 6;
            }
            if (sinr >= 6 && sinr < 8)
            {
                datarate = 9;
            }
            if (sinr >= 8 && sinr < 10)
            {
                datarate = 12;
            }
            if (sinr >= 10 && sinr < 12)
            {
                datarate = 18;
            }
            if (sinr >= 12 && sinr < 16)
            {
                datarate = 24;
            }
            if (sinr >= 16 && sinr < 20)
            {
                datarate = 36;
            }
            if (sinr >= 20 && sinr < 21)
            {
                datarate = 48;
            }
            if (sinr >= 21)
            {
                datarate = 54;
            }

            return (datarate*1000/8);

        }

        public int ShiftDataRate(int datarate, int key)
        {
            int[] Table = { 6, 9, 12, 18, 24, 36, 48, 54 };
            int index = new int();
            for(int i =0;i<=7;i++)
            {
                if(Table[i]==datarate)
                {
                    index = i;
                    break;
                }
                else
                {
                    continue;
                }
            }

            if((index-key)<=0)
            {
                return 6*1000/8;
            }
            else
            {
                return (Table[index - key]*1000/8);
            }

            
        }

        public double SimpleGaussian(Random random, double mean, double stddev)
        {
            // The method requires sampling from a uniform random of (0,1]
            // but Random.NextDouble() returns a sample of [0,1).

            double x1 = 1 - random.NextDouble();
            double x2 = 1 - random.NextDouble();

            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return y1 * stddev + mean;
        }
    }
}
