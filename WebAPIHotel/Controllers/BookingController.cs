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
    public class BookingController : ApiController
    {
        string getBookingsCMD = "SELECT * FROM cust_booking LEFT JOIN ROOM on cust_booking.room_ID = ROOM.room_ID INNER JOIN hotel ON hotel.hotel_ID = ROOM.hotel_ID WHERE cust_id = {0};";

        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            getBookingsCMD = String.Format(getBookingsCMD, id);
            List<Object> bookings = new SQLHandler().execute(getBookingsCMD, RequestType.BOOKING_REQUEST);
            string jsonResponse = JsonConvert.SerializeObject(bookings);
            return jsonResponse;
        }

        // POST api/<controller>
        public void Post([FromBody]Booking value)
        {
            new SQLHandler().executePost(RequestType.BOOKING_POST_REQUEST, value);
        }

        [Route("api/Booking/CheckQR/{QRString}")]
        [HttpGet]
        public string CheckQR(string QRString)
        {
            string qrexists = new SQLHandler().checkQRString(QRString);
            return qrexists;
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}