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
    public class Link : Topology , ISerializable, IEquatable<Link>
    {
        public int Number { get; set; }
        public int Receiver { get; set; }
        public int Transmitter { get; set; }
        public double Channel_gain { get; set; }
        public double Distance { get; set; }
        public List<SecondaryQLPair> SecondaryQLTable { get; set; }

        [XmlIgnoreAttribute]
        public Boolean Occupy;
        [XmlIgnoreAttribute]
        public int ActivitedTime;
        [XmlIgnoreAttribute]
        public SecondaryAction CurrentSecondaryAction { get; set; }
        [XmlIgnoreAttribute]
        public SecondaryAction LastSecondaryAction { get; set; }




        public Link()
        {
            Receiver = new int();
            Transmitter = new int();
            Number = new int();
            Distance = new double();
            Channel_gain = new double();
            SecondaryQLTable = new List<SecondaryQLPair>();
            Occupy = false;
            ActivitedTime = 0;
            CurrentSecondaryAction = new SecondaryAction();
            LastSecondaryAction = new SecondaryAction();
        }

        public Link(Node transmitter, Node receiver)
        {
            Receiver = receiver.Number;
            Transmitter = transmitter.Number;
            transmitter.Links.Add(this);
            Number = transmitter.Links.Count-1;
            Distance = CalculateDistance(transmitter, receiver);
            double l_0 = transmitter.Transmit_Antenna_Gain * receiver.Receive_Antenna_Gain * Math.Pow((LightSspeed / (4 * Frequency * Math.PI * 1)), 2);
            double pt = 0.1;
            double pr = 0.1 * l_0;
            double L_0 = 10 * Math.Log10(pt / pr);
            double c_0 = Math.Pow(1, 2) * Math.Pow(10, (-L_0 / 10));
            double F_g = 1;
            Channel_gain = (c_0 * F_g) / (Math.Pow((CalculateDistance(transmitter, receiver)), 2));
            SecondaryQLTable = new List<SecondaryQLPair>();
            Occupy = false;
            ActivitedTime = 0;
            CurrentSecondaryAction = new SecondaryAction();
            LastSecondaryAction = new SecondaryAction();
        }

        public Link(SerializationInfo info, StreamingContext context)
        {
            Number = (int)info.GetValue("Number", typeof(int));
            Receiver = (int)info.GetValue("Receiver", typeof(int));
            Transmitter = (int)info.GetValue("Transmitter", typeof(int));
            Channel_gain = (double)info.GetValue("Channel Gain", typeof(double));
            Distance = (double)info.GetValue("Distance", typeof(double));
            SecondaryQLTable = (List<SecondaryQLPair>)info.GetValue("Secondary Q Table", typeof(List<SecondaryQLPair>));
            Occupy = false;
            ActivitedTime = 0;
            CurrentSecondaryAction = new SecondaryAction();
            LastSecondaryAction = new SecondaryAction();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Number", Number);
            info.AddValue("Receiver", Receiver);
            info.AddValue("Transmitter", Transmitter);
            info.AddValue("Channel Gain", Channel_gain);
            info.AddValue("Distance", Distance);
            info.AddValue("Secondary Q Table", SecondaryQLTable);
        }

        public bool Equals(Link other)
        {
            if (other == null)
                return false;
            if(this.Number==other.Number&&this.Occupy==other.Occupy)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
