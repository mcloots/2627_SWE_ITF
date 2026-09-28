import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FeedPageComponent } from './feed-page';
import { appEnv } from '../../core/config/app-env';

describe('Follower feed', () => {
  let page: FeedPageComponent;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FeedPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    page = TestBed.createComponent(FeedPageComponent).componentInstance;
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('follows the chosen author and explains delayed delivery', () => {
    page.authorId.set('author');
    page.changeFollower('follower');
    page.follow();
    const request = http.expectOne(`${appEnv.apiBaseUrl}/authors/author/followers/follower`);
    expect(request.request.method).toBe('PUT');
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(page.busy()).toBe(false);
    expect(page.message()).toContain('shortly after publication');
  });

  it('uses the server cursor and clears the old feed when identity changes', () => {
    page.changeFollower('first');
    page.refresh();
    http.expectOne(`${appEnv.apiBaseUrl}/feeds/first`).flush({
      items: [{ sequence: 22, postId: 'post', authorId: 'author', content: 'Hello', createdAt: '2026-09-28T10:00:00Z' }], nextCursor: 22,
    });
    page.refresh(true);
    http.expectOne(`${appEnv.apiBaseUrl}/feeds/first?before=22`).flush({ items: [], nextCursor: null });
    expect(page.items().length).toBe(1);
    page.changeFollower('second');
    expect(page.items()).toEqual([]);
    expect(page.cursor()).toBeNull();
  });

  it('shows a recoverable error and releases the loading state', () => {
    page.changeFollower('follower');
    page.refresh();
    http.expectOne(`${appEnv.apiBaseUrl}/feeds/follower`).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(page.error()).toBe(true);
    expect(page.busy()).toBe(false);
  });
});
