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
        string getBookingsCMD = "SELECT * FROM cust_booking LEFT JOIN hotel ON cust_booking.hotel_id = hotel.hotel_id LEFT JOIN ROOM on cust_booking.room_ID = ROOM.room_ID WHERE cust_id = {0};";
        string addBookingCMD = "INSERT INTO cust_booking(hotel_id, cust_id, date_booking_made, date_for_booking, booking_activated, hide_booking)";
        string testCMD = "SELECT * FROM cust_booking WHERE cust_id = {0};";

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