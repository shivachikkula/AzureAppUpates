import { Injectable, inject, signal } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { AccountInfo } from '@azure/msal-browser';
import { UserRole } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly msal = inject(MsalService);

  readonly account = signal<AccountInfo | null>(null);
  readonly roles = signal<UserRole[]>([]);

  init(): void {
    const active = this.msal.instance.getActiveAccount() ?? this.msal.instance.getAllAccounts()[0] ?? null;
    this.msal.instance.setActiveAccount(active);
    this.account.set(active);
    this.roles.set(this.extractRoles(active));
  }

  login(): void {
    this.msal.loginRedirect();
  }

  logout(): void {
    this.msal.logoutRedirect();
  }

  get displayName(): string {
    return this.account()?.name ?? this.account()?.username ?? 'Unknown user';
  }

  get email(): string {
    return this.account()?.username ?? '';
  }

  isManager(): boolean {
    return this.roles().includes('Manager') || this.roles().includes('Admin');
  }

  isAdmin(): boolean {
    return this.roles().includes('Admin');
  }

  private extractRoles(account: AccountInfo | null): UserRole[] {
    const claims = account?.idTokenClaims as { roles?: UserRole[] } | undefined;
    return claims?.roles ?? [];
  }
}
