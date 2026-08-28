import { Component } from '@angular/core';
import { Topbar } from './topbar';

@Component({
  selector: 'app-shell',
  imports: [Topbar],
  templateUrl: './app-shell.html'
})
export class AppShell {}
