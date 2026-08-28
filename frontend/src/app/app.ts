import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppShell } from './core/layout/app-shell';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppShell],
  templateUrl: './app.html'
})
export class App {
}
