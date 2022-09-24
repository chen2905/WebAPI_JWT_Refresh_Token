using BookStoresWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoresWebAPI.Controllers
    {
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PublishersController : ControllerBase
        {

        private readonly BookStoresDBContext _context;

        public PublishersController(BookStoresDBContext context)
            {
            _context = context;
            }
        [HttpGet]

        [HttpGet("GetPublishers")]
        public async Task<ActionResult<IEnumerable<Publisher>>> GetPublishers()
            {
            return await _context.Publishers.ToListAsync();           
                
                    
                    }

        // GET: api/Publishers/GetPublisherDetails/5
        [HttpGet("GetPublisherDetails/{id}")]

        public async Task<ActionResult<Publisher>> GetPublisherDetails(int id)
            {
            //eager loading
            //var publisher = await _context.Publishers
            //                                 .Include(pub=>pub.Books)
            //                                    .ThenInclude(book=>book.Sales)
            //                                 .Include(pub => pub.Users)
            //                                   .Where(pub => pub.PubId == id)
            //                                .FirstOrDefaultAsync();
            //explicit loading

            var publisher = await _context.Publishers.SingleAsync(p => p.PubId == id);

            _context.Entry(publisher)
                    .Collection(pub => pub.Users)
                    .Load();

            _context.Entry(publisher)
                    .Collection(pub => pub.Books)
                    .Query()
                    .Where(book=>book.Title.Contains("Surreptitious") )
                    .Load();

            if (publisher == null)
                {
                return NotFound();
                }

            return publisher;
            }

        // GET: api/Publishers/PostPublisherDetails
        [HttpGet("PostPublisherDetails")]

        public async Task<ActionResult<Publisher>> PostPublisherDetails()
            {

            var publisher = new Publisher();
            publisher.PublisherName = "C&G Publisher";
            publisher.City = "New York City";
            publisher.State = "NY";
            publisher.Country = "USA";

            var book1 = new Book();
            book1.Title = "God Is Great";
            book1.PublishedDate = Convert.ToDateTime("2022-09-20");

            var book2 = new Book();
            book2.Title = "How To Pray To God";
            book2.PublishedDate = Convert.ToDateTime("2022-09-21");

            Sale sale1 = new Sale();
            sale1.Quantity = 1;
            sale1.StoreId = "6380";
            sale1.OrderNum = "o1233";
            sale1.PayTerms = "Net 30";
            sale1.OrderDate = DateTime.Now;

            Sale sale2 = new Sale();
            sale2.Quantity = 2;
            sale2.StoreId = "6380";
            sale2.OrderNum = "o1233";
            sale2.PayTerms = "Net 60";
            sale2.OrderDate = DateTime.Now;
            book1.Sales.Add(sale1);
            book2.Sales.Add(sale2);


            publisher.Books.Add(book1);
            publisher.Books.Add(book2);



            _context.Publishers.Add(publisher);
            _context.SaveChanges();

            var publishers = await _context.Publishers
                                             .Include(pub => pub.Books)
                                                .ThenInclude(book => book.Sales)
                                             .Include(pub => pub.Users)
                                               .Where(pub => pub.PubId == publisher.PubId)
                                            .FirstOrDefaultAsync();

            if (publishers == null)
                {
                return NotFound();
                }

            return publishers;
            }


        // GET: api/Publishers/5
        [HttpGet("GetPublisher/{id}")]

        public async Task<ActionResult<Publisher>> GetPublisher(int id)
            {
            var publisher = await _context.Publishers
                                            .Where(pub => pub.PubId == id)
                                            .FirstOrDefaultAsync();

            if (publisher == null)
                {
                return NotFound();
                }

            return publisher;
            }

        [HttpPost("CreatePublisher")]
        public async Task<ActionResult<Publisher>> PostPublisher(Publisher publisher)
            {
            _context.Publishers.Add(publisher);
            await _context.SaveChangesAsync();

            return await Task.FromResult(publisher); //CreatedAtAction("GetPublisher", new { id = publisher.PubId }, publisher);
            }


        [HttpPut("UpdatePublisher/{id}")]
        public async Task<IActionResult> PutPublisher(int id, Publisher publisher)
            {
            if (id != publisher.PubId)
                {
                return BadRequest();
                }

            _context.Entry(publisher).State = EntityState.Modified;

            try
                {
                await _context.SaveChangesAsync();
                }
            catch (DbUpdateConcurrencyException)
                {
                if (!PublisherExists(id))
                    {
                    return NotFound();
                    }
                else
                    {
                    throw;
                    }
                }

            return NoContent();
            }

        // DELETE: api/Publishers/5
        [HttpDelete("DeletePublisher/{id}")]
        public async Task<ActionResult<Publisher>> DeletePublisher(int id)
            {
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
                {
                return NotFound();
                }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            return publisher;
            }
        private bool PublisherExists(int id)
            {
            return _context.Publishers.Any(e => e.PubId == id);
            }

        }



    }
