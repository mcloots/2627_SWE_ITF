import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { CreatePostRequest } from '../../../core/models/create-post-request.model';
import { CreatePostResponse } from '../../../core/models/create-post-response.model';
import { PostsApiService } from '../../../core/services/posts-api.service';
import { Card } from '../../../shared/ui/card';
import { PageTitle } from '../../../shared/ui/page-title';

type SaveState = 'idle' | 'saving' | 'saved' | 'error';

@Component({
  selector: 'app-create-post',
  imports: [FormsModule, PageTitle, Card],
  templateUrl: './create-post.html',
})
export class CreatePost {
  private readonly postsApi = inject(PostsApiService);
  private readonly destroyRef = inject(DestroyRef);

  // Test-only author. With authentication, the API should use the logged-in user's identity.
  readonly authorId = signal(crypto.randomUUID());
  readonly content = signal('');
  readonly state = signal<SaveState>('idle');
  readonly post = signal<CreatePostResponse | null>(null);
  readonly error = signal<string | null>(null);
  readonly maximumLength = 2000;
  readonly isSaving = computed(() => this.state() === 'saving');
  readonly isValidAuthor = computed(() => {
    const id = this.authorId().trim();
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)
      && id !== '00000000-0000-0000-0000-000000000000';
  });
  readonly canSubmit = computed(() =>
    this.isValidAuthor() && this.content().trim().length > 0
      && this.content().length <= this.maximumLength && !this.isSaving(),
  );

  createPost(): void {
    if (!this.canSubmit()) return;

    const request: CreatePostRequest = {
      authorId: this.authorId().trim(),
      content: this.content().trim(),
    };

    this.state.set('saving');
    this.post.set(null);
    this.error.set(null);

    this.postsApi.createPost(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.post.set(response);
        this.content.set('');
        this.authorId.set(crypto.randomUUID());
        this.state.set('saved');
      },
      error: (error: unknown) => {
        this.error.set(error instanceof HttpErrorResponse && error.status === 400
          ? 'The post could not be saved. Check the author ID and content.'
          : 'Could not create your post. Please try again.');
        this.state.set('error');
      },
    });
  }
}
