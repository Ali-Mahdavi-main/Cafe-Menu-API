using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafeMenu.Api.Services
{
    public class CurrentCafeService : ICurrentCafeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentCafeService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public int? CafeId
        {
            get
            {
                var claim = _httpContextAccessor
                    .HttpContext?
                    .User
                    .FindFirst("CafeId");
                
                if (claim == null) 
                    return null;

                return int.Parse(claim.Value);
            }
        }
    }
}