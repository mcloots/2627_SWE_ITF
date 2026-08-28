import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { appEnv } from '../config/app-env';

@Component({
  selector: 'app-topbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './topbar.html'
})
export class Topbar {
  readonly appName = appEnv.appName;
}
