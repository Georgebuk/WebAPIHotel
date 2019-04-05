using HotelClassLibrary;
using Newtonsoft.Json;
using System.Web.Http;

namespace WebAPIHotel.Controllers
{
    public class UserController : ApiController
    {
        // GET api/<controller>
        public string Get(string email, string password)
        {
            Customer customer = new SQLHandler().getUser(email, password);
            string jsonResponse = JsonConvert.SerializeObject(customer);
            return jsonResponse;
        }
        // POST api/<controller>
        public string Post([FromBody]UserDetailsAPIModel model)
        {
            Customer customer = new SQLHandler().getUser(model.Email, model.Password);
            string jsonResponse = JsonConvert.SerializeObject(customer);
            return jsonResponse;
        }

        [Route("api/user/register")]
        [HttpPost]
        public ErrorEnum Register([FromBody]Customer c)
        {
            return new SQLHandler().executePost(RequestType.ADD_NEW_USER_POST_REQUEST, c);
        }
    }
}