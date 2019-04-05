using System.Web.Http;

namespace WebAPIHotel.Controllers
{
    public class SetupController : ApiController
    {
        [HttpGet]
        [Route("api/Setup/setupDB")]
        public bool Setup()
        {
            return new SQLHandler().databaseSetup();
        }

        [HttpGet]
        [Route("api/Setup/connected")]
        public bool connected()
        {
            return true;
        }
    }
}