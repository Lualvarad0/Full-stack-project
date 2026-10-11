import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Observable } from 'rxjs';
import { LucideBan, LucideCircleCheck, LucideCircleX, LucideHistory, LucideTrash2, LucideUserCheck } from '@lucide/angular';
import { ESTADOS_SUSCRIPCION, PLANES } from '@core/constants';
import { AvisoCuenta, PestanaAdmin, PlanSuscripcion, UsuarioAdmin } from '@core/models';
import { AdminService, AuthService, AvisosService } from '@core/services';
import { capitalizar, cargar, fechaHora, nombreMes } from '@core/utils';
import { Buscador, ErrorCarga, Segmentado, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';

/** Panel de superadministrador: el negocio de un vistazo, usuarios, pagos y estado del servidor. */
@Component({
  selector: 'app-admin',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, Titulo, Segmentado, Buscador, ErrorCarga, DineroPipe, LucideBan, LucideUserCheck, LucideTrash2, LucideHistory, LucideCircleCheck, LucideCircleX],
  template: `
    <app-titulo linea1="Panel de" linea2="administración" subtitulo="Solo tú lo ves: métricas de Nudo, cuentas, pagos y estado del servidor." />
    <app-segmentado class="mt-6 block" [opciones]="pestanas" [(valor)]="pestana" etiqueta="Sección del panel" />

    @switch (pestana()) {
      @case ('resumen') {
        @if (metricas().datos; as m) {
          <section class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4" aria-label="Indicadores">
            @for (k of [
              { t: 'Usuarios', v: m.usuariosTotales, d: m.nuevosUltimos30Dias + ' nuevos en 30 días' },
              { t: 'Activos en 30 días', v: m.activosUltimos30Dias, d: m.usuariosSuspendidos + ' suspendidos' },
              { t: 'Suscripciones pagando', v: m.suscripcionesActivas, d: m.suscripcionesEnPrueba + ' en prueba · ' + m.suscripcionesConPagoPendiente + ' con pago pendiente' },
              { t: 'Facturas emitidas', v: m.facturasEmitidas, d: m.facturasAutorizadas + ' autorizadas' }
            ]; track k.t) {
              <div class="card p-5">
                <p class="etiqueta">{{ k.t }}</p>
                <p class="cifra mt-2 text-[28px] leading-none">{{ k.v }}</p>
                <p class="mt-2 text-xs text-muted-fg">{{ k.d }}</p>
              </div>
            }
            <div class="card p-5">
              <p class="etiqueta">Ingreso mensual recurrente</p>
              <p class="cifra mt-2 text-[28px] leading-none">{{ m.ingresoMensualRecurrente | dinero }}</p>
              <p class="mt-2 text-xs text-muted-fg">Sin IVA · planes anuales ÷ 12</p>
            </div>
            <div class="card p-5">
              <p class="etiqueta">Cobrado en 30 días</p>
              <p class="cifra mt-2 text-[28px] leading-none">{{ m.cobradoUltimos30Dias | dinero }}</p>
              <p class="mt-2 text-xs text-muted-fg">Pagos aprobados por Kushki</p>
            </div>
            <div class="card p-5 sm:col-span-2">
              <p class="etiqueta">Usuarios por plan</p>
              <div class="mt-3 space-y-2">
                @for (p of planes; track p.id) {
                  <div>
                    <div class="flex justify-between text-sm"><span>{{ p.nombre }}</span><span class="font-medium">{{ m.porPlan[p.id] }}</span></div>
                    <div class="mt-1 h-1.5 overflow-hidden rounded-full bg-muted">
                      <div class="h-full rounded-full bg-brand" [style.width.%]="m.usuariosTotales ? (m.porPlan[p.id] / m.usuariosTotales) * 100 : 0"></div>
                    </div>
                  </div>
                }
              </div>
            </div>
          </section>

          <section class="card mt-4 p-5 sm:p-6" aria-labelledby="titulo-registros">
            <h2 id="titulo-registros" class="font-semibold">Registros por mes</h2>
            <div class="mt-5 flex h-40 items-end gap-3">
              @for (r of m.registrosPorMes; track r.mes) {
                <div class="flex h-full flex-1 flex-col items-center justify-end gap-1">
                  <span class="text-xs font-medium">{{ r.usuarios }}</span>
                  <div class="w-full max-w-12 rounded-t-md bg-brand" [style.height.%]="maxRegistros() ? (r.usuarios / maxRegistros()) * 85 : 0" [style.min-height.px]="2"></div>
                  <span class="text-xs text-muted-fg">{{ mesCorto(r.mes) }}</span>
                </div>
              }
            </div>
            <p class="mt-4 text-sm text-muted-fg">{{ m.comprobantesImportados }} comprobantes importados en total · {{ m.comprobantesEsteMes }} este mes.</p>
          </section>
        } @else if (metricas().cargando) {
          <div class="card mt-6 h-48 animate-pulse"></div>
        }
        <app-error-carga [mensaje]="metricas().error" />
      }

      @case ('usuarios') {
        <div class="mt-6 flex flex-wrap items-center gap-3">
          <app-buscador placeholder="Buscar por nombre, correo o RUC" [valor]="busqueda()" (valorChange)="buscar($event)" />
          <select class="input w-auto min-w-40" aria-label="Filtrar por plan" (change)="filtroPlan.set($any($event.target).value)">
            <option value="">Todos los planes</option>
            @for (p of planes; track p.id) { <option [value]="p.id">{{ p.nombre }}</option> }
          </select>
        </div>

        @if (usuarios().datos; as lista) {
          <div class="card mt-4 overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="border-b border-border text-left text-muted-fg">
                <tr>
                  <th class="px-4 py-3 font-medium">Usuario</th>
                  <th class="px-4 py-3 font-medium">Plan</th>
                  <th class="px-4 py-3 font-medium">Uso</th>
                  <th class="px-4 py-3 font-medium">Registro · último ingreso</th>
                  <th class="px-4 py-3 text-right font-medium">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (u of lista; track u.id) {
                  <tr class="border-b border-border align-top last:border-0" [class.opacity-60]="!u.activo">
                    <td class="px-4 py-3">
                      <p class="font-medium">{{ u.nombre }} @if (u.rol === 'admin') { <span class="pildora ml-1 bg-ink text-[10px] text-ink-fg">Admin</span> }</p>
                      <p class="text-xs text-muted-fg">{{ u.correo }}@if (u.ruc) { · RUC {{ u.ruc }} }</p>
                      @if (!u.activo) { <p class="mt-1 text-xs font-medium text-danger">Suspendida</p> }
                    </td>
                    <td class="px-4 py-3">
                      <select class="input min-h-9 w-auto py-1 text-sm" [attr.aria-label]="'Plan de ' + u.nombre" [disabled]="!!u.estadoSuscripcion || ocupado() === u.id"
                        (change)="cambiarPlan(u, $any($event.target).value)">
                        @for (p of planes; track p.id) { <option [value]="p.id" [selected]="p.id === u.plan">{{ p.nombre }}</option> }
                      </select>
                      @if (u.estadoSuscripcion) {
                        <p class="mt-1 text-xs text-muted-fg">Kushki: {{ estadosSuscripcion[u.estadoSuscripcion] }}</p>
                      }
                    </td>
                    <td class="px-4 py-3 text-xs text-muted-fg">{{ u.comprobantes }} comprobantes<br />{{ u.facturasEmitidas }} facturas</td>
                    <td class="px-4 py-3 text-xs text-muted-fg">{{ u.fechaCreacion | date: 'dd/MM/yyyy' }}<br />{{ u.ultimoIngresoEn ? (u.ultimoIngresoEn | date: 'dd/MM/yyyy HH:mm') : 'Nunca' }}</td>
                    <td class="px-4 py-3">
                      @if (confirmar()?.id === u.id) {
                        <div class="flex flex-wrap justify-end gap-2">
                          <span class="w-full text-right text-xs">{{ confirmar()!.texto }}</span>
                          <button type="button" class="btn btn-danger min-h-8 px-3 text-xs" (click)="ejecutarConfirmado(u)">Confirmar</button>
                          <button type="button" class="btn btn-ghost min-h-8 px-3 text-xs" (click)="confirmar.set(null)">Cancelar</button>
                        </div>
                      } @else {
                        <div class="flex justify-end gap-1">
                          <button type="button" class="btn btn-ghost min-h-8 px-2" title="Ver actividad" [attr.aria-label]="'Actividad de ' + u.nombre" (click)="verActividad(u)">
                            <svg lucideHistory class="size-4" aria-hidden="true"></svg>
                          </button>
                          @if (u.id !== miId()) {
                            @if (u.activo) {
                              <button type="button" class="btn btn-ghost min-h-8 px-2" title="Suspender" [attr.aria-label]="'Suspender a ' + u.nombre" (click)="confirmar.set({ id: u.id, accion: 'suspender', texto: '¿Suspender esta cuenta?' })">
                                <svg lucideBan class="size-4" aria-hidden="true"></svg>
                              </button>
                            } @else {
                              <button type="button" class="btn btn-ghost min-h-8 px-2" title="Reactivar" [attr.aria-label]="'Reactivar a ' + u.nombre" (click)="cambiarEstado(u, true)">
                                <svg lucideUserCheck class="size-4" aria-hidden="true"></svg>
                              </button>
                            }
                            @if (u.estadoSuscripcion) {
                              <button type="button" class="btn btn-ghost min-h-8 px-2 text-xs" (click)="confirmar.set({ id: u.id, accion: 'cancelar', texto: '¿Cancelar su suscripción en Kushki?' })">Cancelar plan</button>
                            }
                            <button type="button" class="btn btn-ghost min-h-8 px-2 text-danger" title="Eliminar" [attr.aria-label]="'Eliminar a ' + u.nombre" (click)="confirmar.set({ id: u.id, accion: 'eliminar', texto: '¿Eliminar la cuenta y todos sus datos? No se puede deshacer.' })">
                              <svg lucideTrash2 class="size-4" aria-hidden="true"></svg>
                            </button>
                          }
                        </div>
                      }
                    </td>
                  </tr>
                  @if (actividad()?.id === u.id) {
                    <tr class="border-b border-border bg-muted/40">
                      <td colspan="5" class="px-4 py-3">
                        <p class="text-xs font-medium">Actividad reciente</p>
                        <ul class="mt-2 space-y-1 text-xs">
                          @for (a of actividad()!.avisos; track a.id) {
                            <li><span class="text-muted-fg">{{ fecha(a.fecha) }}</span> · {{ a.titulo }} @if (a.dispositivo) { <span class="text-muted-fg">· {{ a.dispositivo }}</span> }</li>
                          } @empty {
                            <li class="text-muted-fg">Sin actividad registrada.</li>
                          }
                        </ul>
                      </td>
                    </tr>
                  }
                } @empty {
                  <tr><td colspan="5" class="px-4 py-10 text-center text-muted-fg">No hay usuarios con ese filtro.</td></tr>
                }
              </tbody>
            </table>
          </div>
        } @else if (usuarios().cargando) {
          <div class="card mt-4 h-48 animate-pulse"></div>
        }
        <app-error-carga [mensaje]="usuarios().error" />
      }

      @case ('pagos') {
        @if (pagos().datos; as lista) {
          <div class="card mt-6 overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="border-b border-border text-left text-muted-fg">
                <tr><th class="px-4 py-3 font-medium">Fecha</th><th class="px-4 py-3 font-medium">Usuario</th><th class="px-4 py-3 font-medium">Plan</th><th class="px-4 py-3 text-right font-medium">Monto</th><th class="px-4 py-3 font-medium">Resultado</th></tr>
              </thead>
              <tbody>
                @for (p of lista; track p.id) {
                  <tr class="border-b border-border last:border-0">
                    <td class="px-4 py-3 whitespace-nowrap text-muted-fg">{{ p.fecha | date: 'dd/MM/yyyy HH:mm' }}</td>
                    <td class="px-4 py-3">{{ p.usuario }}<br /><span class="text-xs text-muted-fg">{{ p.correo }}</span></td>
                    <td class="px-4 py-3">{{ p.plan }}</td>
                    <td class="money px-4 py-3 text-right font-medium">{{ p.monto | dinero }}</td>
                    <td class="px-4 py-3">
                      @if (p.aprobado) {
                        <span class="pildora bg-success-soft text-success"><svg lucideCircleCheck class="size-3.5" aria-hidden="true"></svg> Aprobado</span>
                      } @else {
                        <span class="pildora bg-danger-soft text-danger"><svg lucideCircleX class="size-3.5" aria-hidden="true"></svg> Rechazado</span>
                        @if (p.detalle) { <p class="mt-1 text-xs text-muted-fg">{{ p.detalle }}</p> }
                      }
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="5" class="px-4 py-10 text-center text-muted-fg">Aún no hay cobros. Aparecerán aquí cuando Kushki avise de cada pago.</td></tr>
                }
              </tbody>
            </table>
          </div>
        }
        <app-error-carga [mensaje]="pagos().error" />
      }

      @case ('sistema') {
        @if (sistema().datos; as s) {
          <section class="card mt-6 max-w-2xl p-5 sm:p-6">
            <ul class="divide-y divide-border text-sm">
              @for (r of [
                { t: 'Pagos con Kushki', ok: s.kushkiConfigurado, d: s.kushkiConfigurado ? (s.kushkiPruebas ? 'Ambiente de pruebas (UAT)' : 'Producción') : 'Faltan Kushki:ClavePublica y Kushki:ClavePrivada' },
                { t: 'Webhooks de Kushki', ok: s.kushkiWebhookConfigurado, d: s.kushkiWebhookConfigurado ? 'Firma configurada' : 'Falta Kushki:FirmaWebhook' },
                { t: 'Correo saliente (avisos)', ok: s.smtpConfigurado, d: s.smtpConfigurado ? 'SMTP configurado' : 'Sin SMTP: los avisos solo quedan en la app' },
                { t: 'Buzón de facturas', ok: !!s.dominioBuzon, d: s.dominioBuzon ?? 'Falta Correo:DominioEntrante' },
                { t: 'Claves de cifrado de firmas', ok: s.dataProtectionPersistente, d: s.dataProtectionPersistente ? 'Carpeta persistente configurada' : 'Define DataProtection:RutaClaves antes de producción' },
                { t: 'Base de datos', ok: s.migracionesPendientes === 0, d: s.migracionesPendientes === 0 ? 'Al día' : s.migracionesPendientes + ' migraciones pendientes' }
              ]; track r.t) {
                <li class="flex items-start gap-3 py-3">
                  @if (r.ok) { <svg lucideCircleCheck class="mt-0.5 size-5 shrink-0 text-success" aria-hidden="true"></svg> }
                  @else { <svg lucideCircleX class="mt-0.5 size-5 shrink-0 text-warning" aria-hidden="true"></svg> }
                  <span><span class="font-medium">{{ r.t }}</span><br /><span class="text-muted-fg">{{ r.d }}</span></span>
                </li>
              }
            </ul>
            <p class="mt-4 text-xs text-muted-fg">Entorno {{ s.entorno }} · hora del servidor {{ s.horaServidorUtc | date: 'dd/MM/yyyy HH:mm' }}</p>
          </section>
        }
        <app-error-carga [mensaje]="sistema().error" />
      }
    }
  `,
})
export class Admin {
  private readonly servicio = inject(AdminService);
  private readonly avisos = inject(AvisosService);
  private readonly auth = inject(AuthService);

