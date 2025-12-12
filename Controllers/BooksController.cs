using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAPI2.Data;
using LibraryAPI2.Models;
using LibraryAPI2.DTOs;

namespace LibraryAPI2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _context;

        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks()
        {
            var books = await _context.Books
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    IsAvailable = b.IsAvailable
                })
                .ToListAsync();

            return Ok(books);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            return Ok(new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                IsAvailable = book.IsAvailable
            });
        }

        [HttpPost]
        [Authorize(Policy = "LibrarianOrAdmin")]
        public async Task<ActionResult<BookDto>> CreateBook(CreateBookDto createBookDto)
        {
            var book = new Book
            {
                Title = createBookDto.Title,
                Author = createBookDto.Author,
                IsAvailable = true
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var bookDto = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                IsAvailable = book.IsAvailable
            };

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, bookDto);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "LibrarianOrAdmin")]
        public async Task<IActionResult> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            if (!string.IsNullOrEmpty(updateBookDto.Title))
                book.Title = updateBookDto.Title;
            if (!string.IsNullOrEmpty(updateBookDto.Author))
                book.Author = updateBookDto.Author;
            if (updateBookDto.IsAvailable.HasValue)
                book.IsAvailable = updateBookDto.IsAvailable.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/checkout")]
        public async Task<IActionResult> CheckoutBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            if (!book.IsAvailable) return BadRequest("Book already checked out");

            book.IsAvailable = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Book '{book.Title}' checked out successfully" });
        }

        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckinBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            if (book.IsAvailable) return BadRequest("Book already available");

            book.IsAvailable = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Book '{book.Title}' checked in successfully" });
        }
    }
}
