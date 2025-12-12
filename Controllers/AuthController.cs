using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAPI2.Data;
using LibraryAPI2.DTOs;
using LibraryAPI2.Services;

namespace LibraryAPI2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly AuthService _authService;

        public AuthController(LibraryContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !AuthService.VerifyPassword(loginDto.Password, user.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            var token = _authService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Role = user.Role
            });
        }
    }
}
