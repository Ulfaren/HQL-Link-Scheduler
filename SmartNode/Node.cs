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
    [Serializable()]
    public class Node:ISerializable
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Number { get; set; }
        public double Transmission_Power { get; set; }
        public double Transmit_Antenna_Gain { get; set; }
        public double Receive_Antenna_Gain { get; set; }
        public List<int> Neightbors { get; set; }
 
        
        public List<Link> Links { get; set; }
        public List<PrimaryQLPair> PrimaryQLTable { get; set; }

        [XmlIgnoreAttribute]
        public List<Package> Buffer { get; set; }
        [XmlIgnoreAttribute]
        public Boolean IsReady { get; set; }
        [XmlIgnoreAttribute]
        public Boolean IsTransmitting { get; set; }
        [XmlIgnoreAttribute]
        public Boolean IsReceiving { get; set; }
        [XmlIgnoreAttribute]
        public Boolean IsSuccessful { get; set; }
        [XmlIgnoreAttribute]
        public double WaitingTimer { get; set; }
        [XmlIgnoreAttribute]
        public int Iterations { get; set; }
        [XmlIgnoreAttribute]
        public double SlotLength { get; set; }
        [XmlIgnoreAttribute]
        public List<bool> CurrentSystemState { get; set; }
        [XmlIgnoreAttribute]
        public List<bool> LastSystemState { get; set; }
        [XmlIgnoreAttribute]
        public PrimaryAction CurrentPrimaryAction { get; set; }
        [XmlIgnoreAttribute]
        public Boolean PrimaryChosenGreedy { get; set; }
        [XmlIgnoreAttribute]
        public double PrimaryTotalReward { get; set; }
        [XmlIgnoreAttribute]
        public double Primaryp_k { get; set; }
        [XmlIgnoreAttribute]
        public double PrimaryTotalTime { get; set; }
        [XmlIgnoreAttribute]
        public Boolean SecondaryChosenGreedy { get; set; }
        [XmlIgnoreAttribute]
        public double SecondaryTotalReward { get; set; }
        [XmlIgnoreAttribute]
        public double Secondaryp_k { get; set; }
        [XmlIgnoreAttribute]
        public double SecondaryTotalTime { get; set; }
        [XmlIgnoreAttribute]
        public int BackoffCounter { get; set; }




        public Node()
        {
            X = new int();
            Y = new int();
            Number = new int();
            Transmission_Power = new double();
            Transmit_Antenna_Gain = Math.Pow(10, 0.1);
            Receive_Antenna_Gain = Math.Pow(10, 0.1);
            Links = new List<Link>();
            Buffer = new List<Package>();
            Neightbors = new List<int>();
            PrimaryQLTable = new List<PrimaryQLPair>();


            IsReady = true;
            IsTransmitting = false;
            IsReceiving = false;
            IsSuccessful = true;
            WaitingTimer = new double();
            Iterations = new int();
            SlotLength = new double();
            CurrentSystemState = new List<Boolean>();
            LastSystemState = new List<Boolean>();
            CurrentPrimaryAction = new PrimaryAction();
            PrimaryChosenGreedy = false;
            PrimaryTotalReward = new double();
            Primaryp_k = new double();
            PrimaryTotalTime = new double();
            SecondaryChosenGreedy = false;
            SecondaryTotalReward = new double();
            Secondaryp_k = new double();
            SecondaryTotalTime = new double();
            BackoffCounter = new int();
            

        }

        public Node(SerializationInfo info, StreamingContext context)
        {
            
            X = (int)info.GetValue("X", typeof(int));
            Y = (int)info.GetValue("Y", typeof(int));
            Number = (int)info.GetValue("Number", typeof(int));
            Transmission_Power = (double)info.GetValue("Transmission Power", typeof(double));
            Transmit_Antenna_Gain = (double)info.GetValue("Transmit Antenna Gain", typeof(double));
            Receive_Antenna_Gain = (double)info.GetValue("Receive Antenna Gain", typeof(double));
            Neightbors = (List<int>)info.GetValue("Neighbors", typeof(List<int>));
            //Buffer = (List<Package>)info.GetValue("Buffer", typeof(List<Package>));
            Links = (List<Link>)info.GetValue("Buffer", typeof(List<Link>));
            PrimaryQLTable = (List<PrimaryQLPair>)info.GetValue("Primary Q Table", typeof(List<PrimaryQLPair>));

            IsReady = true;
            IsTransmitting = false;
            IsReceiving = false;
            IsSuccessful = true;
            WaitingTimer = new double();
            Iterations = new int();
            SlotLength = new double();
            CurrentSystemState = new List<Boolean>();
            LastSystemState = new List<Boolean>();
            CurrentPrimaryAction = new PrimaryAction();
            PrimaryChosenGreedy = false;
            PrimaryChosenGreedy = false;
            PrimaryTotalReward = new double();
            Primaryp_k = new double();
            PrimaryTotalTime = new double();
            SecondaryChosenGreedy = false;
            SecondaryTotalReward = new double();
            Secondaryp_k = new double();
            SecondaryTotalTime = new double();
            BackoffCounter = new int();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
            info.AddValue("X", X);
            info.AddValue("Y", Y);
            info.AddValue("Number", Number);
            info.AddValue("Transmission Power", Transmission_Power);
            info.AddValue("Transmit Antenna Gain", Transmit_Antenna_Gain);
            info.AddValue("Receive Antenna Gain", Receive_Antenna_Gain);
            info.AddValue("Neighbors", Neightbors);
            //info.AddValue("Buffer", Buffer);
            info.AddValue("Links", Links);
            info.AddValue("Primary Q Table", PrimaryQLTable);
        }

        public int CompareTo(Node other)
        {
            if (this.Number != 0 && other.Number != 0)
            {
                if (other == null)
                    return 1;
                else
                    return this.Number.CompareTo(other.Number);
            }
            else
            {
                return 0;
            }
        }
    }
}
