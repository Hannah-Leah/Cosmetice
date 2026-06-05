using Cosmetice.Models;
using Cosmetice.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cosmetice.Controllers
{

   
    public class ProductsController : Controller
    {
        private readonly CosmeticeContext _context;

        public ProductsController(CosmeticeContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var cosmeticeContext = _context.Products.Include(p => p.Brand).Include(p => p.Category).Include(p => p.ProductImages);
            return View(await cosmeticeContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
    .Include(p => p.Brand)
    .Include(p => p.Category)
    .Include(p => p.ProductImages)
    .Include(p => p.Reviews)
   .ThenInclude(r => r.ReviewImages)
    .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }



            return View(product);
        }

        [Authorize(Roles = "Admin")]
        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BrandId,CategoryId,Name,Description,\r\nReleaseDate,CountryOfOrigin,Price,SkinType,\r\nIsVegan,Shade,Ingredients,Volume,FinishType")] Product product, List<IFormFile> imageFiles)
        {


            if (ModelState.IsValid)
            {

                _context.Add(product);
                await _context.SaveChangesAsync();
              
                // HANDLE IMAGE UPLOAD
                if (imageFiles != null && imageFiles.Any())
                {
                    foreach (var imageFile in imageFiles)
                    {
                        if (imageFile.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                            var filePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot/images",
                                fileName
                            );

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }

                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.ProductId,
                                ImageUrl = "/images/" + fileName
                            });
                        }
                    }

                    product.CreatedAt = DateTime.UtcNow;
                    product.AverageRating = 0;
                    product.ReviewCount = 0;

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin")]

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,BrandId,CategoryId,Name,Description,ReleaseDate,CountryOfOrigin,Price,SkinType,IsVegan,Shade,Ingredients,Volume,FinishType,AverageRating,ReviewCount,CreatedAt")] Product product, List<IFormFile> imageFiles)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                return View(product);
            }
                try
                {
                var existingProduct = await _context.Products
               .Include(p => p.ProductImages)
               .FirstOrDefaultAsync(p => p.ProductId == id);

                if (existingProduct == null)
                {
                    return NotFound();
                }


                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.BrandId = product.BrandId;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Price = product.Price;
                existingProduct.SkinType = product.SkinType;
                existingProduct.IsVegan = product.IsVegan;
                existingProduct.ReleaseDate = product.ReleaseDate;
                existingProduct.CountryOfOrigin = product.CountryOfOrigin;
                existingProduct.Shade = product.Shade;
                existingProduct.Ingredients = product.Ingredients;
                existingProduct.Volume = product.Volume;
                existingProduct.FinishType = product.FinishType;

                // Add new uploaded images
                if (imageFiles != null && imageFiles.Any())
                {
                    foreach (var imageFile in imageFiles)
                    {
                        if (imageFile.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);

                            var filePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot/images",
                                fileName
                            );

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }

                            existingProduct.ProductImages.Add(new ProductImage
                            {
                                ProductId = existingProduct.ProductId,
                                ImageUrl = "/images/" + fileName
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.ProductId))
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

        [Authorize(Roles = "Admin")]

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
      .Include(p => p.ProductImages)
      .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product != null)

            {
                // Remove images
                _context.ProductImages.RemoveRange(product.ProductImages);
                _context.Products.Remove(product);

                // remove images from folder

                foreach (var image in product.ProductImages)
                {
                    var fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        image.ImageUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }



            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
