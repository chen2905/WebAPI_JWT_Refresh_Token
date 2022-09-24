using BookStoresWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
namespace BookStoresWebAPI.Controllers
    {

    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class UsersController : ControllerBase
        {
        private readonly BookStoresDBContext _context;
        private readonly JWTSettings _jwtSettings;
   
        public UsersController(BookStoresDBContext context,IOptions<JWTSettings> jwtSettings)
            {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
            {
            return await _context.Users.ToListAsync();


            }


        [HttpGet("GetUser")]
        public async Task<ActionResult<User>> GetUser()
            {
            string emailAddress = HttpContext.User.Identity.Name;

            var user =  _context.Users.Where(u => u.EmailAddress == emailAddress)
                                           .FirstOrDefault();

            user.Password = null;

            return user;


            }



        [HttpGet("Login")]
        public async Task<ActionResult<UserWithToken>> Login([FromBody] User user)
            {
            user = await _context.Users.Include(u => u.Role)
                    .Where(u => u.EmailAddress == user.EmailAddress 
                          && u.Password == user.Password).FirstOrDefaultAsync();

            if (user == null)
                {
                return NotFound();
                }

            UserWithToken userWithToken = new UserWithToken(user);
            if (userWithToken == null)
                {
                return NotFound();
                }

            var tokenHandler = new JwtSecurityTokenHandler();
            //var key = Encoding.ASCII.GetBytes("thisisasecretkeyanddontsharewithanyone");//we have to encode the key to not readable format
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey.ToString());
            var tokenDescriptor = new SecurityTokenDescriptor
                {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name,user.EmailAddress)
                    }),

                Expires = DateTime.UtcNow.AddMonths(6),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
                };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            userWithToken.Token = tokenHandler.WriteToken(token);
            return userWithToken;

            }


        }
    }
