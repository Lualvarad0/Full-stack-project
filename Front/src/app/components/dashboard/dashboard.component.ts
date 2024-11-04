import { Component, OnInit } from '@angular/core';
import { Chart } from 'chart.js/auto';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  totalFacturas: number = 150;
  totalUsuarios: number = 50;
  totalProductos: number = 100;
  isAdmin: boolean = true; // Cambia esto a false si el usuario no es admin

  constructor() {}

  ngOnInit(): void {
    this.loadChart();
  }

  loadChart(): void {
    const canvas = document.getElementById('myChart') as HTMLCanvasElement;
    const ctx = canvas?.getContext('2d');

    if (ctx) {
      new Chart(ctx, {
        type: 'bar',  // Gráfico de barras
        data: {
          labels: ['Facturas', 'Usuarios', 'Productos'],
          datasets: [
            {
              label: 'Cantidad',
              data: [this.totalFacturas, this.totalUsuarios, this.totalProductos],
              backgroundColor: [
                'rgba(54, 162, 235, 0.5)',
                'rgba(255, 99, 132, 0.5)',
                'rgba(75, 192, 192, 0.5)'
              ],
              borderColor: [
                'rgba(54, 162, 235, 1)',
                'rgba(255, 99, 132, 1)',
                'rgba(75, 192, 192, 1)'
              ],
              borderWidth: 1
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false, // Permite mayor flexibilidad de tamaño
          scales: {
            y: {
              beginAtZero: true
            }
          },
          plugins: {
            legend: {
              display: false
            },
            title: {
              display: true,
              text: 'Estadísticas Generales'
            }
          }
        }
      });
    } else {
      console.error('No se pudo obtener el contexto del canvas para el gráfico');
    }
  }
}
