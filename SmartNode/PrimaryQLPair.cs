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
    public class PrimaryQLPair:ISerializable,IEquatable<PrimaryQLPair>
    {
        public List<Boolean> SystemState { get; set; }
        public List<PrimaryAction> PrimaryActionSpace { get; set; }

        public PrimaryQLPair()
        {
            SystemState = new List<Boolean>();
            PrimaryActionSpace = new List<PrimaryAction>();
        }

        public PrimaryQLPair(Node n, List<Boolean> CurrentSystemState)
        {
            SystemState = CurrentSystemState.ToList();
            PrimaryActionSpace = new List<PrimaryAction>();

            foreach (Link l in n.Links)
            {
                PrimaryAction a = new PrimaryAction();
                a.Chosen_Link = l.Number;
                a.PrimaryQ = 0;
                PrimaryActionSpace.Add(a);
            }          
        }

        public PrimaryQLPair(SerializationInfo info, StreamingContext context)
        {
            SystemState = (List<Boolean>)info.GetValue("Primary System State", typeof(List<Boolean>));
            PrimaryActionSpace = (List<PrimaryAction>)info.GetValue("Primary Action Space", typeof(List<PrimaryAction>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Primary System state", SystemState);
            info.AddValue("Primary Action Space", PrimaryActionSpace);
        }

        public bool Equals(PrimaryQLPair other)
        {
            if (this.SystemState.SequenceEqual(other.SystemState))
                return true;
            else
                return false;

        }
    }
}
