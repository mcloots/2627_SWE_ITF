import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { appEnv } from '../config/app-env';
import { CreatePostRequest } from '../models/create-post-request.model';
import { CreatePostResponse } from '../models/create-post-response.model';

@Injectable({ providedIn: 'root' })
export class PostsApiService {
  private readonly http = inject(HttpClient);

  createPost(request: CreatePostRequest): Observable<CreatePostResponse> {
    return this.http.post<CreatePostResponse>(`${appEnv.apiBaseUrl}/posts`, request);
  }
}
