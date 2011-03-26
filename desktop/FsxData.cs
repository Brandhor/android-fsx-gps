using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SimConnectTestWPF
{
    class PlaneData
    {
        public String title { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double altitude { get; set; }
        public double heading { get; set; }
        public bool isUser { get; set; }
    }
    class FsxData
    {
        public static Hashtable planeTable { get; set; }
        public static String port { get; set; }
        public static String coords { get; set; }

        public static PlaneData getPlane(UInt32 objectId)
        {
            if (FsxData.planeTable.Contains(objectId))
            {
                return (PlaneData)FsxData.planeTable[objectId];
            }
            else
            {
                PlaneData p = new PlaneData();
                FsxData.planeTable.Add(objectId, p);
                return p;
            }
        }
    }
}
