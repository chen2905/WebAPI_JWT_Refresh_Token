using BookStoresWebAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace BookStoresWebAPI.Handlers
    {
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
        private readonly BookStoresDBContext _context;

        public BasicAuthenticationHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder,
                ISystemClock clock,
                BookStoresDBContext context)
            : base(options, logger, encoder, clock)

            {
            _context = context;
            }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
            {
            //go to base64encode.org to encode a username and password into Base64string
            //and then tested in postman
            //john.smith@gmail.com:8be7dbd7237e2e0bf90ff81b8ff44333
            //am9obi5zbWl0aEBnbWFpbC5jb206OGJlN2RiZDcyMzdlMmUwYmY5MGZmODFiOGZmNDQzMzM=


            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Authorization header was not found");
            try
                {
                var authenticationHeaderValue = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);

                var bytes = Convert.FromBase64String(authenticationHeaderValue.Parameter);
                string[] credentials = Encoding.UTF8.GetString(bytes).Split(":");
                string emailAddress = credentials[0];
                string password = credentials[1];

                User user = _context.Users.Where(u => u.EmailAddress == emailAddress && u.Password == password).FirstOrDefault();

                if (user == null)
                    {
                    return AuthenticateResult.Fail("Invaid username or password");
                    }
                else
                    {
                    var claims = new[] { new Claim(ClaimTypes.Name, user.EmailAddress) };
                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);
                    return AuthenticateResult.Success(ticket);

                    }

                }
            catch
                {
                return AuthenticateResult.Fail("Error has occured");
                }
            
             
                
                return AuthenticateResult.Fail("Need to implement how to handle AuthenticateAsync");
            }
        }
    }
 