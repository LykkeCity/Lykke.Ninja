using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("balances")]
    public class AddressTransactionsController:Controller
    {

        [HttpGet("{address}")]
        public async Task<string> Get(string address)
        {
            return "test";
        }
    }
}
