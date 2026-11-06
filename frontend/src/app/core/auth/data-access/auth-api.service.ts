import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AuthMeResponse,
  AuthService as GeneratedAuthService,
  CurrentUserDto,
} from '../../api/generated';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly api = inject(GeneratedAuthService);

  registerCurrentUser(displayName: string): Observable<CurrentUserDto> {
    return this.api.apiAuthMePost$Json({
      body: { displayName },
    });
  }

  getIdentity(): Observable<AuthMeResponse> {
    return this.api.apiAuthMeGet$Json();
  }
}
