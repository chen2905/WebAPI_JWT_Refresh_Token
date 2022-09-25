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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebAPI_JWT_Refresh_Token.Models;

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



        [HttpPost("Login")]
        public async Task<ActionResult<UserWithToken>> Login([FromBody] User user)
            {
            user = await _context.Users.Include(u => u.Role)
                    .Where(u => u.EmailAddress == user.EmailAddress 
                          && u.Password == user.Password).FirstOrDefaultAsync();

            UserWithToken userWithToken = null;

            if(user != null)
                {
                RefreshToken refreshToken = GenerateRefreshToken();
                user.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                //return the fresh token to front end;

                userWithToken = new UserWithToken(user);
                userWithToken.RefreshToken = refreshToken.Token;

                }

            if (user == null)
                {
                return NotFound();
                }

         
            if (userWithToken == null)
                {
                return NotFound();
                }


            userWithToken.AccessToken = GenerateAccessToken(user.UserId);
            return userWithToken;

            }


        [HttpPost("RefreshToken")]
        public async Task<ActionResult<UserWithToken>> RefreshToken([FromBody] RefreshRequest refreshRequest)
            {
            User user = GetUserFromAccessToken(refreshRequest.AccessToken); //get user from expired access token
            //check if the refresh token of user in database and sent by client is matched and not expired
            if (user!=null && ValidateRefreshToken(user,refreshRequest.RefreshToken)) 
                {
                UserWithToken userWithToken = new UserWithToken(user);
                //generate a new accessToken
                userWithToken.AccessToken = GenerateAccessToken(user.UserId);

                return userWithToken;
                }


            return null;
            
            }

        private User GetUserFromAccessToken(string accessToken)
            {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var tokenValidateionParameters = new TokenValidationParameters
                {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero

                };
            SecurityToken securityToken;
            var principle=   tokenHandler.ValidateToken(accessToken, tokenValidateionParameters, out securityToken);
            JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

            if(jwtSecurityToken!=null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                var userId = principle.FindFirst(ClaimTypes.Name)?.Value;
               return _context.Users.Where(u => u.UserId == Convert.ToInt32(userId)).FirstOrDefault();

                }

            return null;
            }

        private bool ValidateRefreshToken(User user, string refreshToken)
            {

            RefreshToken refreshTokenUser = _context.RefreshTokens.Where(rt => rt.Token == refreshToken)
                                          .OrderByDescending(rt => rt.ExpiryDate)
                                          .FirstOrDefault();

            if(refreshTokenUser!=null&& refreshTokenUser.UserId==user.UserId 
                && refreshTokenUser.ExpiryDate>DateTime.UtcNow)
                {
                return true;
                }

            return false;
            }

     

        private RefreshToken GenerateRefreshToken()
            {
            RefreshToken refreshToken = new RefreshToken();

            var randomNumber = new byte[32];

            using(var rng = RandomNumberGenerator.Create())
                {
                rng.GetBytes(randomNumber);
                refreshToken.Token = Convert.ToBase64String(randomNumber);
              
                }
            refreshToken.ExpiryDate = DateTime.UtcNow.AddMonths(6);
            return refreshToken;
            }
    
        private string GenerateAccessToken(int userId)
            {

            var tokenHandler = new JwtSecurityTokenHandler();
            //var key = Encoding.ASCII.GetBytes("thisisasecretkeyanddontsharewithanyone");//we have to encode the key to not readable format
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey.ToString());
            var tokenDescriptor = new SecurityTokenDescriptor
                {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name,userId.ToString())
                    }),

                Expires = DateTime.UtcNow.AddSeconds(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
                };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
            }
        }

    }
