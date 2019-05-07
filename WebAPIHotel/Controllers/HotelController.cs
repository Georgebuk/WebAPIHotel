using HotelClassLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPIHotel.Controllers
{
    public class HotelController : ApiController
    {
        string allHotelsCMD = "SELECT * FROM hotel INNER JOIN ROOM ON ROOM.hotel_ID = HOTEL.hotel_id";
        string specificHotelCMD = "SELECT * FROM hotel INNER JOIN ROOM ON ROOM.hotel_ID = HOTEL.hotel_id WHERE hotel.hotel_id = {0}";

        // GET: api/Hotel
        public string Get()
        {
            //Get list of all hotels, floors and rooms from database
            List<Object> hotels = new SQLHandler().Execute(allHotelsCMD, RequestType.HOTEL_GET_REQUEST);
            //Serialize the list of hotels as a JSON string
            string jsonResponse = JsonConvert.SerializeObject(hotels);
            return jsonResponse;
        }

        // GET: api/Hotel/5
        public string Get(int id)
        {
            specificHotelCMD = String.Format(specificHotelCMD, id);
            List<Object> hotels = new SQLHandler().Execute(specificHotelCMD, RequestType.HOTEL_GET_REQUEST);
            string jsonResponse = JsonConvert.SerializeObject(hotels[0]);
            return jsonResponse;
        }

        [Route("api/Hotel/CheckAvailable/{booking}")]
        [HttpPost]
        public string CheckAvailable([FromBody]Booking dateString)
        {
            int room = new SQLHandler().GetAvailableRoom(dateString);
            if (room < 1)
                return "False";
            else
                return "True";
        }

        [Route("api/Hotel/BookDown")]
        [HttpPost]
        public string BookDown([FromBody]RoomReportWrapper rrw)
        {
            if (new SQLHandler().BookRoomDown(rrw.RoomID, rrw.Report))
                return "Success";
            else
                return "Fail";

        }

        [Route("api/Hotel/Solve/{RoomID}")]
        [HttpGet]
        public string Solve(int roomID)
        {
            new SQLHandler().SolveProblem(roomID);
            return "";

        }

        // POST: api/Hotel
        public void Post([FromBody]string value)
        {
            
        }

        // PUT: api/Hotel/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Hotel/5
        public void Delete(int id)
        {
        }
    }
}
