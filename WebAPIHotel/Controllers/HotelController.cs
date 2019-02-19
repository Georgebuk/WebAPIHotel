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
        string allHotelsCMD = "SELECT * FROM hotel INNER JOIN HOTEL_FLOOR ON hotel.hotel_id = HOTEL_FLOOR.hotel_id INNER JOIN ROOM ON ROOM.hotel_floor_ID = HOTEL_FLOOR.hotel_floor_id";
        string specificHotelCMD = "SELECT * FROM hotel INNER JOIN HOTEL_FLOOR ON hotel.hotel_id = HOTEL_FLOOR.hotel_id INNER JOIN ROOM ON ROOM.hotel_floor_ID = HOTEL_FLOOR.hotel_floor_id WHERE hotel.hotel_id = {0}";

        // GET: api/Hotel
        public string Get()
        {
            //Get list of all hotels, floors and rooms from database
            List<Object> hotels = new SQLHandler().execute(allHotelsCMD, RequestType.HOTEL_GET_REQUEST);
            //Serialize the list of hotels as a JSON string
            string jsonResponse = JsonConvert.SerializeObject(hotels);
            return jsonResponse;
        }

        // GET: api/Hotel/5
        public string Get(int id)
        {
            specificHotelCMD = String.Format(specificHotelCMD, id);
            List<Object> hotels = new SQLHandler().execute(specificHotelCMD, RequestType.HOTEL_GET_REQUEST);
            string jsonResponse = JsonConvert.SerializeObject(hotels);
            return jsonResponse;
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
