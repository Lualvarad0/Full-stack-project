import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ClienteService {
  private apiUrl = 'http://localhost:3000/api/clientes'; // Cambia la URL a la de tu API

  constructor(private http: HttpClient) {}

  // Obtener todos los clientes
  obtenerClientes(filtros?: any): Observable<any> {
    let url = this.apiUrl;
    if (filtros) {
      const query = new URLSearchParams(filtros).toString();
      url += `?${query}`;
    }
    return this.http.get(url);
  }
}
