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
    [Serializable]
    public class Package : ISerializable
    {
        public int Node { get; set; }
        public int Link { get; set; }
        public double Data_Length { get; set; }
        public double Data_Rate { get; set; }
        public double Start_Time { get; set; }
        public double Transmission_Time { get; set; }
        public double End_Time { get; set; }
        [XmlIgnoreAttribute]
        public Boolean IsBeingTransmitting { get; set; }

        public Package()
        {
            Node = new int();
            Link = new int();
            Data_Length = new double();
            Data_Rate = new double();
            Start_Time = new double();
            Transmission_Time = new double();
            End_Time = new double();
            IsBeingTransmitting = false;
 
        }

        public Package(Node n,Random rand)
        {
            Node = n.Number;
            Link = rand.Next(0, n.Links.Count);
            Data_Length = rand.Next(100,250);
            Data_Rate = new double();
            Start_Time = new double();
            Transmission_Time = new double();
            End_Time = new double();
            n.Buffer.Add(this);
            IsBeingTransmitting = false;      
        }

        public Package(Node n, Random rand, int length_min, int length_max)
        {
            Node = n.Number;
            Link = rand.Next(0, n.Links.Count);
            Data_Length = rand.Next(length_min, length_max);
            Data_Rate = new double();
            Start_Time = new double();
            Transmission_Time = new double();
            End_Time = new double();
            IsBeingTransmitting = false;
        }

        public Package(Node n, Link l, Random rand, int length_min, int length_max)
        {
            Node = n.Number;
            Link = l.Number;
            Data_Length = rand.Next(length_min, length_max);
            Data_Rate = new double();
            Start_Time = new double();
            Transmission_Time = new double();
            End_Time = new double();
            IsBeingTransmitting = false;
        }

        public Package(Node n, Node m, Random rand, int length_min, int length_max)
        {
            Node = n.Number;
            Link = rand.Next(0, n.Links.Count);
            Data_Length = rand.Next(length_min, length_max);
            Data_Rate = new double();
            Start_Time = new double();
            Transmission_Time = new double();
            End_Time = new double();
            IsBeingTransmitting = false;
        }

        public int CompareTo(Package other)
        {
            if (other == null)
                return 1;
            else
                return this.Data_Length.CompareTo(other.Data_Length);
        }



        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Node", Node);
            info.AddValue("Link", Link);
            info.AddValue("Data Length", Data_Length);
            info.AddValue("Data Rate", Data_Rate);
            info.AddValue("Start Time", Start_Time);
            info.AddValue("Transmission Time", Transmission_Time);
            info.AddValue("End Time", End_Time);
        }

        public Package(SerializationInfo info, StreamingContext context)
        {
            Node = (int)info.GetValue("Node", typeof(int));
            Link = (int)info.GetValue("Link", typeof(int));
            Data_Length = (double)info.GetValue("Data Length", typeof(double));
            Data_Rate = (double)info.GetValue("Data Rate", typeof(double));
            Start_Time = (double)info.GetValue("Start Time", typeof(double));
            Transmission_Time = (double)info.GetValue("Transmission Time", typeof(double));
            End_Time = (double)info.GetValue("End Time", typeof(double));
            IsBeingTransmitting = false;
        }
    }
}
