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
    public class PrimaryAction:ISerializable, IEquatable<PrimaryAction>
    {
        public int Chosen_Link { get; set; }
        public double PrimaryQ { get; set; }
        public double[] Action_Possibility { get; set; }
        public double Action_Possibility_value { get; set; }
        public Boolean IsVaild { get; set; }

        public PrimaryAction()
        {
            Chosen_Link = new int();
            Action_Possibility = new double[2];
            PrimaryQ = new double();
            Action_Possibility_value = new double();
            IsVaild = new Boolean();
        }

        public PrimaryAction(Link l)
        {
            Chosen_Link = l.Number;
            Action_Possibility = new double[2];
        }


        public PrimaryAction(SerializationInfo info, StreamingContext context)
        {
            Chosen_Link = (int)info.GetValue("Chosen Link", typeof(int));
            PrimaryQ = (double)info.GetValue("Primary Q", typeof(double));
            Action_Possibility = new double[2];
            Action_Possibility_value = new double();
            IsVaild = new Boolean();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Chosen Link", Chosen_Link);
            info.AddValue("Primary Q", PrimaryQ);
        }

        public bool Equals(PrimaryAction other)
        {
            if (this.Chosen_Link == other.Chosen_Link)
                return true;
            else
                return false;

        }
    }
}
