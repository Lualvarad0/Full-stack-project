import { ListaCarga } from '@shared/components/lista-carga';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { categoria, CategoriaGasto, Proveedor, ComprobantesService } from '@core/comprobantes';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { Buscador, CategoriaSelect, ErrorCarga, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
@Component({
  selector: 'app-proveedores',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ListaCarga, Buscador, ErrorCarga, Titulo, RouterLink, DineroPipe, CategoriaSelect],
  templateUrl: './proveedores.html',
})
export class Proveedores {
  private readonly comprobantes = inject(ComprobantesService);
  private readonly avisos = inject(AvisosService);

  private readonly recargas = signal(0);
  protected readonly busqueda = signal('');
  protected readonly soloSinCategoria = signal(false);
  protected readonly guardando = signal<string | null>(null);

  protected readonly carga = cargar(
    () => this.recargas(),
    () => this.comprobantes.proveedores(),
  );

  protected readonly filtrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    return (this.carga().datos ?? []).filter(
      (p) =>
        (!this.soloSinCategoria() || !p.categoria || p.categoria === 'SinCategoria') &&
        (!texto || p.razonSocial.toLowerCase().includes(texto) || p.ruc.includes(texto)),
    );
  });

  protected asignar(p: Proveedor, nueva: CategoriaGasto): void {
    this.guardando.set(p.ruc);
    this.comprobantes.categorizarProveedor(p.ruc, nueva).subscribe({
      next: () => {
        this.guardando.set(null);
        this.avisos.exito(`${p.razonSocial}: ${categoria(nueva).nombre}.`);
        this.recargas.update((n) => n + 1);
      },
      error: (e) => {
        this.guardando.set(null);
        this.avisos.error(e);
      },
    });
  }
}
