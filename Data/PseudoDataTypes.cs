using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace URLServerManagerModern.Data.DataTypes.Pseudo
{
    public enum ModificationDetector
    {
        New,
        Modified,
        Removed,
        /**
         * <summary>
         * Used when user isn't using server data structure
         * </summary>
         **/
        Null
    }

    public enum EntityType
    {
        Server,
        VirtualServer,
        VirtualizationServer,
        Cluster
    }

    public abstract class PseudoEntity : INotifyPropertyChanged
    {
        private ModificationDetector _modDetect;
        public ModificationDetector modDetect { get { return _modDetect; } set { _modDetect = value; OnPropertyChanged("modDetect"); } }
        private Server _server;
        //Maybe not this
        public Server server { get { return _server; } set { _server = value; value.PropertyChanged += PropertyChanged; OnPropertyChanged("server"); } }

        private string _customBackgroundColor, _customBorderColor, _customTextColor;
        public string customBackgroundColor { get { return _customBackgroundColor; } set { _customBackgroundColor = value; OnPropertyChanged("customBackgroundColor"); } }
        public string customBorderColor { get { return _customBorderColor; } set { _customBorderColor = value; OnPropertyChanged("customBorderColor"); } }
        public string customTextColor { get { return _customTextColor; } set { _customTextColor = value; OnPropertyChanged("customTextColor"); } }

        private bool _usesFill = false, _usesBorder = false, _usesText = false;
        public bool usesFill { get { return _usesFill; } set { _usesFill = value; OnPropertyChanged("usesFill"); } }
        public bool usesBorder { get { return _usesBorder; } set { _usesBorder = value; OnPropertyChanged("usesBorder"); } }
        public bool usesText { get { return _usesText; } set { _usesText = value; OnPropertyChanged("usesText"); } }
        public EntityType type = EntityType.Server;

        public PseudoEntity(ModificationDetector m, Server s)
        {
            modDetect = m;
            server = s;
            customTextColor = "#000000";
            customBackgroundColor = "00FFFFFF";
            customBorderColor = "#00FFFFFF";
        }

        public abstract PseudoEntity DeepCopy();

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PseudoServer : PseudoEntity
    {
        public PseudoServer(ModificationDetector m, Server s) : base(m, s) { }


        public override PseudoEntity DeepCopy()
        {
            PseudoServer s = new PseudoServer(modDetect, server.DeepCopy());

            s.customBackgroundColor = customBackgroundColor;
            s.customBorderColor = customBorderColor;
            s.customTextColor = customTextColor;
            s.usesFill = usesFill;
            s.usesBorder = usesBorder;
            s.usesText = usesText;
            s.type = type;

            return s;
        }

        /**
         * <summary>
         * Checks if the server has the same values as the other. It skips modification detection since it would be contraproductive
         * </summary>
         **/
        public bool Equals(PseudoServer ps)
        {
            return ps != null && customBackgroundColor == ps.customBackgroundColor && usesFill == ps.usesFill && customBorderColor == ps.customBorderColor && usesBorder == ps.usesBorder && customTextColor == ps.customTextColor && usesText == ps.usesText && type == ps.type && server.Equals(ps.server);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PseudoWrappingEntity : PseudoEntity
    {
        private List<PseudoEntity> _computersInCluster;
        public List<PseudoEntity> computersInCluster { get { return _computersInCluster; } set { _computersInCluster = value; OnPropertyChanged("computersInCluster"); } }
        public PseudoWrappingEntity(ModificationDetector m, Server s) : base(m, s)
        {
            computersInCluster = new List<PseudoEntity>();
        }

        public PseudoWrappingEntity(ModificationDetector m, Server s, PseudoEntity[] computers) : this(m, s)
        {
            computersInCluster = new List<PseudoEntity>();
            if (computers != null)
                computersInCluster.AddRange(computers);
        }

        public PseudoWrappingEntity(ModificationDetector m, Server s, List<PseudoEntity> computers) : this(m, s)
        {
            computersInCluster = new List<PseudoEntity>();
            if (computers != null)
                computersInCluster.AddRange(computers);
        }

        public override PseudoEntity DeepCopy()
        {
            PseudoWrappingEntity pse = new PseudoWrappingEntity(modDetect, server.DeepCopy(), (PseudoEntity[]) null);

            pse.customBackgroundColor = customBackgroundColor;
            pse.customBorderColor = customBorderColor;
            pse.customTextColor = customTextColor;
            pse.usesFill = usesFill;
            pse.usesBorder = usesBorder;
            pse.usesText = usesText;
            pse.type = type;

            foreach (PseudoEntity e in computersInCluster)
            {
                PseudoServer s;
                if ((s = e as PseudoServer) != null)
                    pse.computersInCluster.Add(s.DeepCopy());
                else
                    pse.computersInCluster.Add((e as PseudoWrappingEntity).DeepCopy());
            }

            return pse;
        }

        /**
         * <summary>
         * Checks if the server has the same values as the other. It skips modification detection since it would be contraproductive and also skips child server modification
         * </summary>
         **/
        public bool Equals(PseudoWrappingEntity ps)
        {
            return ps != null && customBackgroundColor == ps.customBackgroundColor && usesFill == ps.usesFill && customBorderColor == ps.customBorderColor && usesBorder == ps.usesBorder && customTextColor == ps.customTextColor && usesText == ps.usesText && type == ps.type && server.Equals(ps.server) && computersInCluster.Count == ps.computersInCluster.Count;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class ProtocolPortAssociation
    {
        public string protocol { get; set; }
        public int port { get; set; }

        public ProtocolPortAssociation(string protocol, int port)
        {
            this.protocol = protocol;
            this.port = port;
        }
    }

    public class CategoryColorAssociation
    {
        public string category { get; set; }
        public string fillColor { get; set; }
        public string borderColor { get; set; }
        public string textColor { get; set; }

        public CategoryColorAssociation(string category, string fillColor, string borderColor, string textColor)
        {
            this.category = category;
            this.fillColor = fillColor;
            this.borderColor = borderColor;
            this.textColor = textColor;
        }
    }
}
