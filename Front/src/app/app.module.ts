import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AuthModule } from './auth/auth.module';
import { HttpClientModule } from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ComponentsModule } from './components/components.module';
import { LayoutComponent } from './components/layout/layout.component';
import { NavigationGuard } from './guards/navigation.guard';

@NgModule({
  declarations: [
    AppComponent,

  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserModule,
    AuthModule,
    HttpClientModule,
    ReactiveFormsModule,
    RouterModule,
    ComponentsModule
  ],
  exports:[

  ],
  providers: [

  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