  protected readonly pestanas = [
    { valor: 'resumen', etiqueta: 'Resumen' },
    { valor: 'usuarios', etiqueta: 'Usuarios' },
    { valor: 'pagos', etiqueta: 'Pagos' },
    { valor: 'sistema', etiqueta: 'Sistema' },
  ] as const;
  protected readonly planes = PLANES;
  protected readonly estadosSuscripcion = ESTADOS_SUSCRIPCION;
  protected readonly pestana = signal<PestanaAdmin>('resumen');
  protected readonly miId = computed(() => this.auth.usuario()?.id);

  private readonly recargas = signal(0);
  protected readonly busqueda = signal('');
  private readonly busquedaAplicada = signal('');
  protected readonly filtroPlan = signal<PlanSuscripcion | ''>('');
  protected readonly ocupado = signal<number | null>(null);
  protected readonly confirmar = signal<{ id: number; accion: 'suspender' | 'eliminar' | 'cancelar'; texto: string } | null>(null);
  protected readonly actividad = signal<{ id: number; avisos: AvisoCuenta[] } | null>(null);
  private temporizador?: ReturnType<typeof setTimeout>;

  protected readonly metricas = cargar(() => this.recargas(), () => this.servicio.metricas());
  protected readonly usuarios = cargar(
    () => ({ q: this.busquedaAplicada(), plan: this.filtroPlan(), _: this.recargas() }),
    ({ q, plan }) => this.servicio.usuarios(q, plan),
  );
  protected readonly pagos = cargar(() => this.recargas(), () => this.servicio.pagos());
  protected readonly sistema = cargar(() => this.recargas(), () => this.servicio.sistema());
  protected readonly maxRegistros = computed(() => Math.max(0, ...(this.metricas().datos?.registrosPorMes.map((r) => r.usuarios) ?? [])));

