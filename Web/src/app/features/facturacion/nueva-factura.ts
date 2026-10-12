import { ClienteFacturaFormulario } from './cliente-formulario';
import { LineasFacturaEditor } from './lineas-editor';
import { calcularTotales, nuevaLinea } from './calculos-factura';
import { validarFactura } from './validators/factura.validator';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { LucideArrowLeft, LucideLoader, LucideSend, LucideTriangleAlert } from '@lucide/angular';
import { FORMAS_PAGO, LineaEditable, FacturacionService } from '@core/facturacion';
import { AvisosService } from '@core/ui';
import { ComprobantesService } from '@core/comprobantes';
import { mensajeDeError } from '@core/http';
import { MensajeError, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
/** Formulario de factura: cliente, productos o servicios, forma de pago y totales en vivo. */
@Component({
  selector: 'app-nueva-factura',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ClienteFacturaFormulario,
    LineasFacturaEditor,
    MensajeError,
    RouterLink,
    Titulo,
    DineroPipe,
    LucideArrowLeft,
    LucideSend,
    LucideLoader,
    LucideTriangleAlert,
  ],
  templateUrl: './nueva-factura.html',
})
export class NuevaFactura {
  private readonly facturacion = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);
  private readonly router = inject(Router);

  protected readonly formasPago = FORMAS_PAGO;

  protected readonly servicios = toSignal(
    this.facturacion.servicios().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );
  protected readonly clientes = toSignal(
    inject(ComprobantesService)
      .clientes()
      .pipe(catchError(() => of([]))),
    { initialValue: [] },
  );
  protected readonly pendientes = toSignal(
    this.facturacion.datos().pipe(
      map((d) => d.pendientes),
      catchError(() => of<string[]>([])),
    ),
    { initialValue: [] as string[] },
  );

  protected readonly tipo = signal<string>('05');
  protected readonly identificacion = signal('');
  protected readonly razonSocial = signal('');
  protected readonly correo = signal('');
  protected readonly direccion = signal('');
  protected readonly formaPago = signal<string>('20');
  protected readonly lineas = signal<LineaEditable[]>([nuevaLinea(1)]);
  protected readonly emitiendo = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly totales = computed(() => calcularTotales(this.lineas()));

  protected emitir(): void {
    const problema = validarFactura({
      tipo: this.tipo(),
      identificacion: this.identificacion(),
      razonSocial: this.razonSocial(),
      lineas: this.lineas(),
    });
    this.error.set(problema);
    if (problema) return;

    const consumidorFinal = this.tipo() === '07';
    this.emitiendo.set(true);
    this.facturacion
      .emitir({
        tipoIdentificacion: this.tipo(),
        identificacion: consumidorFinal ? '9999999999999' : this.identificacion(),
        razonSocial: consumidorFinal ? 'CONSUMIDOR FINAL' : this.razonSocial().trim(),
        correo: consumidorFinal ? null : this.correo().trim() || null,
        direccion: consumidorFinal ? null : this.direccion().trim() || null,
        formaPago: this.formaPago(),
        detalles: this.lineas().map(({ clave, codigo, ...l }) => ({
          ...l,
          codigo: codigo.trim() || null,
          descripcion: l.descripcion.trim(),
        })),
      })
      .subscribe({
        next: (f) => {
          this.emitiendo.set(false);
          if (f.estado === 'Autorizada')
            this.avisos.exito(`Factura ${f.numero} autorizada por el SRI.`);
          void this.router.navigate(['/facturacion', f.id]);
        },
        error: (e) => {
          this.emitiendo.set(false);
          this.error.set(mensajeDeError(e));
        },
      });
  }
}
