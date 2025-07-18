using CairaEdu.Data.Context;
using CairaEdu.Data.Entities;
using CairaEdu.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CairaEdu.Pages.Admin
{
    [Authorize(Roles = "Administrador")]
    public class CrearMateriaAdmModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CrearMateriaAdmModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> Profesores { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string Nombre { get; set; }

            public string? Competencias { get; set; }

            public string? Objetivos { get; set; }

            public string Estado { get; set; }

            public string? ProfesorId { get; set; }

            [Display(Name = "Imagen Materia")]
            public IFormFile? Imagen { get; set; }
            public string? LogoPath { get; set; }
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = await _userManager.FindByIdAsync(userId);
            var institucionId = usuario?.InstitucionId;

            Profesores = (await _userManager.GetUsersInRoleAsync("Docente"))
                .Where(p => p.InstitucionId == institucionId)
                .Select(p => new SelectListItem
                {
                    Value = p.Id,
                    Text = $"{p.Nombres} {p.Apellidos}"
                }).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Recargar combos si hay error
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = await _userManager.FindByIdAsync(userId);
            var institucionId = usuario?.InstitucionId;

            if (institucionId == null)
            {
                TempData["ErrorMessage"] = "Si no perteneces a una instituci�n, no puedes crear materias.";
                await OnGetAsync();
                return RedirectToPage("/Admin/VerInstitucionAdm");
                
            }

            string? rutaImagen = null;

            if (Input.Imagen != null && Input.Imagen.Length > 0)
            {
                if (Input.Imagen.Length > 1 * 1024 * 1024)
                {
                    ModelState.AddModelError("Input.Imagen", "El archivo no puede pesar m�s de 1MB.");
                    TempData["ErrorMessage"] = "El archivo no puede pesar m�s de 1MB.";
                    return Page();
                }

                var permittedTypes = new[] { "image/png", "image/jpeg", "image/jpg" };
                if (!permittedTypes.Contains(Input.Imagen.ContentType))
                {
                    ModelState.AddModelError("Input.Imagen", "Solo se permiten im�genes PNG, jpg o JPEG.");
                    TempData["ErrorMessage"] = "Solo se permiten im�genes PNG, jpg o JPEG.";
                    return Page();
                }

                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var extension = Path.GetExtension(Input.Imagen.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Input.Imagen", "La extensi�n del archivo no es v�lida.");
                    TempData["ErrorMessage"] = "La extensi�n del archivo no es v�lida.";
                    return Page();
                }
                // Si la validaci�n es correcta, procesar la imagen
                if (ModelState.IsValid)
                {
                    var carpetaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "materias");
                    Directory.CreateDirectory(carpetaDestino);

                    var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(Input.Imagen.FileName);
                    var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await Input.Imagen.CopyToAsync(stream);
                    }

                    rutaImagen = $"/img/materias/{nombreArchivo}";
                    Input.LogoPath = rutaImagen;
                }

            }


            var materia = new Materia
            {
                Nombre = Input.Nombre,
                Competencias = Input.Competencias,
                Objetivos = Input.Objetivos,
                Estado = Input.Estado,
                Imagen = rutaImagen,
                InstitucionId = institucionId.Value
            };

            _context.Materias.Add(materia);

            if (!string.IsNullOrWhiteSpace(Input.ProfesorId)){ 
                var relacion = new MateriaProfesor
                {
                    Materia = materia,
                    UserId = Input.ProfesorId
                };
                _context.Set<MateriaProfesor>().Add(relacion);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Materia creada exitosamente.";
            return RedirectToPage("VerMateriaAdm");
        }
    }
}
