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
        string cmd = "SELECT * FROM cust_booking LEFT JOIN hotel ON cust_booking.hotel_id = hotel.hotel_id WHERE cust_id = {0};";

        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            cmd = String.Format(cmd, id);
            List<Object> bookings = new SQLHandler().execute(cmd, "Bookings");
            string jsonResponse = JsonConvert.SerializeObject(bookings);
            return jsonResponse;
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
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