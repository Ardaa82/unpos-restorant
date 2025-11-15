using System.Net;
using Unpos.ApiServer.Lib;
using Unpos.Domain.Models.Users;

namespace Unpos.ApiServer.Responses
{
    public class UnposApiLoginResponse
    {
        public UnposApiLoginResponse()
        {
            IsValid = false;
        }

        public UnposApiLoginResponse(Token token,
                                     User user,
                                     HttpStatusCode statusCode,
                                     bool isValid = false)
        {
            IsValid = isValid;
            StatusCode = statusCode;
            Token = token;
            UserId = user.Id;
            UserName = user.Name;
            UserRole = user.UserRole;
            UserString = user.UserString;
        }

        public bool IsValid { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public Token Token { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public UserRole UserRole { get; set; }
        public string UserString { get; set; }
    }
}