  protected mesCorto(mes: number): string {
    return capitalizar(nombreMes(mes).slice(0, 3));
  }

  protected fecha(iso: string): string {
    return fechaHora(iso);
  }

  protected buscar(texto: string): void {
    this.busqueda.set(texto);
    clearTimeout(this.temporizador);
    this.temporizador = setTimeout(() => this.busquedaAplicada.set(texto), 300);
  }

  protected cambiarPlan(u: UsuarioAdmin, plan: PlanSuscripcion): void {
    this.accion(u.id, this.servicio.cambiarPlan(u.id, plan), `${u.nombre} ahora tiene el plan ${plan} (cortesía, sin cobro).`);
  }

  protected cambiarEstado(u: UsuarioAdmin, activo: boolean): void {
    this.accion(u.id, this.servicio.cambiarEstado(u.id, activo), activo ? `Reactivaste la cuenta de ${u.nombre}.` : `Suspendiste la cuenta de ${u.nombre}.`);
  }

  protected ejecutarConfirmado(u: UsuarioAdmin): void {
    const accion = this.confirmar()?.accion;
    this.confirmar.set(null);
    if (accion === 'suspender') this.cambiarEstado(u, false);
    else if (accion === 'cancelar') this.accion(u.id, this.servicio.cancelarSuscripcion(u.id), `Cancelaste la suscripción de ${u.nombre}.`);
    else if (accion === 'eliminar') this.accion(u.id, this.servicio.eliminar(u.id), `Eliminaste la cuenta de ${u.nombre}.`);
  }

  protected verActividad(u: UsuarioAdmin): void {
    if (this.actividad()?.id === u.id) {
      this.actividad.set(null);
      return;
    }
    this.servicio.actividad(u.id).subscribe({
      next: (avisos) => this.actividad.set({ id: u.id, avisos }),
      error: (e) => this.avisos.error(e),
    });
  }

  private accion(id: number, peticion: Observable<void>, exito: string): void {
    this.ocupado.set(id);
    peticion.subscribe({
      next: () => {
        this.ocupado.set(null);
        this.avisos.exito(exito);
        this.recargas.update((n) => n + 1);
      },
      error: (e) => {
        this.ocupado.set(null);
        this.avisos.error(e);
        this.recargas.update((n) => n + 1);
      },
    });
  }
}
