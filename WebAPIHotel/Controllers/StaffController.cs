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
    public class StaffController : ApiController
    {


        // POST api/<controller>
        public string Post([FromBody]UserDetailsAPIModel model)
        {
            Staff staff = new SQLHandler().GetStaffMember(model.Email, model.Password);
            string json = JsonConvert.SerializeObject(staff);
            return json;
        }
    }
}
