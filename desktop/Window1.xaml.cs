using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BeatlesBlog.SimConnect;
using System.Windows.Threading;
using System.Diagnostics;
using System.Xml;
using System.Collections;
using System.Globalization;

namespace SimConnectTestWPF
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public delegate void MyDelegate();

        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        VehiclePosition mypos;

        public Window1()
        {
            InitializeComponent();

            lstViewer.Items.Clear();
            lstViewer.ItemsSource = Objects;

            sc = new SimConnect();

            sc.OnRecvOpen += new SimConnect.RecvOpenEventHandler(sc_OnRecvOpen);
            sc.OnRecvException += new SimConnect.RecvExceptionEventHandler(sc_OnRecvException);
            sc.OnRecvQuit += new SimConnect.RecvQuitEventHandler(sc_OnRecvQuit);
            sc.OnRecvEvent += new SimConnect.RecvEventEventHandler(sc_onRecvEventEvent);
            sc.OnRecvEventObjectAddremove += new SimConnect.RecvEventObjectAddremoveEventHandler(sc_OnRecvEventObjectAddremove);
            sc.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(sc_OnRecvSimobjectDataBytype);
            sc.OnRecvEventFilename += new SimConnect.RecvEventFilenameEventHandler(sc_OnRecvEventFileName);
        }

        #region Private Data

        private class ObjectList : ObservableKeyedCollection<UInt32, VehiclePosition>
        {
            protected override UInt32 GetKeyForItem(VehiclePosition item)
            {
                if (item != null)
                    return item.ObjectID;
                else
                    return 0;
            }
        }
        ObjectList Objects = new ObjectList();
        HashSet<uint> PlaneList = new HashSet<uint>();

        private SimConnect sc = null;

        #endregion

        #region Menu Click Handlers

        private void SimConnectConnect(object sender, RoutedEventArgs e)
        {
            bool bConnected = false;
            string strAppName = "SimConnectTestWPF";
            if (sender == ConnectLocal)
            {
                try
                {
                    sc.Open(strAppName);
                }
                catch (SimConnect.SimConnectException)
                {
                    MessageBox.Show(this, "Local connection failed", strAppName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                bConnected = true;
            }
            if (bConnected)
            {
                ConnectLocal.Visibility = Visibility.Collapsed;
                Disconnect.Visibility = Visibility.Visible;
            }
        }

        private void SimConnectDisconnect(object sender, RoutedEventArgs e)
        {
            sc.Close();
            CleanUp();
        }

        private void Connect_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            ConnectLocal.IsEnabled = SimConnect.IsLocalRunning();
        }

        private void CleanUp()
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new MyDelegate(delegate()
            {
                Disconnect.Visibility = Visibility.Collapsed;
                ConnectLocal.Visibility = Visibility.Visible;
                Objects.Clear();
            }));
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void AddObject(UInt32 eObjectID)
        {
            VehiclePosition pos = new VehiclePosition(eObjectID);
            AddObject(pos);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            AddObject(mypos);
        }

        void AddObject(VehiclePosition pos)
        {
            sc.RequestDataOnSimObject(
                (RequestIDs)((uint)RequestIDs.VehicleDataRequestBase + (uint)pos.ObjectID),
                pos.ObjectID,
                SIMCONNECT_PERIOD.VISUAL_FRAME,
                SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                pos);
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new MyDelegate(delegate()
            {
                //Objects.Clear();
                Objects.Add(pos);
            }));
        }

        #endregion

        #region SimConnect OnRecvXxx handlers

        void sc_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
        }

        void sc_onRecvEventEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            if (data.uEventID == (uint)EventIDs.sec1)
            {
                sender.RequestDataOnSimObjectType(RequestIDs.VehicleEnumerate, 0, SIMCONNECT_SIMOBJECT_TYPE.ALL, typeof(VehiclePosition));
            }
            else if (data.uEventID == (uint)EventIDs.SimStart)
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new MyDelegate(delegate()
                {
                    Objects.Clear();
                }));
                FsxData.planeTable.Clear();
            }
        }

        static double ExtractNumbers(string expr)
        {
            CultureInfo c = new CultureInfo("en-us");
            string[] s = System.Text.RegularExpressions.Regex.Split(expr, "[^\\d+(\\.\\d{1,2})?$]");
            return double.Parse(string.Join(null, s), c);
        }

        static string convertCoords(string coord)
        {
            //"N41° 48' 46.70",E12° 16' 31.10",+000014.00"
            string[] splitted = coord.Split(',');
            string[] first = splitted[0].Split(' ');
            string[] second = splitted[1].Split(' ');
            CultureInfo c = new CultureInfo("en-us");

            string latnorth = "";

            if (first[0].StartsWith("S"))
            {
                latnorth = "-";
            }

            string longeast = "";

            if (second[0].StartsWith("W"))
            {
                longeast = "-";
            }
            
            double[] lat = new double[3];
            lat[0] = ExtractNumbers(first[0]);
            lat[1] = ExtractNumbers(first[1]);
            lat[2] = ExtractNumbers(first[2]);

            string latitude = (lat[0] + (((lat[1] * 60) + lat[2]) / 3600)).ToString(c);

            double[] lon = new double[3];
            lon[0] = ExtractNumbers(second[0]);
            lon[1] = ExtractNumbers(second[1]);
            lon[2] = ExtractNumbers(second[2]);

            string longitude = (lon[0] + (((lon[1] * 60) + lon[2]) / 3600)).ToString(c);

            return String.Format("{0}{1:0.######}, {2}{3:0.######}", latnorth, latitude, longeast, longitude);
        }

        void sc_OnRecvEventFileName(SimConnect sender, SIMCONNECT_RECV_EVENT_FILENAME data)
        {            
            XmlTextReader reader = new XmlTextReader(data.szFileName);
            string fullcoords = "";
            while (reader.Read())
            {
                if (reader.Name == "WorldPosition")
                {
                    reader.Read();
                    string coords = reader.Value.Trim();
                    if (coords.Length > 0)
                    {
                        coords = convertCoords(coords);
                        fullcoords += coords + "\n";
                    }
                }
                
            }
            FsxData.coords = fullcoords;
        }

        void sc_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            //sender.SubscribeToSystemEvent(EventIDs.ObjectAdded, "ObjectAdded");
            sender.SubscribeToSystemEvent(EventIDs.sec1, "1sec");
            sender.SubscribeToSystemEvent(EventIDs.SimStart, "SimStart");
            sender.SubscribeToSystemEvent(EventIDs.FPLACTIVATED, "FlightPlanActivated");
            sender.SubscribeToSystemEvent(EventIDs.ObjectRemoved, "ObjectRemoved");

            //sender.RequestDataOnSimObjectType(RequestIDs.VehicleEnumerate, 0, SIMCONNECT_SIMOBJECT_TYPE.USER, typeof(VehiclePosition));
        }

        void sc_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            SimConnectDisconnect(null, null);
        }

        void sc_OnRecvEventObjectAddremove(SimConnect sender, SIMCONNECT_RECV_EVENT_OBJECT_ADDREMOVE data)
        {
            switch ((EventIDs)data.uEventID)
            {
                case EventIDs.ObjectAdded:
                    if (data.dwData != 0 && !Objects.Contains(data.dwData))
                    {
                        //AddObject(data.dwData);
                    }
                    break;

                case EventIDs.ObjectRemoved:
                    if (data.dwData != 0 && Objects.Contains(data.dwData))
                    {
                        this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new MyDelegate(delegate()
                        {
                            Objects.Remove(data.dwData);
                            FsxData.planeTable.Remove(data.dwData);
                        }));
                    }
                    break;
            }
        }

        

        void sc_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            VehiclePosition pos = (VehiclePosition)data.dwData;
            pos.ObjectID = data.dwObjectID;

            if (!Objects.Contains(data.dwObjectID))
            {
                AddObject(pos);
            }
        }

        #endregion

    }

    enum RequestIDs
    {
        VehicleEnumerate,

        VehicleDataRequestBase = 0x01000000,
    }

    enum EventIDs
    {
        ObjectAdded,
        ObjectRemoved,
        sec1,
        FPLACTIVATED,
        SimStart,
    }

    enum DATA_REQUESTS
    {
        SUBSCRIBE_REQ,
        NONSUBSCRIBE_REQ,
    }

    [DataStruct()]
    public class VehiclePosition : System.ComponentModel.INotifyPropertyChanged
    {
        public VehiclePosition(UInt32 eObjectID)
        {
            this.eObjectID = eObjectID;
        }

        public VehiclePosition()
        {
        }

        #region Private Data

        private UInt32 eObjectID;

        private bool bIsUser = false;
        private string title = "";
        private double latitude1 = 10.0;
        private double latitude2 = 10.0;
        private double longitude1 = -105.37;
        private double longitude2 = -105.37;
        private double altitude1 = 250.3;
        private double altitude2 = 250.3;
        private double pitch1 = -12.3;
        private double pitch2 = -12.3;
        private double bank1 = 0.02;
        private double bank2 = 0.02;
        private double heading1 = 325.78;
        private double heading2 = 325.78;
        private string category = "category";

        #endregion

        #region Public Accessors and SimConnect Registration

        public UInt32 ObjectID
        {
            get
            {
                return eObjectID;
            }
            set
            {
                if (FsxData.planeTable.Contains(eObjectID))
                {
                    PlaneData p = (PlaneData)FsxData.planeTable[eObjectID];
                    if (FsxData.planeTable.Contains(value))
                    {
                        FsxData.planeTable.Remove(value);
                    }
                    FsxData.planeTable.Add(value, p);
                    FsxData.planeTable.Remove(eObjectID);
                }
                eObjectID = value;
            }
        }

        [DataItem("Title", 64)]
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                RaisePropertyChanged("Title");
                FsxData.getPlane(this.eObjectID).title = title;
            }
        }

        [DataItem("Is User Sim", "bool")]
        public bool IsUser
        {
            get
            {
                return bIsUser;
            }
            set
            {
                bIsUser = value;
                RaisePropertyChanged("IsUser");
                FsxData.getPlane(this.eObjectID).isUser = bIsUser;
            }
        }

        [DataItem("Plane Latitude", "degrees")]
        public double LatitudeDegrees
        {
            get
            {
                return latitude1;
            }
            set
            {
                latitude1 = value;
                RaisePropertyChanged("LatitudeDegrees");
                FsxData.getPlane(this.eObjectID).latitude = latitude1;
            }
        }

        [DataItem("Plane Latitude", "radians")]
        public double LatitudeRadians
        {
            get
            {
                return latitude2;
            }
            set
            {
                latitude2 = value;
                RaisePropertyChanged("LatitudeRadians");
            }
        }

        [DataItem("Plane Longitude", "degrees")]
        public double LongitudeDegrees
        {
            get
            {
                return longitude1;
            }
            set
            {
                longitude1 = value;
                RaisePropertyChanged("LongitudeDegrees");
                FsxData.getPlane(this.eObjectID).longitude = longitude1;
            }
        }

        [DataItem("Plane Longitude", "radians")]
        public double LongitudeRadians
        {
            get
            {
                return longitude2;
            }
            set
            {
                longitude2 = value;
                RaisePropertyChanged("LongitudeRadians");
            }
        }

        [DataItem("Plane Alt Above Ground", "feet")]
        public double AltitudeFeet
        {
            get
            {
                return altitude1;
            }
            set
            {
                altitude1 = value;
                RaisePropertyChanged("AltitudeFeet");
                FsxData.getPlane(this.eObjectID).altitude = altitude1;
            }
        }

        [DataItem("Plane Alt Above Ground", "meters")]
        public double AltitudeMeters
        {
            get
            {
                return altitude2;
            }
            set
            {
                altitude2 = value;
                RaisePropertyChanged("AltitudeMeters");
            }
        }


        [DataItem("Plane Pitch Degrees", "degrees")]
        public double PitchDegrees
        {
            get
            {
                return pitch1;
            }
            set
            {
                pitch1 = value;
                RaisePropertyChanged("PitchDegrees");
            }
        }

        [DataItem("Plane Pitch Degrees", "radians")]
        public double PitchRadians
        {
            get
            {
                return pitch2;
            }
            set
            {
                pitch2 = value;
                RaisePropertyChanged("PitchRadians");
            }
        }


        [DataItem("Plane Bank Degrees", "degrees")]
        public double BankDegrees
        {
            get
            {
                return bank1;
            }
            set
            {
                bank1 = value;
                RaisePropertyChanged("BankDegrees");
            }
        }

        [DataItem("Plane Bank Degrees", "radians")]
        public double BankRadians
        {
            get
            {
                return bank2;
            }
            set
            {
                bank2 = value;
                RaisePropertyChanged("BankRadians");
            }
        }


        [DataItem("Plane Heading Degrees True", "degrees")]
        public double HeadingDegrees
        {
            get
            {
                return heading1;
            }
            set
            {
                heading1 = value;
                RaisePropertyChanged("HeadingDegrees");
                FsxData.getPlane(this.eObjectID).heading = heading1;
            }
        }

        [DataItem("Plane Heading Degrees True", "radians")]
        public double HeadingRadians
        {
            get
            {
                return heading2;
            }
            set
            {
                heading2 = value;
                RaisePropertyChanged("HeadingRadians");
            }
        }

        [DataItem("CATEGORY", 64)]
        public string Category
        {
            get
            {
                return category;
            }
            set
            {
                category = value;
                RaisePropertyChanged("Category");
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }

    [Serializable]
    public abstract class ObservableKeyedCollection<TKey, TItem> : KeyedCollection<TKey, TItem>, INotifyCollectionChanged
    {
        #region Override Clear/Insert/Remove/Set commands to add a call to RaiseCollectionChanged to fire the CollectionChanged event
        
        protected override void ClearItems()
        {
            base.ClearItems();
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void InsertItem(int index, TItem item)
        {
            base.InsertItem(index, item);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        protected override void RemoveItem(int index)
        {
            object item = this.ElementAt(index);
            base.RemoveItem(index);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        protected override void SetItem(int index, TItem item)
        {
            base.SetItem(index, item);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, index));
        }
        
        #endregion

        #region INotifyCollectionChanged Members - implements CollectionChanged event and RaiseCollectionChanged function

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }
        }

        #endregion
    }

    [Serializable]
    public class ObservableDictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>, INotifyCollectionChanged
    {
        #region Override Add/Remove/Clear commands to add a call to RaiseCollectionChanged to fire the CollectionChanged event
        public new void Add(TKey Key, TValue Value)
        {
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            base.Add(Key, Value);
        }

        public new bool Remove(TKey Key)
        {
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return base.Remove(Key);
        }

        public new void Clear()
        {
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            base.Clear();
        }
        #endregion

        #region INotifyCollectionChanged Members - implements CollectionChanged event and RaiseCollectionChanged function

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }
        }

        #endregion
    }
}
