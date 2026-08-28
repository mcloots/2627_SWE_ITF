import { Component } from '@angular/core';
import { PageTitle } from '../../shared/ui/page-title';
import { Card } from '../../shared/ui/card';
import { appEnv } from '../../core/config/app-env';

@Component({
  selector: 'app-home-page',
  imports: [PageTitle, Card],
  templateUrl: './home-page.html'
})
export class HomePage {
  readonly appName = appEnv.appName;
}
