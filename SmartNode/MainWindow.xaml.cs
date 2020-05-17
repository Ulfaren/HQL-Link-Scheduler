using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.ComponentModel;

namespace SmartNode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Topology Topology = new Topology();
        QL QL = new QL();
        List<Topology> Topologies = new List<Topology>();
        
        private void Generate_Topology_Click(object sender, RoutedEventArgs e)
        {
     
            Topology = new Topology(double.Parse(Ambient_Noise.Text)/1000);
            for (int i = 1; i <= int.Parse(Number_of_Nodes.Text); i++)
            {
                Node n = new Node();
                n.Transmission_Power = double.Parse(Transmission_Power.Text)/1000;
                Topology.Nodes.Add(n);
            }

            Topology.Generate(50, 50);

            Topology_Graph.Children.Clear();

            int scale = 5;

            Thickness thickness = new Thickness() {

                Bottom=0,
                Left=0,
                Right=0,
                Top=0,
            };

            Random rand = new Random();

            foreach (Node node in Topology.Nodes)
            {

                Ellipse ellipse = new Ellipse()
                {
                    Height = 11,
                    Width = 11,
                    Stroke = Brushes.Black,
                    Fill = Brushes.Green,
                    Margin = thickness,

                };

                Topology_Graph.Children.Add(ellipse);
                Canvas.SetTop(ellipse, node.Y * scale);
                Canvas.SetLeft(ellipse, node.X * scale);
                foreach (Link l in node.Links)
                {
                    Line line = new Line()
                    {
                        X1 = Topology.Nodes.ElementAt(l.Transmitter).X * scale + 6,
                        Y1 = Topology.Nodes.ElementAt(l.Transmitter).Y * scale + 6,
                        X2 = Topology.Nodes.ElementAt(l.Receiver).X * scale + 6,
                        Y2 = Topology.Nodes.ElementAt(l.Receiver).Y * scale + 6,
                        Stroke = Brushes.Blue,
                        Fill = Brushes.Blue,
                        Margin = thickness,
                    };
                    Topology_Graph.Children.Add(line);

                }

            }

            
        }

        private void Save_Topology_Click(object sender, RoutedEventArgs e)
        {
            using (Stream Topology_file = new FileStream(AppDomain.CurrentDomain.BaseDirectory+"Topology.xml", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Topology));
                xmlSerializer.Serialize(Topology_file, Topology);
            }
            

        }

        private void Load_Topology_Click(object sender, RoutedEventArgs e)
        {
            Topology = new Topology();
            using(FileStream Topoloy_file = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "Topology.xml"))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Topology));
                Topology = (Topology)xmlSerializer.Deserialize(Topoloy_file);
            }

            

            Topology_Graph.Children.Clear();

            int scale = 5;

            Thickness thickness = new Thickness()
            {

                Bottom = 0,
                Left = 0,
                Right = 0,
                Top = 0,
            };

            foreach (Node node in Topology.Nodes)
            {

                Ellipse ellipse = new Ellipse()
                {
                    Height = 11,
                    Width = 11,
                    Stroke = Brushes.Black,
                    Fill = Brushes.Green,
                    Margin = thickness,

                };

                Topology_Graph.Children.Add(ellipse);
                Canvas.SetTop(ellipse, node.Y * scale);
                Canvas.SetLeft(ellipse, node.X * scale);
                foreach (Link l in node.Links)
                {
                    Line line = new Line()
                    {
                        X1 = Topology.Nodes.ElementAt(l.Transmitter).X * scale + 6,
                        Y1 = Topology.Nodes.ElementAt(l.Transmitter).Y * scale + 6,
                        X2 = Topology.Nodes.ElementAt(l.Receiver).X * scale + 6,
                        Y2 = Topology.Nodes.ElementAt(l.Receiver).Y * scale + 6,
                        Stroke = Brushes.Blue,
                        Fill = Brushes.Blue,
                        Margin = thickness,
                    };
                    Topology_Graph.Children.Add(line);

                }


            }
        }

        private void Train_Click(object sender, RoutedEventArgs e)
        {
            Topology.NoiseVar = double.Parse(Noise_Var.Text);
            QL.QLearning(double.Parse(Package_Arrive_Rate.Text), Topology, QL_Result, G1, G2);
        }

        private void Simulate_Click(object sender, RoutedEventArgs e)
        {
            Topology.NoiseVar = double.Parse(Noise_Var.Text);
            QL.Simulate(int.Parse(Iteration.Text), double.Parse(Package_Arrive_Rate.Text), Topology);
        }

        private void Gt_Click(object sender, RoutedEventArgs e)
        {
            Topologies.Clear();
            for(int i =1;i<=(int.Parse(NumTop.Text));i++)
            {
                Topology = new Topology(double.Parse(N_0.Text) / 1000);
                for (int j = 1; j <= int.Parse(NumNode.Text); j++)
                {
                    Node n = new Node();
                    n.Transmission_Power = double.Parse(P_t.Text) / 1000;
                    Topology.Nodes.Add(n);
                }

                Topology.Generate(50, 50);

                Topologies.Add(Topology);
            }
        }

        private void SavTop_Click(object sender, RoutedEventArgs e)
        {
            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/Topologies");
            for (int i=0;i<=Topologies.Count-1;i++)
            {
                using (Stream Topology_file = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "/Topologies/Topology" + i.ToString()+".xml", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Topology));
                    xmlSerializer.Serialize(Topology_file, Topologies.ElementAt(i));
                }
            }
        }

        private void TraTop_Click(object sender, RoutedEventArgs e)
        {
            for(int i=0;i<=Topologies.Count-1;i++)
            {
                QL.QLearningMul(1, Topologies.ElementAt(i), g1, g2,i);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            foreach (Topology t in Topologies)
            {
                t.NoiseVar = double.Parse(NV.Text);
            }

            for (int i = 0; i <= Topologies.Count - 1; i++)
            {
                QL.QLearningMul(1, Topologies.ElementAt(i), g1, g2, i);
            }

            for (int i = 0; i <= Topologies.Count - 1; i++)
            {
                QL.Simulate_Mul(int.Parse(SimDur.Text), 1, Topologies.ElementAt(i), i);
            }
        }
    }
}
