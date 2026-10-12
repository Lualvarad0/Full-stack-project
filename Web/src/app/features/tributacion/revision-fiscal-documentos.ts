import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  DocumentoFiscal,
  TributacionService,
} from '@core/contabilidad/services/tributacion.service';
import { DineroPipe } from '@shared/pipes';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-revision-fiscal-documentos',
  imports: [FormsModule, RouterLink, DineroPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './revision-fiscal-documentos.html',
})
export class RevisionFiscalDocumentos extends PaginaContable {
  readonly documentos = input.required<DocumentoFiscal[]>();
  readonly cambiado = output<void>();
  private readonly api = inject(TributacionService);
  protected readonly editando = signal<DocumentoFiscal | null>(null);
  protected pagina = 0;
  protected editar(d: DocumentoFiscal): void {
    this.editando.set({ ...d });
  }
  protected override limpiar(): void {
    this.editando?.set(null);
    this.pagina = 0;
  }
  protected override refrescar(): void {
    this.editando.set(null);
    this.cambiado.emit();
  }
  protected guardar(): void {
    const d = this.editando();
    if (d) this.ejecutar(this.api.revisarDocumento(d), 'Tratamiento fiscal confirmado.');
  }
}
