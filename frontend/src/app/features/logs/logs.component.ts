import { Component, DestroyRef, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApplicationsService } from '../../core/services/applications.service';
import { LogsService } from '../../core/services/logs.service';
import { AzureApplication, LogEntry } from '../../core/models/domain.models';

@Component({
  selector: 'app-logs',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './logs.component.html',
  styleUrl: './logs.component.scss',
})
export class LogsComponent {
  private readonly applicationsService = inject(ApplicationsService);
  private readonly logsService = inject(LogsService);
  private readonly msal = inject(MsalService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('logView') logView?: ElementRef<HTMLDivElement>;

  readonly apps = signal<AzureApplication[]>([]);
  readonly loadingApps = signal(true);
  readonly selectedAppId = signal<string | null>(null);
  readonly streaming = signal(false);
  readonly entries = signal<LogEntry[]>([]);
  readonly connectionError = signal<string | null>(null);

  private streamSub?: Subscription;

  constructor() {
    this.applicationsService.getMyApplications().subscribe({
      next: (apps) => {
        this.apps.set(apps);
        this.loadingApps.set(false);
        const preselect = this.route.snapshot.queryParamMap.get('appId');
        if (preselect && apps.some((a) => a.id === preselect)) {
          this.selectApp(preselect);
        }
      },
      error: () => this.loadingApps.set(false),
    });

    this.destroyRef.onDestroy(() => {
      this.streamSub?.unsubscribe();
      this.logsService.dispose();
    });
  }

  selectApp(appId: string): void {
    this.selectedAppId.set(appId);
    this.entries.set([]);
    this.connectionError.set(null);
    this.streamSub?.unsubscribe();
    this.streaming.set(true);

    this.streamSub = this.logsService.streamLogs(appId, () => this.acquireToken()).subscribe({
      next: (entry) => {
        this.entries.update((current) => {
          const next = [...current, entry];
          return next.length > 500 ? next.slice(next.length - 500) : next;
        });
        queueMicrotask(() => this.scrollToBottom());
      },
      error: () => {
        this.streaming.set(false);
        this.connectionError.set('Lost connection to the live log stream. Reselect the application to retry.');
      },
    });
  }

  private async acquireToken(): Promise<string> {
    const account = this.msal.instance.getActiveAccount() ?? this.msal.instance.getAllAccounts()[0];
    const result = await this.msal.instance.acquireTokenSilent({
      scopes: [environment.entraId.apiScope],
      account,
    });
    return result.accessToken;
  }

  private scrollToBottom(): void {
    const el = this.logView?.nativeElement;
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }

  levelClass(level: LogEntry['level']): string {
    return `log-line level-${level.toLowerCase()}`;
  }
}
