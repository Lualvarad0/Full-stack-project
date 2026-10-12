import { RegimenTributario } from '../models/perfil.model';
export const REGIMENES: readonly {
  valor: RegimenTributario;
  nombre: string;
  descripcion: string;
}[] = [
  { valor: 'General', nombre: 'Régimen general', descripcion: 'Declaración de IVA mensual' },
  {
    valor: 'RimpeEmprendedor',
    nombre: 'RIMPE emprendedor',
    descripcion: 'Declaración de IVA semestral',
  },
  {
    valor: 'RimpeNegocioPopular',
    nombre: 'RIMPE negocio popular',
    descripcion: 'Cuota fija anual, sin IVA en ventas',
  },
];
