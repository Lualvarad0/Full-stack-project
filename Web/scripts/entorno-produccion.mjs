// Genera src/environments/environment.ts para el build de producción (Vercel u otro CI).
// Ese archivo está en .gitignore; en el servidor de build se arma con la variable NUDO_API_URL,
// la URL pública de la API terminada en /api (p. ej. https://nudo-api.onrender.com/api).
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const apiUrl = (process.env.NUDO_API_URL ?? '').trim().replace(/\/+$/, '');
if (!/^https:\/\/.+\/api$/.test(apiUrl)) {
  console.error('Falta NUDO_API_URL (https://…/api). Configúrala en las variables de entorno del proyecto.');
  process.exit(1);
}

const destino = fileURLToPath(new URL('../src/environments/environment.ts', import.meta.url));
writeFileSync(
  destino,
  `// Generado por scripts/entorno-produccion.mjs; no editar a mano.\nexport const environment = {\n  apiUrl: ${JSON.stringify(apiUrl)},\n};\n`,
);
console.log(`environment.ts generado (apiUrl: ${apiUrl})`);
