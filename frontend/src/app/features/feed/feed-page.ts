import { HttpClient } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { appEnv } from '../../core/config/app-env';
import { Card } from '../../shared/ui/card';
import { PageTitle } from '../../shared/ui/page-title';

interface FeedPost { sequence: number; postId: string; authorId: string; content: string; createdAt: string }
interface FeedPage { items: FeedPost[]; nextCursor: number | null }

@Component({
  selector: 'app-feed-page',
  imports: [FormsModule, DatePipe, Card, PageTitle],
  templateUrl: './feed-page.html',
})
export class FeedPageComponent {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly followerId = signal<string>(crypto.randomUUID());
  readonly authorId = signal('');
  readonly items = signal<FeedPost[]>([]);
  readonly cursor = signal<number | null>(null);
  readonly busy = signal(false);
  readonly message = signal('Follow an author, then create a post using that author ID.');
  readonly error = signal(false);

  changeFollower(value: string): void {
    this.followerId.set(value);
    this.items.set([]);
    this.cursor.set(null);
  }

  follow(): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(false);
    this.http.put(`${appEnv.apiBaseUrl}/authors/${encodeURIComponent(this.authorId().trim())}/followers/${encodeURIComponent(this.followerId().trim())}`, {})
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => this.message.set('Following this author. New posts will appear shortly after publication. Refresh to check.'),
        error: () => { this.error.set(true); this.message.set('Could not follow. Use two different, valid user UUIDs and check the API.'); },
      });
  }

  refresh(older = false): void {
    if (this.busy() || (older && !this.cursor())) return;
    this.busy.set(true);
    this.error.set(false);
    const query = older ? `?before=${this.cursor()}` : '';
    this.http.get<FeedPage>(`${appEnv.apiBaseUrl}/feeds/${encodeURIComponent(this.followerId().trim())}${query}`)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
      .subscribe({
        next: page => {
          this.items.update(current => older ? [...current, ...page.items] : page.items);
          this.cursor.set(page.nextCursor);
          this.message.set(page.items.length ? 'Feed updated. Most recently delivered posts appear first.' : 'No posts yet. Delivery may still be in progress.');
        },
        error: () => { this.error.set(true); this.message.set('Could not load this feed. Check the follower UUID and API connection.'); },
      });
  }
}
