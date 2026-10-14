using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;

namespace Back.Services
{
    public class UsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<List<UsuarioDto>> ObtenerUsuariosAsync() =>
            (await _usuarioRepository.GetAllAsync()).Select(UsuarioDto.From).ToList();

        public async Task<UsuarioDto> ObtenerUsuarioPorIdAsync(int id) =>
            UsuarioDto.From(await ObtenerEntidadAsync(id));

        public async Task<UsuarioDto> CrearUsuarioAsync(UsuarioRequest request)
        {
            EdadMinima.Validar(request.FechaNacimiento);
            var correo = request.Email.Trim().ToLowerInvariant();
            if (await _usuarioRepository.GetByCorreoAsync(correo) is not null)
                throw BusinessException.Conflict("Ya existe una cuenta con ese correo.");

            var usuario = new Usuario
            {
                Nombre = request.Nombre.Trim(),
                FechaNacimiento = request.FechaNacimiento,
                Correo = correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Rol = request.Rol,
            };
            await _usuarioRepository.AddAsync(usuario);
            return UsuarioDto.From(usuario);
        }

        public async Task EliminarUsuarioAsync(int id, int usuarioActualId)
        {
            if (id == usuarioActualId)
                throw new BusinessException("No puedes eliminar tu propio usuario.");

            var usuario = await ObtenerEntidadAsync(id);
            if (await _usuarioRepository.TieneFacturasAsync(id))
                throw BusinessException.Conflict("El usuario tiene facturas registradas y no puede eliminarse.");

            await _usuarioRepository.DeleteAsync(usuario);
        }

        private async Task<Usuario> ObtenerEntidadAsync(int id) =>
            await _usuarioRepository.GetByIdAsync(id) ?? throw BusinessException.NotFound("Usuario no encontrado.");
    }
}
