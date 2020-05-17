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
    public class SecondaryQLPair: ISerializable
    {
        public List<Boolean> SystemState { get; set; }
        public List<SecondaryAction> SecondaryActionSpace { get; set; }

        public SecondaryQLPair()
        {
            SystemState = new List<Boolean>();
            SecondaryActionSpace = new List<SecondaryAction>();
        }

        public SecondaryQLPair(Node n, List<Boolean> CurrentSecondarySystemState)
        {
            SystemState = CurrentSecondarySystemState.ToList();
            SecondaryActionSpace = new List<SecondaryAction>();

            double[] DataRates = new double[9] { 0, 6, 9, 12, 18, 24, 36, 48, 54 };
            for(int i =0;i<=8;i++)
            {
                SecondaryAction a = new SecondaryAction();
                a.DataRate = DataRates[i]*1000/8;
                a.SecondaryQ = 0;
                SecondaryActionSpace.Add(a);
            }
        }

        public SecondaryQLPair(SerializationInfo info, StreamingContext context)
        {
            SystemState = (List<Boolean>)info.GetValue("System State", typeof(List<Boolean>));
            SecondaryActionSpace = (List<SecondaryAction>)info.GetValue("Secondary Action Space", typeof(List<SecondaryAction>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("System State", SystemState);
            info.AddValue("Secondary Action Space", SecondaryActionSpace);
        }

    }
}
