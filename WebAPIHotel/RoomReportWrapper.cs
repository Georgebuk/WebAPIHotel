using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPIHotel
{
    public class RoomReportWrapper
    {
        public RoomReportWrapper()
        { }
        public int RoomID { get; set; }
        public string Report { get; set; }
    }
}