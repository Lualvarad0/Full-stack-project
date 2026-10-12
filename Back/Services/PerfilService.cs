using Back.Avisos;
using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;
using Back.Sri;
using Back.Contabilidad;
using Back.Data;
using Microsoft.EntityFrameworkCore;

namespace Back.Services
{
    public class PerfilService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IComprobanteRepository _comprobanteRepository;
        private readonly AvisoService _avisos;
        private readonly ControlPeriodoService _periodos;
        private readonly AppDbContext _db;

        public PerfilService(IUsuarioRepository usuarioRepository, IComprobanteRepository comprobanteRepository, AvisoService avisos, ControlPeriodoService periodos, AppDbContext db)
        {
            _usuarioRepository = usuarioRepository;
            _comprobanteRepository = comprobanteRepository;
            _avisos = avisos;
            _periodos = periodos; _db = db;
        }

        public async Task<PerfilDto> ObtenerAsync(int usuarioId) => PerfilDto.From(await ObtenerUsuarioAsync(usuarioId));

        public async Task<PerfilActualizadoDto> ActualizarAsync(int usuarioId, PerfilRequest request)
        {
            await using var tx = await _periodos.AbrirAsync(usuarioId, []);
            var cedula = Normalizar(request.Cedula);
            var ruc = Normalizar(request.Ruc);

            if (cedula is not null && !Identificacion.EsCedulaValida(cedula))
                throw new BusinessException("La cédula no es válida. Revisa los 10 dígitos.");
            if (ruc is not null && !Identificacion.EsRucValido(ruc))
                throw new BusinessException("El RUC no es válido. Debe tener 13 dígitos y terminar en el número de establecimiento (por ejemplo, 001).");

            if (ruc is not null && Identificacion.EsPersonaNatural(ruc))
            {
                // El RUC de persona natural es la cédula + establecimiento: se completan entre sí.
                if (cedula is null) cedula = ruc[..10];
                else if (ruc[..10] != cedula)
                    throw new BusinessException("El RUC de persona natural debe empezar con tu número de cédula.");
            }

            var usuario = await ObtenerUsuarioAsync(usuarioId);
            if (usuario.Ruc != ruc) await _periodos.ExigirAbiertosAsync(usuarioId, await _db.Comprobantes.Where(c => c.UsuarioId == usuarioId).Select(c => c.FechaEmision).ToListAsync());
            var rucAnterior = usuario.Ruc;
            var regimenAnterior = usuario.Regimen;
            var razonSocial = string.IsNullOrWhiteSpace(request.RazonSocial) ? null : request.RazonSocial.Trim().ToUpperInvariant();

            var cambios = new List<string>();
            if (usuario.Ruc != ruc) cambios.Add("RUC");
            if (usuario.Cedula != cedula) cambios.Add("cédula");
            if (usuario.RazonSocial != razonSocial) cambios.Add("razón social");
            if (usuario.Regimen != request.Regimen) cambios.Add("régimen");
            if (usuario.CargasFamiliares != request.CargasFamiliares) cambios.Add("cargas familiares");
            if (usuario.EnfermedadCatastrofica != request.EnfermedadCatastrofica) cambios.Add("enfermedad catastrófica");

            usuario.Cedula = cedula;
            usuario.Ruc = ruc;
            usuario.RazonSocial = razonSocial;
            usuario.Regimen = request.Regimen;
            usuario.CargasFamiliares = request.CargasFamiliares;
            usuario.EnfermedadCatastrofica = request.EnfermedadCatastrofica;
            await _usuarioRepository.UpdateAsync(usuario);
            if (rucAnterior != ruc || regimenAnterior != request.Regimen) {
                var obligaciones = await _db.Set<PerfilObligaciones>().SingleOrDefaultAsync(p => p.UsuarioId == usuarioId);
                if (obligaciones is not null) obligaciones.ConfirmadoEn = null;
                if (rucAnterior != ruc) await _db.Set<RevisionFiscalComprobante>().Where(r => r.UsuarioId == usuarioId).ExecuteUpdateAsync(s => s.SetProperty(r => r.Renta, TratamientoRentaDocumento.Pendiente));
                await _db.SaveChangesAsync();
            }

            // Con un RUC nuevo, lo que el usuario emitió pasa a ser ingreso (y viceversa).
            var (emitidos, recibidos) = rucAnterior == ruc
                ? (-1, -1)
                : await _comprobanteRepository.ReclasificarAsync(usuarioId, ruc);

            if (cambios.Count > 0)
                await _avisos.NotificarAsync(usuarioId, TipoAviso.PerfilActualizado, "Se actualizó tu perfil tributario",
                    $"Cambios: {string.Join(", ", cambios)}.");
            await tx.ConfirmarAsync();

            return new PerfilActualizadoDto(PerfilDto.From(usuario), emitidos, recibidos);
        }

        private async Task<Usuario> ObtenerUsuarioAsync(int usuarioId) =>
            await _usuarioRepository.GetByIdAsync(usuarioId) ?? throw BusinessException.NotFound("Usuario no encontrado.");

        private static string? Normalizar(string? valor)
        {
            var limpio = new string((valor ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
            return limpio.Length == 0 ? null : limpio;
        }
    }
}
