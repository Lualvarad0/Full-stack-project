import { copyFile } from 'node:fs/promises';
import { constants } from 'node:fs';

// Nunca sobrescribe configuraciones locales o de despliegue existentes.
for (const name of ['environment.ts', 'environment.development.ts']) {
  const destination = new URL(`../src/environments/${name}`, import.meta.url);
  try {
    await copyFile(new URL(`${destination.href}.example`), destination, constants.COPYFILE_EXCL);
    console.log(`Creado ${name} desde la plantilla pública.`);
  } catch (error) {
    if (error.code !== 'EEXIST') throw error;
    console.log(`Conservado ${name} existente.`);
  }
}
