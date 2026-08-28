import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-title',
  imports: [],
  templateUrl: './page-title.html'
})
export class PageTitle {
  readonly title = input.required<string>();
  readonly subtitle = input<string>('');
}
