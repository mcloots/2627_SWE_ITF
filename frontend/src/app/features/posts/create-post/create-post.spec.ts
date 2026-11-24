import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { appEnv } from '../../../core/config/app-env';
import { CreatePost } from './create-post';

describe('CreatePost workshop interaction', () => {
  let fixture: ComponentFixture<CreatePost>;
  let http: HttpTestingController;
  let page: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreatePost],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(CreatePost);
    http = TestBed.inject(HttpTestingController);
    page = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  async function submitDraft(content: string): Promise<void> {
    const textarea = page.querySelector('textarea')!;
    textarea.value = content;
    textarea.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    page.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
  }

  it('keeps the draft visible and sends only one POST while a slow request is pending', async () => {
    const content = 'Workshop draft on a slow connection';
    const authorId = fixture.componentInstance.authorId();
    await submitDraft(content);
    const request = http.expectOne(`${appEnv.apiBaseUrl}/posts`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ authorId, content });
    expect(page.querySelector('textarea')!.value).toBe(content);
    expect(page.querySelector('button')!.disabled).toBe(true);
    expect(page.querySelector('button')!.textContent).toContain('Creating...');
    expect(page.querySelector('form')!.getAttribute('aria-busy')).toBe('true');

    // Enter/form submission must also be guarded, independently of the disabled button.
    page.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
    http.expectNone(`${appEnv.apiBaseUrl}/posts`);

    const postId = '22222222-2222-4222-8222-222222222222';
    request.flush({ postId }, { status: 201, statusText: 'Created' });
    await fixture.whenStable();
    expect(page.textContent).toContain('Post created');
    expect(page.textContent).toContain(postId);
    expect(page.querySelector('textarea')!.value).toBe('');
    expect(page.querySelector('form')!.getAttribute('aria-busy')).toBe('false');
  });

  it('preserves a failed draft and allows a deliberate retry', async () => {
    const content = 'Keep this draft when the API is unavailable';
    await submitDraft(content);
    http.expectOne(`${appEnv.apiBaseUrl}/posts`).flush('Unavailable', {
      status: 503, statusText: 'Service Unavailable',
    });
    await fixture.whenStable();
    expect(page.querySelector('[role="alert"]')!.textContent).toContain('Could not create your post');
    expect(page.querySelector('textarea')!.value).toBe(content);
    expect(page.querySelector('button')!.disabled).toBe(false);
    http.expectNone(`${appEnv.apiBaseUrl}/posts`);

    page.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
    const retry = http.expectOne(`${appEnv.apiBaseUrl}/posts`);
    expect(retry.request.body.content).toBe(content);
    retry.flush({ postId: '33333333-3333-4333-8333-333333333333' }, {
      status: 201, statusText: 'Created',
    });
    await fixture.whenStable();
    expect(page.textContent).toContain('Post created');
  });
});
