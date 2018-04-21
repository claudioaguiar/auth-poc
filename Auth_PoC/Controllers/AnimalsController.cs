using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth_Poc.Controllers
{
    [Route("api/[controller]"), Authorize(Policy = "Policy.Api")]
    public class AnimalsController : Controller
    {
        [HttpGet]
        public IActionResult GetAnimals()
        {
            var identity = HttpContext.User;
            string[] animals = new string[] { "poodle", "pug", "goldenretriever" };
            if (identity.HasClaim("perms", "policy.api.animals.read"))
            {
                animals = animals.Concat(new string[] { "parrot", "budgie", "seagull" }).ToArray();
            }
            return Ok(animals);
        }
    }
}
