using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactPro.Data;
using ContactPro.Models;
using Microsoft.AspNetCore.Authorization;
using ContactPro.Enums;
using Microsoft.AspNetCore.Identity;
using ContactPro.Services;
using ContactPro.Services.Interfaces;
using ContactPro.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ContactPro.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;
        private readonly IEmailSender _emailService;

        // Dependency Injection
        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, IAddressBookService addressBookService, IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
            _emailService = emailService;
        }

        // GET: Contacts
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            string? appUserId = _userManager.GetUserId(User);

            List<Contact>? contacts = new List<Contact>();

            if (categoryId == null)
            {
                // Query for default Contacts

                contacts = await _context.Contacts
                                         .Where(c => c.AppUserId == appUserId)
                                         .Include(c => c.Categories)
                                         .ToListAsync(); // Communicating with database via (LINQ)
            }
            else
            {
                // Query for filtered Contacts by categoryId
                Category? category = new();

                category = await _context.Categories
                                          .Where(c => c.AppUserId == appUserId)
                                         .Include(c => c.Contacts)
                                         .FirstOrDefaultAsync(c => c.Id == categoryId);

                contacts = category?.Contacts.ToList();

            }
            //List<int> categoryIds = new List<int>();
            //categoryIds.Add(categoryId.Value);

            // Get Categories
            ViewData["Categories"] = await GetCategoriesListAsync();

            return View(contacts);
        }

        public async Task<IActionResult> SearchContacts(string? searchString)
        {
            string? appUserId = _userManager.GetUserId(User);

            List<Contact>? contacts = new List<Contact>();

            AppUser? appUser = await _context.Users
                                             .Include(c=>c.Contacts)
                                                .ThenInclude(c => c.Categories)
                                             .FirstOrDefaultAsync(u => u.Id == appUserId);

            if (string.IsNullOrEmpty(searchString))
            {
                contacts = appUser?.Contacts
                                .ToList();
            }
            else
            {
                contacts = appUser?.Contacts
                                  .Where(c => c.FirstName!.ToLower().Contains(searchString.ToLower()))
                                  .ToList();
            }

            //TODO: produce the list of categories

            return View(nameof(Index), contacts);
        }

        // GET: Contacts/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {

            ViewData["CategoryList"] = await GetCategoriesListAsync();

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            

            Contact contact = new Contact();

            return View(contact);
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,DateOfBirth,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile")] Contact contact, IEnumerable<int> selected)
        {
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                contact.AppUserId = _userManager.GetUserId(User);
                contact.DateCreated = DateTime.UtcNow;

                if (contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                if (contact.DueDate != null)
                {
                    contact.DueDate = DateTime.SpecifyKind(contact.DueDate.Value, DateTimeKind.Utc);
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                // Add categories to the contact
                await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);

                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryList"] = await GetCategoriesListAsync(selected);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            // First Check
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }


            // Attempt to find the Contact by Id
            Contact? contact = await _context.Contacts
                                             .Include(c => c.Categories)
                                             .FirstOrDefaultAsync(c => c.Id == id);

            // Second Check
            if (contact == null)
            {
                return NotFound();
            }



            ViewData["CategoryList"] = await GetCategoriesListAsync(contact.Categories.Select(c => c.Id));

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateCreated,AppUserId,FirstName,LastName,DueDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageData,ImageType,ImageFile")] Contact contact, IEnumerable<int> selected)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    contact.DateCreated = DateTime.SpecifyKind(contact.DateCreated, DateTimeKind.Utc);

                    if (contact.ImageFile != null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    if (contact.DueDate != null)
                    {
                        contact.DueDate = DateTime.SpecifyKind(contact.DueDate.Value, DateTimeKind.Utc);
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();


                    // Handle Categories
                    if (selected != null)
                    {
                        // Remove the current categories
                        // Test for no categories
                        await _addressBookService.RemoveCategoriesFromContactAsync(contact.Id);

                        // Add the updated categories
                        await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryList"] = await GetCategoriesListAsync(selected);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<MultiSelectList> GetCategoriesListAsync(IEnumerable<int> categoryIds = null!)
        {
            string? appUserId = _userManager.GetUserId(User);

            IEnumerable<Category> categories = await _context.Categories
                                                             .Where(c => c.AppUserId == appUserId)
                                                             .ToListAsync();

            return new MultiSelectList(categories, "Id", "Name", categoryIds);
        }
    }
}
