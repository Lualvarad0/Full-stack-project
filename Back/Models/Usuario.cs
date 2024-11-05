using System.ComponentModel.DataAnnotations;

namespace Back.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        [EmailAddress]
        public string Correo { get; set; }

        [Required]
        public string Password { get; set; } // Podria encriptar este campo al almacenarlo[no olvidar]
        //puedo agregar mas campos como vea necesario]
    }
}
