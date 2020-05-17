using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.IO;

namespace SmartNode
{
    class QL
    {

        public void QLearning(double Package_Arrive_Rate, Topology Topology, TextBox QL_Result, TextBox g1, TextBox g2)
        {
            Random rand = new Random();
            List<Package> TransmittedPackage = new List<Package>();
            int ContentionTime = 1;
            int ACKTime = Topology.Nodes.Count;
            double G1 = double.Parse(g1.Text);
            double G2 = double.Parse(g2.Text);

            Initialize(Topology);


            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Training");
            StreamWriter SAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training/SAverageReward.txt");
            StreamWriter PAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training/PAverageReward.txt");
            StreamWriter RThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training/RThroughPut.txt");
            StreamWriter AThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training/AThroughPut.txt");

            int k = new int();
            bool IsTrained = new bool();

            while (IsTrained==false)
            {
                k = k + 1;
                double current_noise = Topology.Noise;

                GivePackage(Topology, rand, Package_Arrive_Rate, k,20,20);

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 1;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                GiveReward(Topology, k,SAverageReward,PAverageReward, TransmittedPackage);

                int[] Min_iteration = new int[Topology.Nodes.Count];
                foreach(Node n in Topology.Nodes)
                {
                    Min_iteration[Topology.Nodes.IndexOf(n)] = n.Iterations;
                }

                if((G1 / (G2 + Min_iteration.Min()))<=0.01)
                {
                    IsTrained = true;
                }
                else
                {
                    IsTrained = false;
                }

                ChooseActions(Topology, rand, k, G1, G2, ContentionTime, ACKTime);

                UpdateWaitTimer(Topology);

                IsTransmissionSuccessfull(Topology, current_noise,rand);

            }

            QL_Result.Clear();
            foreach(Node n in Topology.Nodes)
            {
                QL_Result.AppendText("Node: " + n.Number.ToString() +" Total Reward: "+n.SecondaryTotalReward+" Total Iterations: "+n.Iterations.ToString()+"\n");
                foreach(PrimaryQLPair pair in n.PrimaryQLTable)
                {
                    QL_Result.AppendText("System State: ");
                    foreach(bool b in pair.SystemState)
                    {
                        QL_Result.AppendText(b.ToString() + " ");
                    }
                    QL_Result.AppendText("\n");

                    double[] maxPrimaryQ = new double[pair.PrimaryActionSpace.Count];
                    double MaxPrimaryQ = new double();
                    int i = 0;
                    foreach(PrimaryAction a in pair.PrimaryActionSpace)
                    {
                        maxPrimaryQ[i] = a.PrimaryQ;
                        i = i + 1;
                    }
                    MaxPrimaryQ = maxPrimaryQ.Max();
                    foreach(PrimaryAction a in pair.PrimaryActionSpace)
                    {
                        if(a.PrimaryQ==MaxPrimaryQ)
                        {
                            QL_Result.AppendText("Best Primary Action: Choose Link " + a.Chosen_Link.ToString()+" Q: "+a.PrimaryQ.ToString()+"\n");
                            foreach(Link l in n.Links)
                            {
                                if(l.Number==a.Chosen_Link)
                                {
                                    foreach(SecondaryQLPair pair2 in l.SecondaryQLTable)
                                    {
                                        if(pair2.SystemState.SequenceEqual(pair.SystemState.ToList()))
                                        {
                                            double[] maxSecondaryQ = new double[pair2.SecondaryActionSpace.Count];
                                            double MaxSecondaryQ = new double();
                                            int j = 0;
                                            foreach(SecondaryAction a2 in pair2.SecondaryActionSpace)
                                            {
                                                maxSecondaryQ[j] = a2.SecondaryQ;
                                                j = j + 1;
                                            }
                                            MaxSecondaryQ = maxSecondaryQ.Max();
                                            foreach (SecondaryAction a2 in pair2.SecondaryActionSpace)
                                            {
                                                if (MaxSecondaryQ == a2.SecondaryQ)
                                                {
                                                    QL_Result.AppendText("Best Secondary Action: Choose Data Rate " + a2.DataRate.ToString() + " Q: " + a2.SecondaryQ.ToString() + "\n");
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }


                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                QL_Result.AppendText("\n");
                QL_Result.AppendText("\n");
            }

            for (int j = 0; j <= k; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut.WriteLine(throughput.ToString());
            }

            RThroughPut.Close();

            for (int j = 0; j <= k; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut.WriteLine(throughput.ToString());
            }
            AThroughPut.Close();

        }

        public void QLearningMul(double Package_Arrive_Rate, Topology Topology, TextBox g1, TextBox g2, int x)
        {
            Random rand = new Random();
            List<Package> TransmittedPackage = new List<Package>();
            int ContentionTime = 1;
            int ACKTime = Topology.Nodes.Count;
            double G1 = double.Parse(g1.Text);
            double G2 = double.Parse(g2.Text);

            Initialize(Topology);


            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Training"+x.ToString());
            StreamWriter SAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training"+x.ToString()+"/SAverageReward.txt");
            StreamWriter PAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training" + x.ToString() + "/PAverageReward.txt");
            StreamWriter RThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training" + x.ToString() + "/RThroughPut.txt");
            StreamWriter AThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training" + x.ToString() + "/AThroughPut.txt");
            StreamWriter QTable = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Training" + x.ToString() + "/Qtable.txt");

            int k = new int();
            bool IsTrained = new bool();

            while (IsTrained == false)
            {
                k = k + 1;
                double current_noise = Topology.Noise;

                GivePackage(Topology, rand, Package_Arrive_Rate, k, 20, 20);

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 1;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                GiveReward_Mul(Topology, k, SAverageReward, PAverageReward, TransmittedPackage);

                int[] Min_iteration = new int[Topology.Nodes.Count];
                foreach (Node n in Topology.Nodes)
                {
                    Min_iteration[Topology.Nodes.IndexOf(n)] = n.Iterations;
                }

                Console.WriteLine("Training Topology "+x.ToString()+ "..............."+(((Min_iteration.Min())/((G1*100)-G2))*100).ToString()+"%");

                if ((G1 / (G2 + Min_iteration.Min())) <= 0.01)
                {
                    IsTrained = true;
                }
                else
                {
                    IsTrained = false;
                }

                ChooseActions_Mul(Topology, rand, k, G1, G2, ContentionTime, ACKTime);

                UpdateWaitTimer(Topology);

                IsTransmissionSuccessfull(Topology, current_noise, rand);


            }
            

            for (int j = 0; j <= k; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut.WriteLine(throughput.ToString());
            }

            RThroughPut.Close();

            for (int j = 0; j <= k; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut.WriteLine(throughput.ToString());
            }
            AThroughPut.Close();

            foreach (Node n in Topology.Nodes)
            {
                QTable.Write("Node: " + n.Number.ToString() + " Total Reward: " + n.SecondaryTotalReward + " Total Iterations: " + n.Iterations.ToString() + "\n");
                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                {
                    QTable.Write("System State: ");
                    foreach (bool b in pair.SystemState)
                    {
                        QTable.Write(b.ToString() + " ");
                    }
                    QTable.Write("\n");

                    double[] maxPrimaryQ = new double[pair.PrimaryActionSpace.Count];
                    double MaxPrimaryQ = new double();
                    int i = 0;
                    foreach (PrimaryAction a in pair.PrimaryActionSpace)
                    {
                        maxPrimaryQ[i] = a.PrimaryQ;
                        i = i + 1;
                    }
                    MaxPrimaryQ = maxPrimaryQ.Max();
                    foreach (PrimaryAction a in pair.PrimaryActionSpace)
                    {
                        if (a.PrimaryQ == MaxPrimaryQ)
                        {
                            QTable.Write("Best Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q: " + a.PrimaryQ.ToString() + "\n");
                            foreach (Link l in n.Links)
                            {
                                if (l.Number == a.Chosen_Link)
                                {
                                    foreach (SecondaryQLPair pair2 in l.SecondaryQLTable)
                                    {
                                        if (pair2.SystemState.SequenceEqual(pair.SystemState.ToList()))
                                        {
                                            double[] maxSecondaryQ = new double[pair2.SecondaryActionSpace.Count];
                                            double MaxSecondaryQ = new double();
                                            int j = 0;
                                            foreach (SecondaryAction a2 in pair2.SecondaryActionSpace)
                                            {
                                                maxSecondaryQ[j] = a2.SecondaryQ;
                                                j = j + 1;
                                            }
                                            MaxSecondaryQ = maxSecondaryQ.Max();
                                            foreach (SecondaryAction a2 in pair2.SecondaryActionSpace)
                                            {
                                                if (MaxSecondaryQ == a2.SecondaryQ)
                                                {
                                                    QTable.Write("Best Secondary Action: Choose Data Rate " + a2.DataRate.ToString() + " Q: " + a2.SecondaryQ.ToString() + "\n");
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }


                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                QTable.Write("\n");
                QTable.Write("\n");

            }
            QTable.Close();
        }

        public void Simulate_QL(int Time, double Package_Arrive_Rate, Topology Topology)
        {
            Random rand = new Random();
            int ContentionTime = 1;
            int ACKTime = Topology.Nodes.Count;
            List<Package> TransmittedPackage = new List<Package>();
            List<Package> TransmittedPackage2 = new List<Package>();

            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL");
            StreamWriter SAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/SAverageReward.txt");
            StreamWriter PAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/PAverageReward.txt");
            StreamWriter RThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/RThroughPut.txt");
            StreamWriter AThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/AThroughPut.txt");

            /*
            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA");
            StreamWriter RThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA/RThroughPut.txt");
            StreamWriter AThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA/AThroughPut.txt");
            */

            double G1 = 100;
            double G2 = 200;

            /*
            Topology topology_csma = new Topology();
            topology_csma=Topology;
            */

            Initialize(Topology);

            
            // Initialize(topology_csma);

            for (int i = 0; i <= Time; i++)
            {

                double current_noise = Topology.Noise;

                GivePackage(Topology, rand, Package_Arrive_Rate, i,20,65535);

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 0;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                GiveReward_Trained(Topology, i, SAverageReward, PAverageReward, TransmittedPackage);

                ChooseActions2(Topology, rand, i, G1, G2, ContentionTime, ACKTime);

                UpdateWaitTimer(Topology);

                IsTransmissionSuccessfull(Topology, current_noise,rand);

            }

            PAverageReward.Close();
            SAverageReward.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut.WriteLine(throughput.ToString());
            }

            RThroughPut.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut.WriteLine(throughput.ToString());
            }
            AThroughPut.Close();

            /*
            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut2.WriteLine(throughput.ToString());
            }

            RThroughPut2.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut2.WriteLine(throughput.ToString());
            }
            AThroughPut2.Close();
            */
        }

        public void Simulate_CSMA(int Time, double Package_Arrive_Rate, Topology Topology)
        {
            Random rand = new Random();
            int ContentionTime = 1;
            int ACKTime = Topology.Nodes.Count;
            List<Package> TransmittedPackage2 = new List<Package>();

            
            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA");
            StreamWriter RThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA/RThroughPut.txt");
            StreamWriter AThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA/AThroughPut.txt");
            

            
            Topology topology_csma = new Topology();
            topology_csma=Topology;


            Initialize(topology_csma);

            for (int i = 0; i <= Time; i++)
            {

                double current_noise = Topology.Noise;

                GivePackage(topology_csma, rand, Package_Arrive_Rate, i, 20, 65535);

                
                foreach (Node n in topology_csma.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        n.SlotLength = n.SlotLength - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 0;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                

                GiveReward_CSMA(topology_csma, TransmittedPackage2, i,rand);

                ChooseAction_CSMA(topology_csma, rand, i, current_noise, ContentionTime, ACKTime);

                foreach(Node n in topology_csma.Nodes)
                {
                    if(n.WaitingTimer>0)
                    {
                        n.IsReady = false;
                    }
                }

                IsTransmissionSuccessfull(topology_csma, current_noise,rand);
            }      

            
            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut2.WriteLine(throughput.ToString());
            }

            RThroughPut2.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut2.WriteLine(throughput.ToString());
            }
            AThroughPut2.Close();
            
        }

        public void Simulate(int Time, double Package_Arrive_Rate, Topology Topology)
        {
            Random rand = new Random();
            int ContentionTime = 1;
            int ACKTime = Topology.Nodes.Count;
            List<Package> TransmittedPackage = new List<Package>();
            List<Package> TransmittedPackage2 = new List<Package>();

            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL");
            StreamWriter SAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/SAverageReward.txt");
            StreamWriter PAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/PAverageReward.txt");
            StreamWriter RThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/RThroughPut.txt");
            StreamWriter AThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/QL/AThroughPut.txt");

            
            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA");
            StreamWriter RThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA/RThroughPut.txt");
            StreamWriter AThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate/CSMA/AThroughPut.txt");
            

            double G1 = 100;
            double G2 = 200;

            
            Topology topology_csma = new Topology();
            topology_csma=Topology;
            

            Initialize(Topology);
            Initialize(topology_csma);

            List<Package> PList = new List<Package>();
            List<double> NList = new List<double>();

            for(int i=0;i<=Time;i++)
            {
                double current_noise = SimpleGaussian(rand, Topology.Noise, Topology.NoiseVar);
                NList.Add(current_noise);

                foreach (Node n in Topology.Nodes)
                {
                    if (rand.NextDouble() <= Package_Arrive_Rate)
                    {
                        Package p = new Package(n, rand, 20, 65535);
                        p.Start_Time = i;
                        PList.Add(p);
                    }
                }
            }

            Initialize(Topology);
            Initialize(topology_csma);

            Console.WriteLine("################################ QL simulation ################################");
            for (int i = 0; i <= Time; i++)
            {

                double current_noise = Topology.Noise;

                foreach (Node n in Topology.Nodes)
                {
                    foreach (Package p in PList)
                    {
                        if (p.Start_Time == i && p.Node==n.Number)
                        {
                            n.Buffer.Add(p);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 0;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                GiveReward_Trained(Topology, i, SAverageReward, PAverageReward, TransmittedPackage);

                ChooseActions2(Topology, rand, i, G1, G2, ContentionTime, ACKTime);

                UpdateWaitTimer(Topology);

                IsTransmissionSuccessfull(Topology, current_noise,rand);

            }

            PAverageReward.Close();
            SAverageReward.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut.WriteLine(throughput.ToString());
            }

            RThroughPut.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut.WriteLine(throughput.ToString());
            }
            AThroughPut.Close();

            Initialize(Topology);
            Initialize(topology_csma);

            Console.WriteLine("################################ CSMA simulation ################################");
            for (int i=0;i<=Time;i++)
            {
                double current_noise = topology_csma.Noise;
                foreach (Node n in topology_csma.Nodes)
                {
                    foreach (Package p in PList)
                    {
                        if (p.Start_Time == i && p.Node == n.Number)
                        {
                            n.Buffer.Add(p);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                foreach (Node n in topology_csma.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        n.SlotLength = n.SlotLength - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 0;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }


                GiveReward_CSMA(topology_csma, TransmittedPackage2, i,rand);

                ChooseAction_CSMA(topology_csma, rand, i, current_noise, ContentionTime, ACKTime);

                foreach (Node n in topology_csma.Nodes)
                {
                    if (n.WaitingTimer > 0)
                    {
                        n.IsReady = false;
                    }
                }

                IsTransmissionSuccessfull(topology_csma, current_noise,rand);

            }

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut2.WriteLine(throughput.ToString());
            }

            RThroughPut2.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut2.WriteLine(throughput.ToString());
            }
            AThroughPut2.Close();

        }

        public void Simulate_Mul(int Time, double Package_Arrive_Rate, Topology Topology, int x)
        {
            Random rand = new Random();
            int ContentionTime = 1;
            int ACKTime = Topology.Nodes.Count;
            List<Package> TransmittedPackage = new List<Package>();
            List<Package> TransmittedPackage2 = new List<Package>();

            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Simulate"+x.ToString()+"/QL");
            StreamWriter SAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/QL/SAverageReward.txt");
            StreamWriter PAverageReward = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/QL/PAverageReward.txt");
            StreamWriter RThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/QL/RThroughPut.txt");
            StreamWriter AThroughPut = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/QL/AThroughPut.txt");
            StreamWriter Log_QL = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/QL/Log_QL.txt");

            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/CSMA");
            StreamWriter RThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/CSMA/RThroughPut.txt");
            StreamWriter AThroughPut2 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/CSMA/AThroughPut.txt");
            StreamWriter Log_CSMA = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "/Simulate" + x.ToString() + "/CSMA/Log_CSMA.txt");


            double G1 = 100;
            double G2 = 200;



            Topology topology_csma = new Topology();
            topology_csma = Topology;


            Initialize(Topology);
            Initialize(topology_csma);

            List<Package> PList = new List<Package>();
            List<ChannelCondition> CCList = new List<ChannelCondition>();
           
            for (int i = 0; i <= Time; i++)
            {
                foreach (Node n in Topology.Nodes)
                {
                    if (rand.NextDouble() <= Package_Arrive_Rate)
                    {
                        foreach(Link l in n.Links)
                        {
                            Package p = new Package(n, l, rand, 20, 65535)
                            {
                                Start_Time = i
                            };
                            PList.Add(p);
                        }
                    }
                }

                
                foreach(Node n in Topology.Nodes)
                {
                    foreach(Node m in Topology.Nodes)
                    {
                        if (n.Number != m.Number)
                        {
                            ChannelCondition cc = new ChannelCondition();
                            cc.Time = i;
                            cc.N1 = n.Number;
                            cc.N2 = m.Number;
                            double l_0 = n.Transmit_Antenna_Gain * m.Receive_Antenna_Gain * Math.Pow((Topology.LightSspeed / (4 * Topology.Frequency * Math.PI * 1)), 2);
                            double pt = 0.1;
                            double pr = 0.1 * l_0;
                            double L_0 = 10 * Math.Log10(pt / pr);
                            double c_0 = Math.Pow(1, 2) * Math.Pow(10, (-L_0 / 10));
                            double F_g = Math.Pow(10, -(SimpleGaussian(rand, 0, Topology.NoiseVar) / 10));
                            double PL = (c_0 * F_g) / (Math.Pow((Topology.CalculateDistance(n, m)), 2));
                            cc.Gain = PL;
                            CCList.Add(cc);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                
            }
            

            Initialize(Topology);
            Initialize(topology_csma);

            Console.WriteLine("################################ QL simulation "+"Topology "+x.ToString()+" ################################");
            for (int i = 0; i <= Time; i++)
            {

                double current_noise = Topology.Noise;

                foreach (Node n in Topology.Nodes)
                {
                    foreach (Package p in PList)
                    {
                        if (p.Start_Time == i && p.Node == n.Number)
                        {
                            n.Buffer.Add(p);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 0;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                GiveReward_Trained_Mul(Topology, i, SAverageReward, PAverageReward, TransmittedPackage, Log_QL);

                Console.WriteLine("QL simulating " + "Topology " + x.ToString() + "..............." + i.ToString()+"ms");

                ChooseActions2_Mul(Topology, rand, i, G1, G2, ContentionTime, ACKTime, Log_QL);

                UpdateWaitTimer(Topology);

                IsTransmissionSuccessfull_Mul(Topology, current_noise,CCList,i, Log_QL);
            }

            Log_QL.Close();
            PAverageReward.Close();
            SAverageReward.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut.WriteLine(throughput.ToString());
            }

            RThroughPut.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut.WriteLine(throughput.ToString());
            }
            AThroughPut.Close();

            Initialize(Topology);
            Initialize(topology_csma);

            Console.WriteLine("################################ CSMA simulation " + "Topology " + x.ToString() + " ################################");
            for (int i = 0; i <= Time; i++)
            {
                double current_noise = topology_csma.Noise;
                foreach (Node n in topology_csma.Nodes)
                {
                    foreach (Package p in PList)
                    {
                        if (p.Start_Time == i && p.Node == n.Number)
                        {
                            n.Buffer.Add(p);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                foreach (Node n in topology_csma.Nodes)
                {
                    if (n.IsReady == false)
                    {
                        n.WaitingTimer = n.WaitingTimer - 1;
                        n.SlotLength = n.SlotLength - 1;
                        if (n.WaitingTimer <= 0)
                        {
                            n.IsReady = true;
                            n.Iterations = n.Iterations + 0;
                        }
                        else
                        {
                            n.IsReady = false;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }


                GiveReward_CSMA_Mul(topology_csma, TransmittedPackage2, i, rand, Log_CSMA);

                Console.WriteLine("CSMA simulating " + "Topology " + x.ToString() + "..............." + i.ToString()+"ms");

                ChooseAction_CSMA_Mul(topology_csma, rand, i, current_noise, ContentionTime, ACKTime, CCList,i, Log_CSMA);

                foreach (Node n in topology_csma.Nodes)
                {
                    if (n.WaitingTimer > 0)
                    {
                        n.IsReady = false;
                    }
                }

                IsTransmissionSuccessfull_Mul(topology_csma, current_noise,CCList,i,Log_CSMA);
            }

            Log_CSMA.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.Start_Time <= j && p.End_Time >= j)
                    {
                        throughput = throughput + p.Data_Rate;
                    }
                    else
                    {
                        continue;
                    }
                }
                RThroughPut2.WriteLine(throughput.ToString());
            }

            RThroughPut2.Close();

            for (int j = 0; j <= Time; j++)
            {
                double throughput = new double();
                double totalDataLength = new double();
                foreach (Package p in TransmittedPackage2)
                {
                    if (p.End_Time <= j)
                    {
                        totalDataLength = totalDataLength + p.Data_Length;
                    }
                    else
                    {
                        continue;
                    }
                }
                throughput = totalDataLength / j;
                AThroughPut2.WriteLine(throughput.ToString());
            }
            AThroughPut2.Close();

        }

        public void Initialize(Topology Topology)
        {
            foreach(Node n in Topology.Nodes)
            {
                n.Buffer.Clear();
            }
        }

        public void GivePackage(Topology Topology, Random rand, double Package_Arrive_Rate, int k,int length_min, int length_max)
        {
            foreach (Node n in Topology.Nodes)
            {
                if (rand.NextDouble() <= Package_Arrive_Rate)
                {
                    foreach (Link l in n.Links)
                    {
                        Package p = new Package(n, l, rand, length_min, length_max);
                        n.Buffer.Add(p);
                    }
                    /*
                    Console.WriteLine();
                    Console.WriteLine("#" + k.ToString() + ": New package for Link: (N" + n.Links.ElementAt(p.Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(p.Link).Receiver.ToString() + ") with Data Length: " + p.Data_Length.ToString() + " arrived on Node " + n.Number.ToString());
                    */
                    foreach (Package package in n.Buffer)
                    {
                        foreach (Link l in n.Links)
                        {
                            if (l.Occupy == true)
                            {
                                continue;
                            }
                            else
                            {
                                if (package.Link == l.Number)
                                {
                                    l.Occupy = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ChooseActions(Topology Topology, Random rand, int k, double G1, double G2, int ContentionTime, int ACKTime)
        {
            /*
            foreach(Node n in Topology.Nodes)
            {
                if (n.IsReady == true && n.IsTransmitting == false &&n.Buffer.Count!=0)
                {
                    n.LastSystemState = n.CurrentSystemState;
                    n.CurrentSystemState.Clear();
                    foreach (int m in n.Neightbors)
                    {
                        n.CurrentSystemState.Add(Topology.Nodes.ElementAt(m).IsTransmitting);
                    }


                    Boolean IsThereNewSystemState = true;
                    int CurrentSystemState_index = new int();
                    foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                    {
                        if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                        {
                            IsThereNewSystemState = false;
                            CurrentSystemState_index = n.PrimaryQLTable.IndexOf(pair);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (IsThereNewSystemState)
                    {
                        PrimaryQLPair newPrimaryQLPair = new PrimaryQLPair(n, n.CurrentSystemState);
                        foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                        {
                            foreach (Link l in n.Links)
                            {
                                if (a.Chosen_Link == l.Number)
                                {
                                    if (l.Occupy == true)
                                    {
                                        a.IsVaild = true;
                                        a.Action_Possibility_value = 1 - (G1 / (G2 + n.Iterations));
                                    }
                                    else
                                    {
                                        a.IsVaild = false;
                                        a.Action_Possibility_value = 0;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }

                        double TotalActionPossibilityValue = new double();
                        foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                        {
                            TotalActionPossibilityValue = TotalActionPossibilityValue + a.Action_Possibility_value;
                        }

                        foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                        {
                            if (newPrimaryQLPair.PrimaryActionSpace.IndexOf(a) == 0)
                            {
                                a.Action_Possibility[0] = 0;
                                a.Action_Possibility[1] = a.Action_Possibility[0] + (a.Action_Possibility_value / TotalActionPossibilityValue);
                            }
                            else
                            {
                                a.Action_Possibility[0] = newPrimaryQLPair.PrimaryActionSpace.ElementAt(newPrimaryQLPair.PrimaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                a.Action_Possibility[1] = a.Action_Possibility[0] + (a.Action_Possibility_value / TotalActionPossibilityValue);
                            }
                        }

                        double r = rand.NextDouble();
                        foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                        {
                            if (r >= a.Action_Possibility[0] && r < a.Action_Possibility[1])
                            {
                                n.CurrentPrimaryAction = a;

                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        n.PrimaryQLTable.Add(newPrimaryQLPair);
                        n.PrimaryChosenGreedy = true;
                    }
                    else
                    {
                        double Max_Q = 0;
                        foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                        {
                            if (Max_Q < a.PrimaryQ)
                            {
                                Max_Q = a.PrimaryQ;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        double greedy = (1 - (G1 / (G2 + n.Iterations)));
                        double non_greedy = (G1 / (G2 + n.Iterations));

                        foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                        {
                            foreach (Link l in n.Links)
                            {
                                if (a.Chosen_Link == l.Number)
                                {
                                    if (l.Occupy == true)
                                    {
                                        if (a.PrimaryQ == Max_Q)
                                        {
                                            a.IsVaild = true;
                                            a.Action_Possibility_value = greedy;
                                        }
                                        else
                                        {
                                            a.IsVaild = true;
                                            a.Action_Possibility_value = non_greedy;
                                        }
                                    }
                                    else
                                    {
                                        a.IsVaild = false;
                                        a.Action_Possibility_value = 0;
                                    }
                                }
                                else
                                {
                                    continue;
                                }

                            }
                        }

                        double TotalActionPossibilityValue = new double();
                        foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                        {
                            TotalActionPossibilityValue = TotalActionPossibilityValue + a.Action_Possibility_value;
                        }

                        foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                        {
                            if (n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.IndexOf(a) == 0)
                            {
                                a.Action_Possibility[0] = 0;
                                a.Action_Possibility[1] = a.Action_Possibility[0] + a.Action_Possibility_value / TotalActionPossibilityValue;
                            }
                            else
                            {
                                a.Action_Possibility[0] = n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.ElementAt((n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.IndexOf(a) - 1)).Action_Possibility[1];
                                a.Action_Possibility[1] = a.Action_Possibility[0] + a.Action_Possibility_value / TotalActionPossibilityValue;
                            }

                        }

                        double r = rand.NextDouble();
                        foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                        {
                            if (r >= a.Action_Possibility[0] && r < a.Action_Possibility[1])
                            {
                                n.CurrentPrimaryAction = a;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if(n.CurrentPrimaryAction.PrimaryQ==Max_Q)
                        {
                            n.PrimaryChosenGreedy = true;
                        }
                        else
                        {
                            n.PrimaryChosenGreedy = false;
                        }

                    }

                    foreach (Link l in n.Links)
                    {

                        if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                        {
                            Boolean IsThereNewSystemState2 = true;
                            int CurrentSystemState_index2 = new int();

                            foreach (SecondaryQLPair pair in l.SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    IsThereNewSystemState2 = false;
                                    CurrentSystemState_index2 = l.SecondaryQLTable.IndexOf(pair);
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            if (IsThereNewSystemState2)
                            {
                                SecondaryQLPair newSecondaryQLPair = new SecondaryQLPair(n, n.CurrentSystemState);
                                double gready = (1 - (G1 / (G2 + n.Iterations))) / ((1 - (G1 / (G2 + n.Iterations))) * newSecondaryQLPair.SecondaryActionSpace.Count);
                                foreach (SecondaryAction a in newSecondaryQLPair.SecondaryActionSpace)
                                {
                                    if (newSecondaryQLPair.SecondaryActionSpace.IndexOf(a) == 0)
                                    {
                                        a.Action_Possibility[0] = 0;
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + gready;
                                    }
                                    else
                                    {
                                        a.Action_Possibility[0] = newSecondaryQLPair.SecondaryActionSpace.ElementAt(newSecondaryQLPair.SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + gready;
                                    }
                                }

                                double r2 = rand.NextDouble();
                                foreach (SecondaryAction a in newSecondaryQLPair.SecondaryActionSpace)
                                {
                                    if (r2 >= a.Action_Possibility[0] && r2 < a.Action_Possibility[1])
                                    {
                                        l.CurrentSecondaryAction = a;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                l.SecondaryQLTable.Add(newSecondaryQLPair);
                                n.SecondaryChosenGreedy = true;
                                break;
                            }
                            else
                            {
                                double Max_Q2 = 0;
                                foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                {
                                    if (Max_Q2 < a.SecondaryQ)
                                    {
                                        Max_Q2 = a.SecondaryQ;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }


                                double greedy = (1 - (G1 / (G2 + n.Iterations))) / ((1 - (G1 / (G2 + n.Iterations))) + ((G1 / (G2 + n.Iterations)) * (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.Count - 1)));
                                double non_greedy = (G1 / (G2 + n.Iterations)) / ((1 - (G1 / (G2 + n.Iterations))) + ((G1 / (G2 + n.Iterations)) * (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.Count - 1)));

                                foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                {
                                    if (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) == 0)
                                    {
                                        if (a.SecondaryQ == Max_Q2)
                                        {
                                            a.Action_Possibility[0] = 0;
                                            a.Action_Possibility[1] = a.Action_Possibility[0] + greedy;
                                        }
                                        else
                                        {
                                            a.Action_Possibility[0] = 0;
                                            a.Action_Possibility[1] = a.Action_Possibility[0] + non_greedy;
                                        }
                                    }
                                    else
                                    {
                                        if (a.SecondaryQ == Max_Q2)
                                        {
                                            a.Action_Possibility[0] = l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.ElementAt(l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                            a.Action_Possibility[1] = a.Action_Possibility[0] + greedy;
                                        }
                                        else
                                        {
                                            a.Action_Possibility[0] = l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.ElementAt(l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                            a.Action_Possibility[1] = a.Action_Possibility[0] + non_greedy;
                                        }
                                    }
                                }

                                double r2 = rand.NextDouble();
                                foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                {
                                    if (r2 >= a.Action_Possibility[0] && r2 < a.Action_Possibility[1])
                                    {
                                        l.CurrentSecondaryAction = a;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.SecondaryQ==Max_Q2)
                                {
                                    n.SecondaryChosenGreedy = true;
                                }
                                else
                                {
                                    n.SecondaryChosenGreedy = false;
                                }
                                break;

                            }
                        }
                        else
                        {
                            continue;
                        }

                    }

                    foreach (Package p in n.Buffer)
                    {
                        if (p.Link == n.CurrentPrimaryAction.Chosen_Link)
                        {
                            foreach(Link l in n.Links)
                            {
                                if(l.Number==n.CurrentPrimaryAction.Chosen_Link)
                                {
                                    p.Data_Rate = l.CurrentSecondaryAction.DataRate;
                                    if (p.Data_Rate > 0)
                                    {
                                        p.IsBeingTransmitting = true;
                                        p.Start_Time = k;
                                        p.Transmission_Time = p.Data_Length / p.Data_Rate;
                                        n.IsTransmitting = true;
                                        n.WaitingTimer = ContentionTime + p.Transmission_Time + ACKTime;
                                        n.SlotLength = ContentionTime + p.Transmission_Time + ACKTime;
                                        foreach (Node m in Topology.Nodes)
                                        {
                                            if (m.Number == l.Receiver)
                                            {
                                                m.IsReceiving = true;
                                            }
                                        }


                                    }
                                    else
                                    {
                                        p.IsBeingTransmitting = false;
                                        p.Start_Time = new int();
                                        p.Transmission_Time = new double();
                                        n.IsTransmitting = false;
                                        n.WaitingTimer = new int();
                                        n.SlotLength = new int();
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    continue;
                }
            }

            */

            Topology.Nodes.Shuffle();

            bool temp = false;
            while(temp==false)
            {
                List<bool> systemp1 = new List<bool>();
                foreach(Node n in Topology.Nodes)
                {
                    systemp1.Add(n.IsTransmitting);
                }
                
                foreach(Node n in Topology.Nodes)
                {
                    if (n.IsReady == true)
                    {
                        List<bool> TrueSystemState = new List<bool>();

                        /*
                        foreach(Node m in Topology.Nodes)
                        {
                            if(m.Number==n.Number)
                            {
                                continue;
                            }
                            else
                            {
                                TrueSystemState.Add(m.IsTransmitting);
                            }
                        }
                        */
                        

                        /*
                        foreach(int m in n.Neightbors)
                        {
                            TrueSystemState.Add(Topology.Nodes.ElementAt(m).IsTransmitting);
                        }
                        */

                        foreach (Node m in Topology.Nodes)
                        {
                            if (n.Number != m.Number)
                            {
                                double d = Topology.CalculateDistance(n, m);
                                double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                                if (d <= (maximum_transmission_range * 1))
                                {
                                    TrueSystemState.Add(m.IsTransmitting);
                                }
                            }
                        }


                        /*
                        foreach (int m in n.Neightbors)
                        {
                            TrueSystemState.Add(systemp1.ElementAt(m));

                            //obtain neighbors' neighbors' state
                            foreach(int p in Topology.Nodes.ElementAt(m).Neightbors)
                            {
                                bool Isrepeated = false;
                                foreach (int q in n.Neightbors)
                                {
                                    if (p == q)
                                    {
                                        Isrepeated = true;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (Isrepeated == true)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (p == n.Number)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        TrueSystemState.Add(systemp1.ElementAt(p));
                                    }
                                }
                            }
                        }
                        */

                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                p.IsBeingTransmitting = false;
                                p.Start_Time = new int();
                                p.Transmission_Time = new double();
                                p.End_Time = new double();
                            }
                        }

                        if (n.IsReady == true && n.Buffer.Count != 0)
                        {
                            n.CurrentSystemState = TrueSystemState.ToList();
                            Boolean IsThereNewSystemState = true;
                            int CurrentSystemState_index = new int();
                            foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    IsThereNewSystemState = false;
                                    CurrentSystemState_index = n.PrimaryQLTable.IndexOf(pair);
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            if (IsThereNewSystemState)
                            {
                                PrimaryQLPair newPrimaryQLPair = new PrimaryQLPair(n, n.CurrentSystemState);
                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (a.Chosen_Link == l.Number)
                                        {
                                            if (l.Occupy == true)
                                            {
                                                a.IsVaild = true;
                                                a.Action_Possibility_value = 1 - (G1 / (G2 + n.Iterations));
                                            }
                                            else
                                            {
                                                a.IsVaild = false;
                                                a.Action_Possibility_value = 0;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }

                                double TotalActionPossibilityValue = new double();
                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    TotalActionPossibilityValue = TotalActionPossibilityValue + a.Action_Possibility_value;
                                }

                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    if (newPrimaryQLPair.PrimaryActionSpace.IndexOf(a) == 0)
                                    {
                                        a.Action_Possibility[0] = 0;
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + (a.Action_Possibility_value / TotalActionPossibilityValue);
                                    }
                                    else
                                    {
                                        a.Action_Possibility[0] = newPrimaryQLPair.PrimaryActionSpace.ElementAt(newPrimaryQLPair.PrimaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + (a.Action_Possibility_value / TotalActionPossibilityValue);
                                    }
                                }

                                double r = rand.NextDouble();
                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    if (r >= a.Action_Possibility[0] && r < a.Action_Possibility[1])
                                    {
                                        n.CurrentPrimaryAction = a;

                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                n.PrimaryQLTable.Add(newPrimaryQLPair);
                                n.PrimaryChosenGreedy = true;
                            }
                            else
                            {
                                double Max_Q = 0;
                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    if (Max_Q < a.PrimaryQ)
                                    {
                                        Max_Q = a.PrimaryQ;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                double greedy = (1 - (G1 / (G2 + n.Iterations)));
                                double non_greedy = (G1 / (G2 + n.Iterations));

                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (a.Chosen_Link == l.Number)
                                        {
                                            if (l.Occupy == true)
                                            {
                                                if (a.PrimaryQ == Max_Q)
                                                {
                                                    a.IsVaild = true;
                                                    a.Action_Possibility_value = greedy;
                                                }
                                                else
                                                {
                                                    a.IsVaild = true;
                                                    a.Action_Possibility_value = non_greedy;
                                                }
                                            }
                                            else
                                            {
                                                a.IsVaild = false;
                                                a.Action_Possibility_value = 0;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                    }
                                }

                                double TotalActionPossibilityValue = new double();
                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    TotalActionPossibilityValue = TotalActionPossibilityValue + a.Action_Possibility_value;
                                }

                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    if (n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.IndexOf(a) == 0)
                                    {
                                        a.Action_Possibility[0] = 0;
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + a.Action_Possibility_value / TotalActionPossibilityValue;
                                    }
                                    else
                                    {
                                        a.Action_Possibility[0] = n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.ElementAt((n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.IndexOf(a) - 1)).Action_Possibility[1];
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + a.Action_Possibility_value / TotalActionPossibilityValue;
                                    }

                                }

                                double r = rand.NextDouble();
                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    if (r >= a.Action_Possibility[0] && r < a.Action_Possibility[1])
                                    {
                                        n.CurrentPrimaryAction = a;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (n.CurrentPrimaryAction.PrimaryQ == Max_Q)
                                {
                                    n.PrimaryChosenGreedy = true;
                                }
                                else
                                {
                                    n.PrimaryChosenGreedy = false;
                                }

                            }

                            foreach (Link l in n.Links)
                            {

                                if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                {
                                    Boolean IsThereNewSystemState2 = true;
                                    int CurrentSystemState_index2 = new int();

                                    foreach (SecondaryQLPair pair in l.SecondaryQLTable)
                                    {
                                        if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                        {
                                            IsThereNewSystemState2 = false;
                                            CurrentSystemState_index2 = l.SecondaryQLTable.IndexOf(pair);
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    if (IsThereNewSystemState2)
                                    {
                                        SecondaryQLPair newSecondaryQLPair = new SecondaryQLPair(n, n.CurrentSystemState);
                                        double gready = (1 - (G1 / (G2 + n.Iterations))) / ((1 - (G1 / (G2 + n.Iterations))) * newSecondaryQLPair.SecondaryActionSpace.Count);
                                        foreach (SecondaryAction a in newSecondaryQLPair.SecondaryActionSpace)
                                        {
                                            if (newSecondaryQLPair.SecondaryActionSpace.IndexOf(a) == 0)
                                            {
                                                a.Action_Possibility[0] = 0;
                                                a.Action_Possibility[1] = a.Action_Possibility[0] + gready;
                                            }
                                            else
                                            {
                                                a.Action_Possibility[0] = newSecondaryQLPair.SecondaryActionSpace.ElementAt(newSecondaryQLPair.SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                                a.Action_Possibility[1] = a.Action_Possibility[0] + gready;
                                            }
                                        }

                                        double r2 = rand.NextDouble();
                                        foreach (SecondaryAction a in newSecondaryQLPair.SecondaryActionSpace)
                                        {
                                            if (r2 >= a.Action_Possibility[0] && r2 < a.Action_Possibility[1])
                                            {
                                                l.CurrentSecondaryAction = a;
                                                break;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        l.SecondaryQLTable.Add(newSecondaryQLPair);
                                        n.SecondaryChosenGreedy = true;
                                        break;
                                    }
                                    else
                                    {
                                        double Max_Q2 = 0;
                                        foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                        {
                                            if (Max_Q2 < a.SecondaryQ)
                                            {
                                                Max_Q2 = a.SecondaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }


                                        double greedy = (1 - (G1 / (G2 + n.Iterations))) / ((1 - (G1 / (G2 + n.Iterations))) + ((G1 / (G2 + n.Iterations)) * (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.Count - 1)));
                                        double non_greedy = (G1 / (G2 + n.Iterations)) / ((1 - (G1 / (G2 + n.Iterations))) + ((G1 / (G2 + n.Iterations)) * (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.Count - 1)));

                                        foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                        {
                                            if (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) == 0)
                                            {
                                                if (a.SecondaryQ == Max_Q2)
                                                {
                                                    a.Action_Possibility[0] = 0;
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + greedy;
                                                }
                                                else
                                                {
                                                    a.Action_Possibility[0] = 0;
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + non_greedy;
                                                }
                                            }
                                            else
                                            {
                                                if (a.SecondaryQ == Max_Q2)
                                                {
                                                    a.Action_Possibility[0] = l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.ElementAt(l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + greedy;
                                                }
                                                else
                                                {
                                                    a.Action_Possibility[0] = l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.ElementAt(l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + non_greedy;
                                                }
                                            }
                                        }

                                        double r2 = rand.NextDouble();
                                        foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                        {
                                            if (r2 >= a.Action_Possibility[0] && r2 < a.Action_Possibility[1])
                                            {
                                                l.CurrentSecondaryAction = a;
                                                break;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }

                                        if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.SecondaryQ == Max_Q2)
                                        {
                                            n.SecondaryChosenGreedy = true;
                                        }
                                        else
                                        {
                                            n.SecondaryChosenGreedy = false;
                                        }
                                        break;

                                    }
                                }
                                else
                                {
                                    continue;
                                }

                            }

                            foreach (Package p in n.Buffer)
                            {
                                if (p.Link == n.CurrentPrimaryAction.Chosen_Link)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                        {
                                            p.Data_Rate = l.CurrentSecondaryAction.DataRate;
                                            if (p.Data_Rate > 0)
                                            {
                                                p.IsBeingTransmitting = true;
                                                p.Start_Time = k;
                                                p.Transmission_Time = p.Data_Length / p.Data_Rate;
                                                n.IsTransmitting = true;
                                                n.WaitingTimer = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.SlotLength = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.IsSuccessful = true;
                                                foreach (Node m in Topology.Nodes)
                                                {
                                                    if (m.Number == l.Receiver)
                                                    {
                                                        m.IsReceiving = true;
                                                    }
                                                }


                                            }
                                            else
                                            {
                                                p.IsBeingTransmitting = false;
                                                p.Start_Time = new int();
                                                p.Transmission_Time = new double();
                                                n.IsTransmitting = false;
                                                n.WaitingTimer = new int();
                                                n.SlotLength = new int();
                                                n.IsSuccessful = true;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                List<bool> systemp2 = new List<bool>();
                foreach (Node n in Topology.Nodes)
                {
                    systemp2.Add(n.IsTransmitting);
                }

                if(systemp1.SequenceEqual(systemp2))
                {
                    temp = true;
                    foreach (Node n in Topology.Nodes)
                    {
                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                Console.WriteLine();
                                Console.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + p.Data_Length.ToString() + " on Link " + n.CurrentPrimaryAction.Chosen_Link.ToString() + ": (N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Receiver.ToString() + ") at Data Rate: " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() +"............");
                            }
                        }
                    }
                    break;
                }
                else
                {
                    temp = false;
                }
            }
            List<Node> temp_nodes = new List<Node>();
            temp_nodes = Topology.Nodes.ToList();
            Topology.Nodes.Clear();
            for(int i =0;i<= temp_nodes.Count;i++)
            {
                foreach(Node n in temp_nodes)
                {
                    if(n.Number==i)
                    {
                        Topology.Nodes.Add(n);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

        }

        public void ChooseActions_Mul(Topology Topology, Random rand, int k, double G1, double G2, int ContentionTime, int ACKTime)
        {
            

            Topology.Nodes.Shuffle();

            bool temp = false;
            while (temp == false)
            {
                List<bool> systemp1 = new List<bool>();
                foreach (Node n in Topology.Nodes)
                {
                    systemp1.Add(n.IsTransmitting);
                }

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == true)
                    {
                        List<bool> TrueSystemState = new List<bool>();

                    

                        foreach (Node m in Topology.Nodes)
                        {
                            if (n.Number != m.Number)
                            {
                                double d = Topology.CalculateDistance(n, m);
                                double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                                if (d <= (maximum_transmission_range * 1))
                                {
                                    TrueSystemState.Add(m.IsTransmitting);
                                }
                            }
                        }



                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                p.IsBeingTransmitting = false;
                                p.Start_Time = new int();
                                p.Transmission_Time = new double();
                                p.End_Time = new double();
                            }
                        }

                        if (n.IsReady == true && n.Buffer.Count != 0)
                        {
                            n.CurrentSystemState = TrueSystemState.ToList();
                            Boolean IsThereNewSystemState = true;
                            int CurrentSystemState_index = new int();
                            foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    IsThereNewSystemState = false;
                                    CurrentSystemState_index = n.PrimaryQLTable.IndexOf(pair);
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            if (IsThereNewSystemState)
                            {
                                PrimaryQLPair newPrimaryQLPair = new PrimaryQLPair(n, n.CurrentSystemState);
                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (a.Chosen_Link == l.Number)
                                        {
                                            if (l.Occupy == true)
                                            {
                                                a.IsVaild = true;
                                                a.Action_Possibility_value = 1 - (G1 / (G2 + n.Iterations));
                                            }
                                            else
                                            {
                                                a.IsVaild = false;
                                                a.Action_Possibility_value = 0;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }

                                double TotalActionPossibilityValue = new double();
                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    TotalActionPossibilityValue = TotalActionPossibilityValue + a.Action_Possibility_value;
                                }

                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    if (newPrimaryQLPair.PrimaryActionSpace.IndexOf(a) == 0)
                                    {
                                        a.Action_Possibility[0] = 0;
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + (a.Action_Possibility_value / TotalActionPossibilityValue);
                                    }
                                    else
                                    {
                                        a.Action_Possibility[0] = newPrimaryQLPair.PrimaryActionSpace.ElementAt(newPrimaryQLPair.PrimaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + (a.Action_Possibility_value / TotalActionPossibilityValue);
                                    }
                                }

                                double r = rand.NextDouble();
                                foreach (PrimaryAction a in newPrimaryQLPair.PrimaryActionSpace)
                                {
                                    if (r >= a.Action_Possibility[0] && r < a.Action_Possibility[1])
                                    {
                                        n.CurrentPrimaryAction = a;

                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                n.PrimaryQLTable.Add(newPrimaryQLPair);
                                n.PrimaryChosenGreedy = true;
                            }
                            else
                            {
                                double Max_Q = 0;
                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    if (Max_Q < a.PrimaryQ)
                                    {
                                        Max_Q = a.PrimaryQ;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                double greedy = (1 - (G1 / (G2 + n.Iterations)));
                                double non_greedy = (G1 / (G2 + n.Iterations));

                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (a.Chosen_Link == l.Number)
                                        {
                                            if (l.Occupy == true)
                                            {
                                                if (a.PrimaryQ == Max_Q)
                                                {
                                                    a.IsVaild = true;
                                                    a.Action_Possibility_value = greedy;
                                                }
                                                else
                                                {
                                                    a.IsVaild = true;
                                                    a.Action_Possibility_value = non_greedy;
                                                }
                                            }
                                            else
                                            {
                                                a.IsVaild = false;
                                                a.Action_Possibility_value = 0;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                    }
                                }

                                double TotalActionPossibilityValue = new double();
                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    TotalActionPossibilityValue = TotalActionPossibilityValue + a.Action_Possibility_value;
                                }

                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    if (n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.IndexOf(a) == 0)
                                    {
                                        a.Action_Possibility[0] = 0;
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + a.Action_Possibility_value / TotalActionPossibilityValue;
                                    }
                                    else
                                    {
                                        a.Action_Possibility[0] = n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.ElementAt((n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace.IndexOf(a) - 1)).Action_Possibility[1];
                                        a.Action_Possibility[1] = a.Action_Possibility[0] + a.Action_Possibility_value / TotalActionPossibilityValue;
                                    }

                                }

                                double r = rand.NextDouble();
                                foreach (PrimaryAction a in n.PrimaryQLTable.ElementAt(CurrentSystemState_index).PrimaryActionSpace)
                                {
                                    if (r >= a.Action_Possibility[0] && r < a.Action_Possibility[1])
                                    {
                                        n.CurrentPrimaryAction = a;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (n.CurrentPrimaryAction.PrimaryQ == Max_Q)
                                {
                                    n.PrimaryChosenGreedy = true;
                                }
                                else
                                {
                                    n.PrimaryChosenGreedy = false;
                                }

                            }

                            foreach (Link l in n.Links)
                            {

                                if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                {
                                    Boolean IsThereNewSystemState2 = true;
                                    int CurrentSystemState_index2 = new int();

                                    foreach (SecondaryQLPair pair in l.SecondaryQLTable)
                                    {
                                        if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                        {
                                            IsThereNewSystemState2 = false;
                                            CurrentSystemState_index2 = l.SecondaryQLTable.IndexOf(pair);
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    if (IsThereNewSystemState2)
                                    {
                                        SecondaryQLPair newSecondaryQLPair = new SecondaryQLPair(n, n.CurrentSystemState);
                                        double gready = (1 - (G1 / (G2 + n.Iterations))) / ((1 - (G1 / (G2 + n.Iterations))) * newSecondaryQLPair.SecondaryActionSpace.Count);
                                        foreach (SecondaryAction a in newSecondaryQLPair.SecondaryActionSpace)
                                        {
                                            if (newSecondaryQLPair.SecondaryActionSpace.IndexOf(a) == 0)
                                            {
                                                a.Action_Possibility[0] = 0;
                                                a.Action_Possibility[1] = a.Action_Possibility[0] + gready;
                                            }
                                            else
                                            {
                                                a.Action_Possibility[0] = newSecondaryQLPair.SecondaryActionSpace.ElementAt(newSecondaryQLPair.SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                                a.Action_Possibility[1] = a.Action_Possibility[0] + gready;
                                            }
                                        }

                                        double r2 = rand.NextDouble();
                                        foreach (SecondaryAction a in newSecondaryQLPair.SecondaryActionSpace)
                                        {
                                            if (r2 >= a.Action_Possibility[0] && r2 < a.Action_Possibility[1])
                                            {
                                                l.CurrentSecondaryAction = a;
                                                break;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        l.SecondaryQLTable.Add(newSecondaryQLPair);
                                        n.SecondaryChosenGreedy = true;
                                        break;
                                    }
                                    else
                                    {
                                        double Max_Q2 = 0;
                                        foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                        {
                                            if (Max_Q2 < a.SecondaryQ)
                                            {
                                                Max_Q2 = a.SecondaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }


                                        double greedy = (1 - (G1 / (G2 + n.Iterations))) / ((1 - (G1 / (G2 + n.Iterations))) + ((G1 / (G2 + n.Iterations)) * (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.Count - 1)));
                                        double non_greedy = (G1 / (G2 + n.Iterations)) / ((1 - (G1 / (G2 + n.Iterations))) + ((G1 / (G2 + n.Iterations)) * (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.Count - 1)));

                                        foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                        {
                                            if (l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) == 0)
                                            {
                                                if (a.SecondaryQ == Max_Q2)
                                                {
                                                    a.Action_Possibility[0] = 0;
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + greedy;
                                                }
                                                else
                                                {
                                                    a.Action_Possibility[0] = 0;
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + non_greedy;
                                                }
                                            }
                                            else
                                            {
                                                if (a.SecondaryQ == Max_Q2)
                                                {
                                                    a.Action_Possibility[0] = l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.ElementAt(l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + greedy;
                                                }
                                                else
                                                {
                                                    a.Action_Possibility[0] = l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.ElementAt(l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace.IndexOf(a) - 1).Action_Possibility[1];
                                                    a.Action_Possibility[1] = a.Action_Possibility[0] + non_greedy;
                                                }
                                            }
                                        }

                                        double r2 = rand.NextDouble();
                                        foreach (SecondaryAction a in l.SecondaryQLTable.ElementAt(CurrentSystemState_index2).SecondaryActionSpace)
                                        {
                                            if (r2 >= a.Action_Possibility[0] && r2 < a.Action_Possibility[1])
                                            {
                                                l.CurrentSecondaryAction = a;
                                                break;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }

                                        if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.SecondaryQ == Max_Q2)
                                        {
                                            n.SecondaryChosenGreedy = true;
                                        }
                                        else
                                        {
                                            n.SecondaryChosenGreedy = false;
                                        }
                                        break;

                                    }
                                }
                                else
                                {
                                    continue;
                                }

                            }

                            foreach (Package p in n.Buffer)
                            {
                                if (p.Link == n.CurrentPrimaryAction.Chosen_Link)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                        {
                                            p.Data_Rate = l.CurrentSecondaryAction.DataRate;
                                            if (p.Data_Rate > 0)
                                            {
                                                p.IsBeingTransmitting = true;
                                                p.Start_Time = k;
                                                p.Transmission_Time = p.Data_Length / p.Data_Rate;
                                                n.IsTransmitting = true;
                                                n.WaitingTimer = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.SlotLength = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.IsSuccessful = true;
                                                foreach (Node m in Topology.Nodes)
                                                {
                                                    if (m.Number == l.Receiver)
                                                    {
                                                        m.IsReceiving = true;
                                                    }
                                                }


                                            }
                                            else
                                            {
                                                p.IsBeingTransmitting = false;
                                                p.Start_Time = new int();
                                                p.Transmission_Time = new double();
                                                n.IsTransmitting = false;
                                                n.WaitingTimer = new int();
                                                n.SlotLength = new int();
                                                n.IsSuccessful = true;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                List<bool> systemp2 = new List<bool>();
                foreach (Node n in Topology.Nodes)
                {
                    systemp2.Add(n.IsTransmitting);
                }

                if (systemp1.SequenceEqual(systemp2))
                {
                    temp = true;
                    foreach (Node n in Topology.Nodes)
                    {
                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                /*
                                Console.WriteLine();
                                Console.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + p.Data_Length.ToString() + " on Link " + n.CurrentPrimaryAction.Chosen_Link.ToString() + ": (N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Receiver.ToString() + ") at Data Rate: " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + "............");
                                */
                            }
                        }
                    }
                    break;
                }
                else
                {
                    temp = false;
                }
            }
            List<Node> temp_nodes = new List<Node>();
            temp_nodes = Topology.Nodes.ToList();
            Topology.Nodes.Clear();
            for (int i = 0; i <= temp_nodes.Count; i++)
            {
                foreach (Node n in temp_nodes)
                {
                    if (n.Number == i)
                    {
                        Topology.Nodes.Add(n);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

        }

        public void ChooseActions2(Topology Topology, Random rand, int k, double G1, double G2, int ContentionTime, int ACKTime)
        {
        begin: int attempts = 0;
            Topology.Nodes.Shuffle();
            bool temp = false;
            while (temp == false)
            {
                List<bool> systemp1 = new List<bool>();
                foreach (Node n in Topology.Nodes)
                {
                    systemp1.Add(n.IsTransmitting);
                }

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == true&&n.Buffer.Count!=0)
                    {
                        List<bool> TrueSystemState = new List<bool>();

                        /*
                        foreach (Node m in Topology.Nodes)
                        {
                            if (m.Number == n.Number)
                            {
                                continue;
                            }
                            else
                            {
                                TrueSystemState.Add(m.IsTransmitting);
                            }
                        }
                        */

                        /*
                        foreach (int m in n.Neightbors)
                        {
                            TrueSystemState.Add(Topology.Nodes.ElementAt(m).IsTransmitting);
                        }
                        */

                        foreach (Node m in Topology.Nodes)
                        {
                            if (n.Number != m.Number)
                            {
                                double d = Topology.CalculateDistance(n, m);
                                double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                                if (d <= (maximum_transmission_range * 1))
                                {
                                    TrueSystemState.Add(m.IsTransmitting);
                                }
                            }
                        }

                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                p.IsBeingTransmitting = false;
                                p.Start_Time = new int();
                                p.Transmission_Time = new double();
                                p.End_Time = new double();
                            }
                        }

                        if (n.IsReady == true)
                        {
                            n.CurrentSystemState = TrueSystemState.ToList();

                            foreach(PrimaryQLPair pair in n.PrimaryQLTable)
                            {
                                if(pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    double[] MaxPrimaryQ = new double[pair.PrimaryActionSpace.Count];
                                    int i = 0;
                                    foreach(PrimaryAction a in pair.PrimaryActionSpace)
                                    {
                                        MaxPrimaryQ[i] = a.PrimaryQ;
                                        i = i + 1;
                                    }

                                    foreach(PrimaryAction a in pair.PrimaryActionSpace)
                                    {
                                        if(a.PrimaryQ==MaxPrimaryQ.Max())
                                        {
                                            n.CurrentPrimaryAction = a;
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

 
                                    foreach(SecondaryQLPair pair2 in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                    {
                                        if(pair2.SystemState.SequenceEqual(n.CurrentSystemState))
                                        {
                                            double[] MaxSecondaryQ = new double[pair2.SecondaryActionSpace.Count];
                                            int j = 0;
                                            foreach(SecondaryAction a in pair2.SecondaryActionSpace)
                                            {
                                                MaxSecondaryQ[j] = a.SecondaryQ;
                                                j = j + 1;
                                            }

                                            foreach (SecondaryAction a in pair2.SecondaryActionSpace)
                                            {
                                               if(a.SecondaryQ==MaxSecondaryQ.Max())
                                                {
                                                    n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction = a;
                                                    break;
                                                }
                                               else
                                                {
                                                    continue;
                                                }
                                            }

                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            /*
                            if (n.CurrentPrimaryAction.PrimaryQ > 0)
                            {
                                foreach (Package p in n.Buffer)
                                {
                                    if (p.Link == n.CurrentPrimaryAction.Chosen_Link)
                                    {
                                        foreach (Link l in n.Links)
                                        {
                                            if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                            {
                                                p.Data_Rate = l.CurrentSecondaryAction.DataRate;
                                                if (p.Data_Rate > 0)
                                                {
                                                    p.IsBeingTransmitting = true;
                                                    p.Start_Time = k;
                                                    p.Transmission_Time = p.Data_Length / p.Data_Rate;
                                                    n.IsTransmitting = true;
                                                    n.WaitingTimer = ContentionTime + p.Transmission_Time + ACKTime;
                                                    n.SlotLength = ContentionTime + p.Transmission_Time + ACKTime;
                                                    n.IsSuccessful = true;
                                                    foreach (Node m in Topology.Nodes)
                                                    {
                                                        if (m.Number == l.Receiver)
                                                        {
                                                            m.IsReceiving = true;
                                                        }
                                                    }


                                                }
                                                else
                                                {
                                                    p.IsBeingTransmitting = false;
                                                    p.Start_Time = new int();
                                                    p.Transmission_Time = new double();
                                                    n.IsTransmitting = false;
                                                    n.WaitingTimer = new int();
                                                    n.SlotLength = new int();
                                                    n.IsSuccessful = true;
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                n.IsTransmitting = false;
                                n.WaitingTimer = new int();
                                n.SlotLength = new int();
                                n.IsSuccessful = true;
                            }
                            */

                            foreach (Package p in n.Buffer)
                            {
                                if (p.Link == n.CurrentPrimaryAction.Chosen_Link)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                        {
                                            p.Data_Rate = l.CurrentSecondaryAction.DataRate;
                                            if (p.Data_Rate > 0)
                                            {
                                                p.IsBeingTransmitting = true;
                                                p.Start_Time = k;
                                                p.Transmission_Time = p.Data_Length / p.Data_Rate;
                                                n.IsTransmitting = true;
                                                n.WaitingTimer = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.SlotLength = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.IsSuccessful = true;
                                                foreach (Node m in Topology.Nodes)
                                                {
                                                    if (m.Number == l.Receiver)
                                                    {
                                                        m.IsReceiving = true;
                                                    }
                                                }


                                            }
                                            else
                                            {
                                                
                                                p.IsBeingTransmitting = false;
                                                p.Start_Time = new int();
                                                p.Transmission_Time = new double();
                                                n.IsTransmitting = false;
                                                n.WaitingTimer = new int();
                                                n.SlotLength = new int();
                                                n.IsSuccessful = true;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                List<bool> systemp2 = new List<bool>();
                foreach (Node n in Topology.Nodes)
                {
                    systemp2.Add(n.IsTransmitting);
                }


                if (systemp1.SequenceEqual(systemp2))
                {
                    temp = true;
                    foreach (Node n in Topology.Nodes)
                    {
                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                Console.WriteLine();
                                Console.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + p.Data_Length.ToString() + " on Link " + n.CurrentPrimaryAction.Chosen_Link.ToString() + ": (N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Receiver.ToString() + ") at Data Rate: " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + "............");
                            }
                        }
                    }
                }
                else
                {
                    attempts = attempts + 1;
                    temp = false;
                    if (attempts >= 50)
                    {
                        goto begin;
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            List<Node> temp_nodes = new List<Node>();
            temp_nodes = Topology.Nodes.ToList();
            Topology.Nodes.Clear();
            for (int i = 0; i <= temp_nodes.Count; i++)
            {
                foreach (Node n in temp_nodes)
                {
                    if (n.Number == i)
                    {
                        Topology.Nodes.Add(n);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        public void ChooseActions2_Mul(Topology Topology, Random rand, int k, double G1, double G2, int ContentionTime, int ACKTime, StreamWriter Log_QL)
        {
        begin: int attempts = 0;
            Topology.Nodes.Shuffle();
            bool temp = false;
            while (temp == false)
            {
                List<bool> systemp1 = new List<bool>();
                foreach (Node n in Topology.Nodes)
                {
                    systemp1.Add(n.IsTransmitting);
                }

                foreach (Node n in Topology.Nodes)
                {
                    if (n.IsReady == true && n.Buffer.Count != 0)
                    {
                        List<bool> TrueSystemState = new List<bool>();

                        /*
                        foreach (Node m in Topology.Nodes)
                        {
                            if (m.Number == n.Number)
                            {
                                continue;
                            }
                            else
                            {
                                TrueSystemState.Add(m.IsTransmitting);
                            }
                        }
                        */

                        /*
                        foreach (int m in n.Neightbors)
                        {
                            TrueSystemState.Add(Topology.Nodes.ElementAt(m).IsTransmitting);
                        }
                        */

                        foreach (Node m in Topology.Nodes)
                        {
                            if (n.Number != m.Number)
                            {
                                double d = Topology.CalculateDistance(n, m);
                                double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                                if (d <= (maximum_transmission_range * 1))
                                {
                                    TrueSystemState.Add(m.IsTransmitting);
                                }
                            }
                        }

                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                p.IsBeingTransmitting = false;
                                p.Start_Time = new int();
                                p.Transmission_Time = new double();
                                p.End_Time = new double();
                            }
                        }

                        if (n.IsReady == true)
                        {
                            n.CurrentSystemState = TrueSystemState.ToList();

                            foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    double[] MaxPrimaryQ = new double[pair.PrimaryActionSpace.Count];
                                    int i = 0;
                                    foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                    {
                                        MaxPrimaryQ[i] = a.PrimaryQ;
                                        i = i + 1;
                                    }

                                    foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                    {
                                        if (a.PrimaryQ == MaxPrimaryQ.Max())
                                        {
                                            n.CurrentPrimaryAction = a;
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }


                                    foreach (SecondaryQLPair pair2 in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                    {
                                        if (pair2.SystemState.SequenceEqual(n.CurrentSystemState))
                                        {
                                            double[] MaxSecondaryQ = new double[pair2.SecondaryActionSpace.Count];
                                            int j = 0;
                                            foreach (SecondaryAction a in pair2.SecondaryActionSpace)
                                            {
                                                MaxSecondaryQ[j] = a.SecondaryQ;
                                                j = j + 1;
                                            }

                                            foreach (SecondaryAction a in pair2.SecondaryActionSpace)
                                            {
                                                if (a.SecondaryQ == MaxSecondaryQ.Max())
                                                {
                                                    n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction = a;
                                                    break;
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }

                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            /*
                            if (n.CurrentPrimaryAction.PrimaryQ > 0)
                            {
                                foreach (Package p in n.Buffer)
                                {
                                    if (p.Link == n.CurrentPrimaryAction.Chosen_Link)
                                    {
                                        foreach (Link l in n.Links)
                                        {
                                            if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                            {
                                                p.Data_Rate = l.CurrentSecondaryAction.DataRate;
                                                if (p.Data_Rate > 0)
                                                {
                                                    p.IsBeingTransmitting = true;
                                                    p.Start_Time = k;
                                                    p.Transmission_Time = p.Data_Length / p.Data_Rate;
                                                    n.IsTransmitting = true;
                                                    n.WaitingTimer = ContentionTime + p.Transmission_Time + ACKTime;
                                                    n.SlotLength = ContentionTime + p.Transmission_Time + ACKTime;
                                                    n.IsSuccessful = true;
                                                    foreach (Node m in Topology.Nodes)
                                                    {
                                                        if (m.Number == l.Receiver)
                                                        {
                                                            m.IsReceiving = true;
                                                        }
                                                    }


                                                }
                                                else
                                                {
                                                    p.IsBeingTransmitting = false;
                                                    p.Start_Time = new int();
                                                    p.Transmission_Time = new double();
                                                    n.IsTransmitting = false;
                                                    n.WaitingTimer = new int();
                                                    n.SlotLength = new int();
                                                    n.IsSuccessful = true;
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                n.IsTransmitting = false;
                                n.WaitingTimer = new int();
                                n.SlotLength = new int();
                                n.IsSuccessful = true;
                            }
                            */

                            foreach (Package p in n.Buffer)
                            {
                                if (p.Link == n.CurrentPrimaryAction.Chosen_Link)
                                {
                                    foreach (Link l in n.Links)
                                    {
                                        if (l.Number == n.CurrentPrimaryAction.Chosen_Link)
                                        {
                                            p.Data_Rate = l.CurrentSecondaryAction.DataRate;
                                            if (p.Data_Rate > 0)
                                            {
                                                p.IsBeingTransmitting = true;
                                                p.Start_Time = k;
                                                p.Transmission_Time = p.Data_Length / p.Data_Rate;
                                                n.IsTransmitting = true;
                                                n.WaitingTimer = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.SlotLength = ContentionTime + p.Transmission_Time + ACKTime;
                                                n.IsSuccessful = true;
                                                foreach (Node m in Topology.Nodes)
                                                {
                                                    if (m.Number == l.Receiver)
                                                    {
                                                        m.IsReceiving = true;
                                                    }
                                                }


                                            }
                                            else
                                            {

                                                p.IsBeingTransmitting = false;
                                                p.Start_Time = new int();
                                                p.Transmission_Time = new double();
                                                n.IsTransmitting = false;
                                                n.WaitingTimer = new int();
                                                n.SlotLength = new int();
                                                n.IsSuccessful = true;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                List<bool> systemp2 = new List<bool>();
                foreach (Node n in Topology.Nodes)
                {
                    systemp2.Add(n.IsTransmitting);
                }


                if (systemp1.SequenceEqual(systemp2))
                {
                    temp = true;
                    foreach (Node n in Topology.Nodes)
                    {
                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                Log_QL.WriteLine();
                                Log_QL.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + p.Data_Length.ToString() + " on Link " + n.CurrentPrimaryAction.Chosen_Link.ToString() + ": (N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Receiver.ToString() + ") at Data Rate: " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + "............");
                            }
                        }
                    }
                }
                else
                {
                    attempts = attempts + 1;
                    temp = false;
                    if (attempts >= 50)
                    {
                        goto begin;
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            List<Node> temp_nodes = new List<Node>();
            temp_nodes = Topology.Nodes.ToList();
            Topology.Nodes.Clear();
            for (int i = 0; i <= temp_nodes.Count; i++)
            {
                foreach (Node n in temp_nodes)
                {
                    if (n.Number == i)
                    {
                        Topology.Nodes.Add(n);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        public void ChooseAction_CSMA(Topology Topology, Random rand, int k, double current_noise, int ContentionTime, int ACKTime)
        {
            foreach(Node n in Topology.Nodes)
            {
                if (n.IsReady == true && n.Buffer.Count!=0)
                {
                    foreach (Package p in n.Buffer)
                    {
                        if (p.IsBeingTransmitting == true)
                        {
                            p.IsBeingTransmitting = false;
                            p.Start_Time = new int();
                            p.Transmission_Time = new double();
                            p.End_Time = new double();
                        }
                    }

                    bool iCanTransmit = true;

                    foreach (Node m in Topology.Nodes)
                    {
                        if (n.Number != m.Number)
                        {
                            double d = Topology.CalculateDistance(n, m);
                            double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                            if (d <= (maximum_transmission_range * 1))
                            {
                                if (iCanTransmit == true)
                                {
                                    if (m.IsTransmitting == false)
                                    {
                                        iCanTransmit = true;

                                    }
                                    else
                                    {
                                        iCanTransmit = false;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    /*
                    foreach (int m in n.Neightbors)
                    {
                        if (iCanTransmit == true)
                        {
                            if (Topology.Nodes.ElementAt(m).IsTransmitting == false)
                            {
                                iCanTransmit = true;

                            }
                            else
                            {
                                iCanTransmit = false;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    */

                    if (iCanTransmit == true)
                    {
                        double CurrentTransmissionPower = Topology.CurrentTransmissionPower(n,Topology.Nodes.ElementAt(n.Links.ElementAt(n.Buffer.ElementAt(0).Link).Receiver),rand);
                        double CurrentSINR = Topology.SINR(CurrentTransmissionPower, current_noise);
                        n.Buffer.ElementAt(0).Data_Rate = Topology.DataRate(CurrentSINR);
                        n.Buffer.ElementAt(0).IsBeingTransmitting = true;
                        n.Buffer.ElementAt(0).Start_Time = k;
                        n.Buffer.ElementAt(0).Transmission_Time = n.Buffer.ElementAt(0).Data_Length / n.Buffer.ElementAt(0).Data_Rate;
                        n.IsTransmitting = true;
                        n.WaitingTimer = ContentionTime + n.Buffer.ElementAt(0).Transmission_Time + ACKTime;
                        n.SlotLength = ContentionTime + n.Buffer.ElementAt(0).Transmission_Time + ACKTime;
                        n.IsSuccessful = true;
                        n.CurrentPrimaryAction.Chosen_Link = n.Buffer.ElementAt(0).Link;
                        n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate = n.Buffer.ElementAt(0).Data_Rate;
                        n.IsReady = false;
                        Console.WriteLine();
                        Console.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + n.Buffer.ElementAt(0).Data_Length.ToString() + " on Link " + n.CurrentPrimaryAction.Chosen_Link.ToString() + ": (N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Receiver.ToString() + ") at Data Rate: " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + "............");
                    }
                    else
                    {
                        n.BackoffCounter = n.BackoffCounter + 1;
                        if(n.BackoffCounter<5)
                        {
                            n.BackoffCounter = 5;
                        }
                        if(n.BackoffCounter>10)
                        {
                            n.BackoffCounter = 10;
                        }

                        n.IsTransmitting = false;
                        n.IsSuccessful = true;
                        n.SlotLength = 0;
                        n.WaitingTimer = rand.Next(0,int.Parse(Math.Pow(2,n.BackoffCounter).ToString()));
                        n.IsReady = false;
                        Console.WriteLine();
                        Console.WriteLine("#" + k.ToString()+": Node " + n.Number.ToString() + " detected channel is busy and back off for " + n.WaitingTimer.ToString() + "ms");
                    }
                }
                else
                {
                    if (n.IsTransmitting == true)
                    {
                        foreach(Package p in n.Buffer)
                        {
                            if(p.IsBeingTransmitting==true)
                            {
                                Console.WriteLine();
                                Console.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + p.Data_Length.ToString() + " on Link " + p.Link.ToString() + ": (N" + n.Links.ElementAt(p.Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(p.Link).Receiver.ToString() + ") at Data Rate: " + p.Data_Rate.ToString() + "............");
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        bool iCanTransmit = true;

                        foreach (Node m in Topology.Nodes)
                        {
                            if (n.Number != m.Number)
                            {
                                double d = Topology.CalculateDistance(n, m);
                                double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                                if (d <= (maximum_transmission_range * 1))
                                {
                                    if (iCanTransmit == true)
                                    {
                                        if (m.IsTransmitting == false)
                                        {
                                            iCanTransmit = true;

                                        }
                                        else
                                        {
                                            iCanTransmit = false;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        if (iCanTransmit == true)
                        {
                            Console.WriteLine();
                            Console.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " back off timer counts dwon to "+n.WaitingTimer.ToString()+"ms");
                            continue;
                        }
                        else
                        {
                            n.WaitingTimer = n.WaitingTimer + 1;
                            Console.WriteLine();
                            Console.WriteLine("#" + k.ToString()+": Node " + n.Number.ToString() + " back off timer is stoped");
                        }
                    }
                }
            }
        }

        public void ChooseAction_CSMA_Mul(Topology Topology, Random rand, int k, double current_noise, int ContentionTime, int ACKTime, List<ChannelCondition> CCList, int i, StreamWriter Log_CSMA)
        {
            foreach (Node n in Topology.Nodes)
            {
                if (n.IsReady == true && n.Buffer.Count != 0)
                {
                    foreach (Package p in n.Buffer)
                    {
                        if (p.IsBeingTransmitting == true)
                        {
                            p.IsBeingTransmitting = false;
                            p.Start_Time = new int();
                            p.Transmission_Time = new double();
                            p.End_Time = new double();
                        }
                    }

                    bool iCanTransmit = true;

                    foreach (Node m in Topology.Nodes)
                    {
                        if (n.Number != m.Number)
                        {
                            double d = Topology.CalculateDistance(n, m);
                            double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                            if (d <= (maximum_transmission_range * 1))
                            {
                                if (iCanTransmit == true)
                                {
                                    if (m.IsTransmitting == false)
                                    {
                                        iCanTransmit = true;

                                    }
                                    else
                                    {
                                        iCanTransmit = false;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    /*
                    foreach (int m in n.Neightbors)
                    {
                        if (iCanTransmit == true)
                        {
                            if (Topology.Nodes.ElementAt(m).IsTransmitting == false)
                            {
                                iCanTransmit = true;

                            }
                            else
                            {
                                iCanTransmit = false;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    */

                    if (iCanTransmit == true)
                    {
                        double CurrentTransmissionPower = new double();
                        foreach (ChannelCondition cc in CCList)
                        {
                            if (cc.Time == i && cc.N1 == n.Number && cc.N2 == Topology.Nodes.ElementAt(n.Links.ElementAt(n.Buffer.ElementAt(0).Link).Receiver).Number)
                            {
                                CurrentTransmissionPower = cc.Gain * n.Transmission_Power;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }                    
                        double CurrentSINR = Topology.SINR(CurrentTransmissionPower, current_noise);

                        n.Buffer.ElementAt(0).Data_Rate = Topology.DataRate(CurrentSINR);
                        n.Buffer.ElementAt(0).IsBeingTransmitting = true;
                        n.Buffer.ElementAt(0).Start_Time = k;
                        n.Buffer.ElementAt(0).Transmission_Time = n.Buffer.ElementAt(0).Data_Length / n.Buffer.ElementAt(0).Data_Rate;
                        n.IsTransmitting = true;
                        n.WaitingTimer = ContentionTime + n.Buffer.ElementAt(0).Transmission_Time + ACKTime;
                        n.SlotLength = ContentionTime + n.Buffer.ElementAt(0).Transmission_Time + ACKTime;
                        n.IsSuccessful = true;
                        n.CurrentPrimaryAction.Chosen_Link = n.Buffer.ElementAt(0).Link;
                        n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate = n.Buffer.ElementAt(0).Data_Rate;
                        n.IsReady = false;
                        Log_CSMA.WriteLine();
                        Log_CSMA.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + n.Buffer.ElementAt(0).Data_Length.ToString() + " on Link " + n.CurrentPrimaryAction.Chosen_Link.ToString() + ": (N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).Receiver.ToString() + ") at Data Rate: " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + "............");
                    }
                    else
                    {
                        n.BackoffCounter = n.BackoffCounter + 1;
                        if (n.BackoffCounter < 5)
                        {
                            n.BackoffCounter = 5;
                        }
                        if (n.BackoffCounter > 10)
                        {
                            n.BackoffCounter = 10;
                        }

                        n.IsTransmitting = false;
                        n.IsSuccessful = true;
                        n.SlotLength = 0;
                        n.WaitingTimer = rand.Next(0, int.Parse(Math.Pow(2, n.BackoffCounter).ToString()));
                        n.IsReady = false;
                        Log_CSMA.WriteLine();
                        Log_CSMA.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " detected channel is busy and back off for " + n.WaitingTimer.ToString() + "ms");
                    }
                }
                else
                {
                    if (n.IsTransmitting == true)
                    {
                        foreach (Package p in n.Buffer)
                        {
                            if (p.IsBeingTransmitting == true)
                            {
                                Log_CSMA.WriteLine();
                                Log_CSMA.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " is transmitting Package with Data Length: " + p.Data_Length.ToString() + " on Link " + p.Link.ToString() + ": (N" + n.Links.ElementAt(p.Link).Transmitter.ToString() + ",N" + n.Links.ElementAt(p.Link).Receiver.ToString() + ") at Data Rate: " + p.Data_Rate.ToString() + "............");
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        bool iCanTransmit = true;

                        foreach (Node m in Topology.Nodes)
                        {
                            if (n.Number != m.Number)
                            {
                                double d = Topology.CalculateDistance(n, m);
                                double maximum_transmission_range = Topology.Maximum_Transmission_Range(n, m);
                                if (d <= (maximum_transmission_range * 1))
                                {
                                    if (iCanTransmit == true)
                                    {
                                        if (m.IsTransmitting == false)
                                        {
                                            iCanTransmit = true;

                                        }
                                        else
                                        {
                                            iCanTransmit = false;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        if (iCanTransmit == true)
                        {
                            Log_CSMA.WriteLine();
                            Log_CSMA.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " back off timer counts dwon to " + n.WaitingTimer.ToString() + "ms");
                            continue;
                        }
                        else
                        {
                            n.WaitingTimer = n.WaitingTimer + 1;
                            Log_CSMA.WriteLine();
                            Log_CSMA.WriteLine("#" + k.ToString() + ": Node " + n.Number.ToString() + " back off timer is stoped");
                        }
                    }
                }
            }
        }

        public void UpdateWaitTimer(Topology Topology)
        {
            bool temp = false;
            while (temp == false)
            {
                List<double> waitemp1 = new List<double>();
                foreach(Node n in Topology.Nodes)
                {
                    waitemp1.Add(n.WaitingTimer);
                }
                foreach (Node n in Topology.Nodes)
                {
                    foreach (int m in n.Neightbors)
                    {

                        if (n.WaitingTimer > Topology.Nodes.ElementAt(m).WaitingTimer)
                        {
                            Topology.Nodes.ElementAt(m).WaitingTimer = n.WaitingTimer;
                            Topology.Nodes.ElementAt(m).SlotLength = Topology.Nodes.ElementAt(m).WaitingTimer;
                        }
                        else
                        {
                            continue;
                        }

                    }

                }
                List<double> waitemp2 = new List<double>();
                foreach (Node n in Topology.Nodes)
                {
                    waitemp2.Add(n.WaitingTimer);
                }
                if(waitemp1.SequenceEqual(waitemp2))
                {
                    temp = true;
                }
                else
                {
                    temp = false;
                }
            }

            foreach (Node n in Topology.Nodes)
            {
                if (n.WaitingTimer > 0)
                {
                    n.IsReady = false;
                }
            }
        }

        public void IsTransmissionSuccessfull(Topology Topology, double current_noise, Random rand)
        {
            foreach (Node n in Topology.Nodes)
            {
                if (n.IsTransmitting == true)
                {
                    Package TransmittingPackage = new Package();
                    foreach (Package p in n.Buffer)
                    {
                        if (p.IsBeingTransmitting == true)
                        {
                            TransmittingPackage = p;
                            break;
                        }
                    }

                    Node ReceivingNode = new Node();
                    foreach (Node m in Topology.Nodes)
                    {
                        if (m.Number == n.Links.ElementAt(TransmittingPackage.Link).Receiver)
                        {
                            ReceivingNode = m;
                            break;
                        }
                    }
                    double CurrentInterference = Topology.Interference(ReceivingNode,n,rand)+current_noise;
                    double CurrentTransmissionPower = Topology.CurrentTransmissionPower(n, ReceivingNode,rand);
                    //double CurrentSINR = 10 * Math.Log10((n.Transmission_Power * n.Links.ElementAt(TransmittingPackage.Link).Channel_gain) / (CurrentInterference));
                    double CurrentSINR = Topology.SINR(CurrentTransmissionPower,CurrentInterference);
                    double CurrentMaxDataRate = Topology.DataRate(CurrentSINR);
                    //Console.WriteLine("S:" + (n.Transmission_Power * n.Links.ElementAt(TransmittingPackage.Link).Channel_gain).ToString());
                    //Console.WriteLine("I:"+CurrentInterference.ToString());
                    if (n.IsSuccessful == true)
                    {
                        if (TransmittingPackage.Data_Rate <= CurrentMaxDataRate)
                        {
                            n.IsSuccessful = true;
                        }
                        else
                        {
                            n.IsSuccessful = false;
                        }
                    }
                    else
                    {
                        continue;
                    }

                }
            }
        }

        public void IsTransmissionSuccessfull_Mul(Topology Topology, double current_noise, List<ChannelCondition> CCList, int i, StreamWriter Log_QL)
        {
            foreach (Node n in Topology.Nodes)
            {
                if (n.IsTransmitting == true)
                {
                    Package TransmittingPackage = new Package();
                    foreach (Package p in n.Buffer)
                    {
                        if (p.IsBeingTransmitting == true)
                        {
                            TransmittingPackage = p;
                            break;
                        }
                    }

                    Node ReceivingNode = new Node();
                    foreach (Node m in Topology.Nodes)
                    {
                        if (m.Number == n.Links.ElementAt(TransmittingPackage.Link).Receiver)
                        {
                            ReceivingNode = m;
                            break;
                        }
                    }

                    double interference = new double();
                    foreach(Node m in Topology.Nodes)
                    {
                        if (m.IsTransmitting == false)
                        {
                            interference = interference + 0;
                        }
                        else
                        {
                            if (ReceivingNode.Number == m.Number)
                            {
                                interference = interference + 0;
                            }
                            else if (m.Number == n.Number)
                            {
                                interference = interference + 0;
                            }
                            else 
                            {
                                foreach (ChannelCondition cc in CCList)
                                {
                                    if (cc.Time == i)
                                    {
                                        if(cc.N1==m.Number)
                                        {
                                            if(cc.N2==ReceivingNode.Number)
                                            {
                                                interference = interference + cc.Gain * m.Transmission_Power;
                                                break;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    double CurrentInterference = interference + current_noise;
                    //Console.WriteLine(CurrentInterference.ToString());

                    double CurrentTransmissionPower = new double();
                    foreach (ChannelCondition cc in CCList)
                    {
                        if (cc.Time == i && cc.N1 == n.Number && cc.N2 == ReceivingNode.Number)
                        {
                            if(cc.N1==n.Number)
                            {
                                if(cc.N2==ReceivingNode.Number)
                                {
                                    CurrentTransmissionPower = cc.Gain * n.Transmission_Power;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }

                        }
                        else
                        {
                            continue;
                        }
                    }

                    //Console.WriteLine(CurrentTransmissionPower.ToString());
                    //double CurrentSINR = 10 * Math.Log10((n.Transmission_Power * n.Links.ElementAt(TransmittingPackage.Link).Channel_gain) / (CurrentInterference));
                    double CurrentSINR = Topology.SINR(CurrentTransmissionPower, CurrentInterference);
                    //Console.WriteLine(CurrentSINR.ToString());
                    double CurrentMaxDataRate = Topology.DataRate(CurrentSINR);
                    //Console.WriteLine("S:" + (n.Transmission_Power * n.Links.ElementAt(TransmittingPackage.Link).Channel_gain).ToString());
                    //Console.WriteLine("I:"+CurrentInterference.ToString());
                    if (n.IsSuccessful == true)
                    {
                        if (TransmittingPackage.Data_Rate <= CurrentMaxDataRate)
                        {
                            n.IsSuccessful = true;
                        }
                        else
                        {
                            Log_QL.WriteLine("#" + i.ToString() + ": Node " + n.Number.ToString() + "Transmission on Link " + TransmittingPackage.Link.ToString() + " is interrupted");
                            n.IsSuccessful = false;
                        }
                    }
                    else
                    {
                        continue;
                    }

                }
            }
        }

        public void GiveReward(Topology Topology, int k, StreamWriter SAverageReward, StreamWriter PAverageReward, List<Package> TransmittedPackages)
        {

            int[] number_of_links = new int[Topology.Nodes.Count];
            int i = 0;
            foreach(Node n in Topology.Nodes)
            {
                number_of_links[i] = n.Links.Count;
                i = i + 1;
            }

            int target = new int();
            foreach(Node n in Topology.Nodes)
            {
                if(n.Links.Count==number_of_links.Max())
                {
                    target = n.Number;
                    break;
                }
                else
                {
                    continue;
                }
            }
            foreach (Node n in Topology.Nodes)
            {
                if(n.IsReady==true)
                {
                    double alpha = (Math.Log(n.Iterations+1) / n.Iterations+1);
                    double beta = 90 / (100 + n.Iterations);
                    n.IsTransmitting = false;
                    n.IsReceiving = false;
                    if (n.Buffer.Count != 0)
                    {
                        if (n.IsSuccessful == true)
                        {
                            /*
                            int cor = new int();
                            foreach(bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            */
 
                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward+(n.SlotLength*1);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward "+n.SlotLength.ToString()+" because it did not transmit");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate)*1);
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) + n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward "+n.SlotLength.ToString()+" because it did not transmit");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) + n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }



                            //Update Secondary Q
                            if (n.LastSystemState.Count != 0)
                            {


                                double Max_Q = new double();

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (Max_Q <= a.SecondaryQ)
                                            {
                                                Max_Q = a.SecondaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = ((1 - alpha) * a.SecondaryQ) + (alpha * ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate + n.SlotLength) - (n.Secondaryp_k * n.SlotLength) + 0.99 * Max_Q));

                                                Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" +n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }

                            }
                            else
                            {
                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = (1 - alpha) * a.SecondaryQ + (alpha * (10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength));

                                                

                                                Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" + n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }

                            }

                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if(MaxSecondaryQ<a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            //MaxSecondaryQ = MaxSecondaryQ * (cor+1);

                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;
                                n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));
                               
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }
                            else
                            {
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));
                                
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }

                            if (n.LastSystemState.Count != 0)
                            {
                                double Max_Q = new double();

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (Max_Q <= a.PrimaryQ)
                                            {
                                                Max_Q = a.PrimaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = ((1 - alpha) * a.PrimaryQ) + (alpha * ((MaxSecondaryQ) - (n.Primaryp_k * n.SlotLength) + 0.99 * Max_Q));

                                                
                                                Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = (1 - alpha) * a.PrimaryQ + (alpha * (MaxSecondaryQ - n.SlotLength));

                                      
                                                Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            //Remove Package from buffer

                            Package SuccessfullyTransmittedPackage = new Package();
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.End_Time = p.Start_Time + p.Transmission_Time;
                                    SuccessfullyTransmittedPackage = p;
                                    TransmittedPackages.Add(p);
                                    break;
                                }
                            }

                            if (n.Buffer.Contains(SuccessfullyTransmittedPackage))
                            {
                                n.Buffer.Remove(SuccessfullyTransmittedPackage);
                            }
                        }
                        else
                        {
                            /*
                            int cor = new int();
                            foreach (bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            */

                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward+(n.SlotLength*1);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward "+n.SlotLength.ToString()+" because it did not transmit");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward - ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate)*1);
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) - n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was unsuccessfull");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward "+n.SlotLength.ToString()+" because it did not transmit");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) - n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was unsuccessfull");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }


                            //Update Secondary Q
                            if (n.LastSystemState.Count != 0)
                            {

                                double Max_Q = new double();

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (Max_Q <= a.SecondaryQ)
                                            {
                                                Max_Q = a.SecondaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = ((1 - alpha) * a.SecondaryQ) + (alpha * ((-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength) - (n.Secondaryp_k * n.SlotLength) + 0.99 * Max_Q));

                                                

                                                Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" + n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }

                            }
                            else
                            {
                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = (1 - alpha) * a.SecondaryQ + (alpha * (-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength));

                                                

                                                Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" + n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                break;

                                            }
                                        }
                                        break;
                                    }
                                }

                            }

                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if (MaxSecondaryQ < a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            //MaxSecondaryQ = MaxSecondaryQ * (cor+1);

                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;
                                n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));
                                
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }
                            else
                            {
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }

                            if (n.LastSystemState.Count != 0)
                            {
                                double Max_Q = new double();

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (Max_Q <= a.PrimaryQ)
                                            {
                                                Max_Q = a.PrimaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = ((1 - alpha) * a.PrimaryQ) + (alpha * ((MaxSecondaryQ) - (n.Primaryp_k * n.SlotLength) + 0.99 * Max_Q));

                                                
                                                Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = (1 - alpha) * a.PrimaryQ + (alpha * (MaxSecondaryQ - n.SlotLength));

                                                
                                                Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            //reset package
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.IsBeingTransmitting = false;
                                    p.Start_Time = new int();
                                    p.Transmission_Time = new double();
                                    break;
                                }
                            }

                        }
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        public void GiveReward_Mul(Topology Topology, int k, StreamWriter SAverageReward, StreamWriter PAverageReward, List<Package> TransmittedPackages)
        {

            int[] number_of_links = new int[Topology.Nodes.Count];
            int i = 0;
            foreach (Node n in Topology.Nodes)
            {
                number_of_links[i] = n.Links.Count;
                i = i + 1;
            }

            int target = new int();
            foreach (Node n in Topology.Nodes)
            {
                if (n.Links.Count == number_of_links.Max())
                {
                    target = n.Number;
                    break;
                }
                else
                {
                    continue;
                }
            }
            foreach (Node n in Topology.Nodes)
            {
                if (n.IsReady == true)
                {
                    double alpha = (Math.Log(n.Iterations + 1) / n.Iterations + 1);
                    double beta = 90 / (100 + n.Iterations);
                    n.IsTransmitting = false;
                    n.IsReceiving = false;
                    if (n.Buffer.Count != 0)
                    {
                        if (n.IsSuccessful == true)
                        {
                            /*
                            int cor = new int();
                            foreach (bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            */

                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + (n.SlotLength * 1);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    //Console.WriteLine();
                                    //Console.WriteLine("******************************************************************************************************");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + n.SlotLength.ToString() + " because it did not transmit");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) * 1);
                                    //Console.WriteLine();
                                    //Console.WriteLine("******************************************************************************************************");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) + n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {

                                    //Console.WriteLine();
                                    //Console.WriteLine("******************************************************************************************************");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + n.SlotLength.ToString() + " because it did not transmit");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {

                                    //Console.WriteLine();
                                    //Console.WriteLine("******************************************************************************************************");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) + n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }



                            //Update Secondary Q
                            if (n.LastSystemState.Count != 0)
                            {


                                double Max_Q = new double();

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (Max_Q <= a.SecondaryQ)
                                            {
                                                Max_Q = a.SecondaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = ((1 - alpha) * a.SecondaryQ) + (alpha * ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate + n.SlotLength) - (n.Secondaryp_k * n.SlotLength) + 0.99 * Max_Q));

                                                //Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                /*
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" + n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                */
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }

                            }
                            else
                            {
                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = (1 - alpha) * a.SecondaryQ + (alpha * (10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength));


                                                /*
                                                Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" + n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                */    
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }

                            }

                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if (MaxSecondaryQ < a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            //MaxSecondaryQ = MaxSecondaryQ * (cor + 1);

                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;
                                n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }
                            else
                            {
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }

                            if (n.LastSystemState.Count != 0)
                            {
                                double Max_Q = new double();

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (Max_Q <= a.PrimaryQ)
                                            {
                                                Max_Q = a.PrimaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = ((1 - alpha) * a.PrimaryQ) + (alpha * ((MaxSecondaryQ) - (n.Primaryp_k * n.SlotLength) + 0.99 * Max_Q));


                                                //Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                //Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = (1 - alpha) * a.PrimaryQ + (alpha * (MaxSecondaryQ - n.SlotLength));


                                                //Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                //Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            //Remove Package from buffer

                            Package SuccessfullyTransmittedPackage = new Package();
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.End_Time = p.Start_Time + p.Transmission_Time;
                                    SuccessfullyTransmittedPackage = p;
                                    TransmittedPackages.Add(p);
                                    break;
                                }
                            }

                            if (n.Buffer.Contains(SuccessfullyTransmittedPackage))
                            {
                                n.Buffer.Remove(SuccessfullyTransmittedPackage);
                            }
                        }
                        else
                        {
                            /*
                            int cor = new int();
                            foreach (bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            */

                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + (n.SlotLength * 1);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    /*
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + n.SlotLength.ToString() + " because it did not transmit");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    */
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward - ((10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) * 1);
                                    /*
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) - n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was unsuccessfull");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    */
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    /*
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + n.SlotLength.ToString() + " because it did not transmit");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    */    
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                else
                                {
                                    /*
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received secondary reward " + ((-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate) - n.SlotLength).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was unsuccessfull");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total secondary reward changes to " + n.SecondaryTotalReward.ToString());
                                    */    
                                    if (n.Number == target)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }


                            //Update Secondary Q
                            if (n.LastSystemState.Count != 0)
                            {

                                double Max_Q = new double();

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (Max_Q <= a.SecondaryQ)
                                            {
                                                Max_Q = a.SecondaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = ((1 - alpha) * a.SecondaryQ) + (alpha * ((-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength) - (n.Secondaryp_k * n.SlotLength) + 0.99 * Max_Q));


                                                /*
                                                Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" + n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                */
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }

                            }
                            else
                            {
                                foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                        {
                                            if (a.Equals(n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction))
                                            {

                                                a.SecondaryQ = (1 - alpha) * a.SecondaryQ + (alpha * (-10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength));


                                                /*
                                                Console.Write("#" + k.ToString() + "  Node: " + n.Number.ToString() + " System State: " + (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable.IndexOf(pair) + 1).ToString());
                                                int temp = 0;
                                                foreach (int j in n.Neightbors)
                                                {
                                                    Console.Write(" N" + j.ToString() + ":" + n.CurrentSystemState.ElementAt(temp).ToString());
                                                    temp = temp + 1;
                                                }
                                                Console.Write("\n");
                                                Console.WriteLine("Secondary Action: Choose Data Rate " + a.DataRate.ToString() + " Q Value Update to " + a.SecondaryQ.ToString());
                                                */
                                                break;

                                            }
                                        }
                                        break;
                                    }
                                }

                            }

                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if (MaxSecondaryQ < a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            //MaxSecondaryQ = MaxSecondaryQ * (cor + 1);

                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;
                                n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }
                            else
                            {
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));


                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " received primary reward " + MaxSecondaryQ.ToString());
                                //Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " total primary reward changes to " + n.PrimaryTotalReward.ToString());
                                if (n.Number == target)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                            }

                            if (n.LastSystemState.Count != 0)
                            {
                                double Max_Q = new double();

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.LastSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (Max_Q <= a.PrimaryQ)
                                            {
                                                Max_Q = a.PrimaryQ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = ((1 - alpha) * a.PrimaryQ) + (alpha * ((MaxSecondaryQ) - (n.Primaryp_k * n.SlotLength) + 0.99 * Max_Q));


                                                //Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                //Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (PrimaryQLPair pair in n.PrimaryQLTable)
                                {
                                    if (n.CurrentSystemState.SequenceEqual(pair.SystemState))
                                    {
                                        foreach (PrimaryAction a in pair.PrimaryActionSpace)
                                        {
                                            if (a.Equals(n.CurrentPrimaryAction))
                                            {

                                                a.PrimaryQ = (1 - alpha) * a.PrimaryQ + (alpha * (MaxSecondaryQ - n.SlotLength));


                                                //Console.WriteLine("Primary Action: Choose Link " + a.Chosen_Link.ToString() + " Q Value Update to " + a.PrimaryQ.ToString());
                                                //Console.WriteLine("******************************************************************************************************");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            //reset package
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.IsBeingTransmitting = false;
                                    p.Start_Time = new int();
                                    p.Transmission_Time = new double();
                                    break;
                                }
                            }

                        }
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        public void GiveReward_Trained(Topology Topology, int k, StreamWriter SAverageReward, StreamWriter PAverageReward, List<Package> TransmittedPackages)
        {
            foreach (Node n in Topology.Nodes)
            {
                if (n.IsReady == true)
                {
                    double alpha = (Math.Log(n.Iterations + 1) / n.Iterations + 1);
                    double beta = 90 / (100 + n.Iterations);
                    n.IsTransmitting = false;
                    n.IsReceiving = false;
                    if (n.Buffer.Count != 0)
                    {
                        if (n.IsSuccessful == true)
                        {
                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + (n.SlotLength);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: "+(n.SecondaryTotalReward/n.SecondaryTotalTime).ToString() + " because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + 10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate;
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }

                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if (MaxSecondaryQ < a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            /*
                            int cor = new int();
                            foreach (bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            MaxSecondaryQ = MaxSecondaryQ * (cor+1);
                            */
                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward changes to: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());
                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Console.WriteLine("******************************************************************************************************");
                            }
                            else
                            {
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward changes to: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());
                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Console.WriteLine("******************************************************************************************************");
                            }


                            //Remove Package from buffer

                            Package SuccessfullyTransmittedPackage = new Package();
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.End_Time = p.Start_Time + p.Transmission_Time;
                                    SuccessfullyTransmittedPackage = p;
                                    TransmittedPackages.Add(p);
                                    break;
                                    
                                }
                            }

                            if (n.Buffer.Contains(SuccessfullyTransmittedPackage))
                            {
                                n.Buffer.Remove(SuccessfullyTransmittedPackage);
                            }
                        }
                        else
                        {
                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + (n.SlotLength);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString()+" because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward - 10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength;
                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString()+ " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was Unsuccessfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString()+" because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {

                                    Console.WriteLine();
                                    Console.WriteLine("******************************************************************************************************");
                                    Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString()+ " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was Unsuccessfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }


                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if (MaxSecondaryQ < a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            /*
                            int cor = new int();
                            foreach (bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            MaxSecondaryQ = MaxSecondaryQ * (cor+1);
                            */

                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;
                                
                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward changes to: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());
                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Console.WriteLine("******************************************************************************************************");
                            }
                            else
                            {

                                Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());

                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Console.WriteLine("******************************************************************************************************");
                            }


                            //reset package
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.IsBeingTransmitting = false;
                                    p.Start_Time = new int();
                                    p.Transmission_Time = new double();
                                    break;
                                }
                            }

                        }
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        public void GiveReward_Trained_Mul(Topology Topology, int k, StreamWriter SAverageReward, StreamWriter PAverageReward, List<Package> TransmittedPackages, StreamWriter Log_QL)
        {
            foreach (Node n in Topology.Nodes)
            {
                if (n.IsReady == true)
                {
                    double alpha = (Math.Log(n.Iterations + 1) / n.Iterations + 1);
                    double beta = 90 / (100 + n.Iterations);
                    n.IsTransmitting = false;
                    n.IsReceiving = false;
                    if (n.Buffer.Count != 0)
                    {
                        if (n.IsSuccessful == true)
                        {
                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + (n.SlotLength);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + 10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate;
                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {

                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {

                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was successfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }

                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if (MaxSecondaryQ < a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            /*
                            int cor = new int();
                            foreach (bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            MaxSecondaryQ = MaxSecondaryQ * (cor + 1);
                            */

                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward changes to: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());
                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Log_QL.WriteLine("******************************************************************************************************");
                            }
                            else
                            {
                                //n.Primaryp_k = ((1 - beta) * n.Primaryp_k) + (beta * (n.PrimaryTotalReward / n.PrimaryTotalTime));

                                Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward changes to: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());
                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Log_QL.WriteLine("******************************************************************************************************");
                            }


                            //Remove Package from buffer

                            Package SuccessfullyTransmittedPackage = new Package();
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.End_Time = p.Start_Time + p.Transmission_Time;
                                    SuccessfullyTransmittedPackage = p;
                                    TransmittedPackages.Add(p);
                                    break;

                                }
                            }

                            if (n.Buffer.Contains(SuccessfullyTransmittedPackage))
                            {
                                n.Buffer.Remove(SuccessfullyTransmittedPackage);
                            }
                        }
                        else
                        {
                            if (n.SecondaryChosenGreedy == true)
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {
                                    n.SecondaryTotalReward = n.SecondaryTotalReward + (n.SlotLength);
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {
                                    n.SecondaryTotalTime = n.SecondaryTotalTime + n.SlotLength;
                                    n.SecondaryTotalReward = n.SecondaryTotalReward - 10 * n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate - n.SlotLength;
                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward changes to: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was Unsuccessfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }
                            else
                            {
                                if (n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate == 0)
                                {

                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because it did not transmit");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                else
                                {

                                    Log_QL.WriteLine();
                                    Log_QL.WriteLine("******************************************************************************************************");
                                    Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Secondary Average Reward: " + (n.SecondaryTotalReward / n.SecondaryTotalTime).ToString() + " because transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " was Unsuccessfull");
                                    /*
                                    if (n.Number == 0)
                                    {
                                        SAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                    }
                                    */
                                }
                                //n.Secondaryp_k = ((1 - beta) * n.Secondaryp_k) + (beta * (n.SecondaryTotalReward / n.SecondaryTotalTime));
                            }


                            double MaxSecondaryQ = new double();
                            foreach (SecondaryQLPair pair in n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).SecondaryQLTable)
                            {
                                if (pair.SystemState.SequenceEqual(n.CurrentSystemState))
                                {
                                    foreach (SecondaryAction a in pair.SecondaryActionSpace)
                                    {
                                        if (MaxSecondaryQ < a.SecondaryQ)
                                        {
                                            MaxSecondaryQ = a.SecondaryQ;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            /*
                            int cor = new int();
                            foreach (bool m in n.CurrentSystemState)
                            {
                                if (m == true)
                                {
                                    cor = cor + 1;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            MaxSecondaryQ = MaxSecondaryQ * (cor + 1);
                            */

                            if (n.PrimaryChosenGreedy == true)
                            {
                                n.PrimaryTotalReward = n.PrimaryTotalReward + MaxSecondaryQ;
                                n.PrimaryTotalTime = n.PrimaryTotalTime + n.SlotLength;

                                Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward changes to: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());
                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Log_QL.WriteLine("******************************************************************************************************");
                            }
                            else
                            {

                                Log_QL.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + " Primary Average Reward: " + (n.PrimaryTotalReward / n.PrimaryTotalTime).ToString());

                                /*
                                if (n.Number == 0)
                                {
                                    PAverageReward.WriteLine((n.SecondaryTotalReward / n.SecondaryTotalTime).ToString());
                                }
                                */
                                Log_QL.WriteLine("******************************************************************************************************");
                            }


                            //reset package
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.IsBeingTransmitting = false;
                                    p.Start_Time = new int();
                                    p.Transmission_Time = new double();
                                    break;
                                }
                            }

                        }
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        public void GiveReward_CSMA(Topology topology_csma, List<Package> TransmittedPackages, int k, Random rand)
        {

            foreach (Node n in topology_csma.Nodes)
            {
                if (n.SlotLength <= 0 && n.IsTransmitting==true)
                {

                    if (n.Buffer.Count != 0)
                    {
                        if (n.IsSuccessful == true)
                        {
                            n.BackoffCounter = 5;
                            n.WaitingTimer = rand.Next(0, Convert.ToInt32(Math.Pow(2, n.BackoffCounter)));
                            double temp = Convert.ToInt32(Math.Pow(2, n.BackoffCounter));
                            
                            
                            //Remove Package from buffer
                            Console.WriteLine();
                            Console.WriteLine("******************************************************************************************************");
                            Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString()+"'s transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " with data rate "+n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString()+" was successfull");
                            Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + "'s back off timer set to " + n.WaitingTimer.ToString() + "ms from 0 to "+temp.ToString());
                            Console.WriteLine("******************************************************************************************************");
                            Package SuccessfullyTransmittedPackage = new Package();
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.End_Time = p.Start_Time + p.Transmission_Time;
                                    SuccessfullyTransmittedPackage = p;
                                    TransmittedPackages.Add(p);
                                    break;

                                }
                            }

                            if (n.Buffer.Contains(SuccessfullyTransmittedPackage))
                            {
                                n.Buffer.Remove(SuccessfullyTransmittedPackage);
                            }
                        }
                        else
                        {
                            n.BackoffCounter = n.BackoffCounter + 1;
                            if (n.BackoffCounter < 5)
                            {
                                n.BackoffCounter = 5;
                            }
                            if (n.BackoffCounter > 10)
                            {
                                n.BackoffCounter = 10;
                            }
                            n.WaitingTimer = rand.Next(0, Convert.ToInt32(Math.Pow(2, n.BackoffCounter)));
                            Console.WriteLine("******************************************************************************************************");
                            Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + "'s transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " with data rate " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + " was Unsuccessfull");
                            Console.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + "'s back off timer set to " + n.WaitingTimer.ToString() + "ms from 0 to "+ Convert.ToInt32(Math.Pow(2, n.BackoffCounter)).ToString());
                            Console.WriteLine("******************************************************************************************************");
                            //reset package
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.IsBeingTransmitting = false;
                                    p.Start_Time = new int();
                                    p.Transmission_Time = new double();
                                    break;
                                }
                            }
                        }
                    }
                    n.IsTransmitting = false;
                    n.IsReceiving = false;
                    if(n.WaitingTimer>0)
                    {
                        n.IsReady = false;
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        public void GiveReward_CSMA_Mul(Topology topology_csma, List<Package> TransmittedPackages, int k, Random rand, StreamWriter Log_CSMA)
        {

            foreach (Node n in topology_csma.Nodes)
            {
                if (n.SlotLength <= 0 && n.IsTransmitting == true)
                {

                    if (n.Buffer.Count != 0)
                    {
                        if (n.IsSuccessful == true)
                        {
                            n.BackoffCounter = 5;
                            n.WaitingTimer = rand.Next(0, Convert.ToInt32(Math.Pow(2, n.BackoffCounter)));
                            double temp = Convert.ToInt32(Math.Pow(2, n.BackoffCounter));


                            //Remove Package from buffer
                            Log_CSMA.WriteLine();
                            Log_CSMA.WriteLine("******************************************************************************************************");
                            Log_CSMA.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + "'s transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " with data rate " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + " was successfull");
                            Log_CSMA.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + "'s back off timer set to " + n.WaitingTimer.ToString() + "ms from 0 to " + temp.ToString());
                            Log_CSMA.WriteLine("******************************************************************************************************");
                            Package SuccessfullyTransmittedPackage = new Package();
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.End_Time = p.Start_Time + p.Transmission_Time;
                                    SuccessfullyTransmittedPackage = p;
                                    TransmittedPackages.Add(p);
                                    break;

                                }
                            }

                            if (n.Buffer.Contains(SuccessfullyTransmittedPackage))
                            {
                                n.Buffer.Remove(SuccessfullyTransmittedPackage);
                            }
                        }
                        else
                        {
                            n.BackoffCounter = n.BackoffCounter + 1;
                            if (n.BackoffCounter < 5)
                            {
                                n.BackoffCounter = 5;
                            }
                            if (n.BackoffCounter > 10)
                            {
                                n.BackoffCounter = 10;
                            }
                            n.WaitingTimer = rand.Next(0, Convert.ToInt32(Math.Pow(2, n.BackoffCounter)));
                            Log_CSMA.WriteLine("******************************************************************************************************");
                            Log_CSMA.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + "'s transmission on link: " + n.CurrentPrimaryAction.Chosen_Link.ToString() + " with data rate " + n.Links.ElementAt(n.CurrentPrimaryAction.Chosen_Link).CurrentSecondaryAction.DataRate.ToString() + " was Unsuccessfull");
                            Log_CSMA.WriteLine("#" + k.ToString() + "  Node: " + n.Number.ToString() + "'s back off timer set to " + n.WaitingTimer.ToString() + "ms from 0 to " + Convert.ToInt32(Math.Pow(2, n.BackoffCounter)).ToString());
                            Log_CSMA.WriteLine("******************************************************************************************************");
                            //reset package
                            foreach (Package p in n.Buffer)
                            {
                                if (p.IsBeingTransmitting == true)
                                {
                                    p.IsBeingTransmitting = false;
                                    p.Start_Time = new int();
                                    p.Transmission_Time = new double();
                                    break;
                                }
                            }
                        }
                    }
                    n.IsTransmitting = false;
                    n.IsReceiving = false;
                    if (n.WaitingTimer > 0)
                    {
                        n.IsReady = false;
                    }
                }
                else
                {
                    continue;
                }
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


    static class MyExtensions
    {
        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    class ChannelCondition
    {
        public int Time { get; set; }
        public int N1 { get; set; }
        public int N2 { get; set; }
        public double Gain { get; set; }
    }
    

}
