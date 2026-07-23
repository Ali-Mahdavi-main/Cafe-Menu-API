using CafeMenu.Api.Models;

namespace CafeMenu.Api.Services;

public interface ITokenService
{
    string GenerateToken(Cafe cafe);
    string GenerateAdminToken(string username, string role);
}