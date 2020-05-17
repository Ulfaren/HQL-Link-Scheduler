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
    public class SecondaryAction:ISerializable, IEquatable<SecondaryAction>
    {
        public double DataRate { get; set; }
        public double SecondaryQ { get; set; }
        public double[] Action_Possibility { get; set; }

        public SecondaryAction()
        {
            DataRate = new double();
            SecondaryQ = new double();
            Action_Possibility = new double[2];
        }

        public SecondaryAction(SerializationInfo info, StreamingContext context)
        {
            DataRate = (double)info.GetValue("Data Rate", typeof(double));
            SecondaryQ = (double)info.GetValue("Secondary Q", typeof(double));
        }

        public bool Equals(SecondaryAction other)
        {
            if (this.DataRate == other.DataRate)
                return true;
            else
                return false;                    
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Data Rate", DataRate);
            info.AddValue("Secondary Q", SecondaryQ);
        }
    }
}
